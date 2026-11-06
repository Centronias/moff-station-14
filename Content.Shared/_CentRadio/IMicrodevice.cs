using System.Collections.Immutable;
using System.Text;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared._CentRadio;

/// A marker interface for microdevice components.
public partial interface IMicrodevice : IComponent;

/// A type which represents data that is outputted and received by <see cref="IMicrodevice"/>s.
public partial interface IMicrodeviceData
{
    /// <summary>
    /// Raises <see cref="MicrodeviceInputEvent"/>s for this data. This is necessary to raise typed
    /// <see cref="MicrodeviceInputEvent{T}"/>s with the concrete runtime type of this data.
    /// </summary>
    /// <param name="bus">The event bus on which to raise the events.</param>
    /// <param name="container">The microdevice container which is facilitating the data transmission.</param>
    /// <param name="input">The input "port" which is receiving the data</param>
    /// <param name="target">The microdevice receiving the data</param>
    /// <returns>True if the data was intentionally handled.</returns>
    /// <remarks>This probably should never be used by anything other than <see cref="MicrodeviceContainerSystem"/></remarks>
    bool RaiseSelf(
        IEventBus bus,
        Entity<MicrodeviceContainerComponent> container,
        MicrodeviceInput input,
        EntityUid target
    );

    /// "Transmutes" this data into <see cref="Raw"/> data. This should be reversible.
    Raw AsRaw(IRobustRandom random);

    /// <see cref="IMicrodeviceData"/> which remembers its own type, <typeparamref name="T"/>.
    interface ITyped<T> : IMicrodeviceData where T : ITyped<T>
    {
        /// <inheritdoc/>
        bool IMicrodeviceData.RaiseSelf(
            IEventBus bus,
            Entity<MicrodeviceContainerComponent> container,
            MicrodeviceInput input,
            EntityUid target
        )
        {
            if (this is not T typed)
                throw new("Bad self type");

            var typedEv = new MicrodeviceInputEvent<T>(container, input, typed);
            bus.RaiseLocalEvent(target, ref typedEv);
            if (typedEv.Handled)
                return true;

            var untypedEv = new MicrodeviceInputEvent(container, input, this);
            bus.RaiseLocalEvent(target, ref untypedEv);
            if (untypedEv.Handled)
                return true;

            var unhandledEvent = new UnhandledMicrodeviceInputEvent(container, input, this);
            bus.RaiseLocalEvent(target, ref unhandledEvent);
            return false;
        }
    }

    /// Raw <see cref="IMicrodeviceData"/>. Just a wrapped sequence of <see cref="byte"/>s.
    [DataRecord, Serializable, NetSerializable]
    readonly partial record struct Raw([field: IncludeDataField] ImmutableArray<byte> Data) : ITyped<Raw>
    {
        public Raw AsRaw(IRobustRandom random) => this;

        // ReSharper disable once UseCollectionExpression // Disallowed by sandboxing
        public static implicit operator Raw(byte[] data) => new(ImmutableArray.Create(data));
    }

    /// Recorded speech. Holds onto the name of its speaker as the "identity" of the speaker.
    [Serializable]
    readonly record struct SpeechAudio(string Message, string Speaker) : ITyped<SpeechAudio>
    {
        public Raw AsRaw(IRobustRandom random) => Encoding.UTF8.GetBytes(Message);
    }
}
