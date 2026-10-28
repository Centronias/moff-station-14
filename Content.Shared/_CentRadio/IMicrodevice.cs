using System.Collections.Immutable;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Primitive;

namespace Content.Shared._CentRadio;

public interface IMicrodevice;

public partial interface IMicrodeviceData
{
    bool RaiseSelf(IEventBus bus, MicrodeviceInput input, EntityUid target);
    Raw AsRaw(IRobustRandom random);

    interface ITyped<T> : IMicrodeviceData where T : ITyped<T>
    {
        bool IMicrodeviceData.RaiseSelf(IEventBus bus, MicrodeviceInput input, EntityUid target)
        {
            if (this is not T typed)
                throw new("Bad self type");

            var typedEv = new MicrodeviceInputEvent<T>(input, typed);
            bus.RaiseLocalEvent(target, ref typedEv);
            if (typedEv.Handled)
                return true;

            var untypedEv = new MicrodeviceInputEvent(input, this);
            bus.RaiseLocalEvent(target, ref untypedEv);
            return untypedEv.Handled;
        }
    }

    [DataRecord, Serializable, NetSerializable]
    readonly partial record struct Raw([field: IncludeDataField] ImmutableArray<byte> Data) : ITyped<Raw>
    {
        public Raw AsRaw(IRobustRandom random) => this;

        public static implicit operator Raw(byte[] data) => new([.. data]);
    }

    [Serializable]
    readonly record struct SpeechAudio(string Message, string Speaker) : ITyped<SpeechAudio>
    {
        public Raw AsRaw(IRobustRandom random) => Encoding.UTF8.GetBytes(Message);
    }
}
