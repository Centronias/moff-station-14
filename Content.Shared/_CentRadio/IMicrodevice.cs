using System.Collections.Immutable;
using Robust.Shared.Random;

namespace Content.Shared._CentRadio;

public interface IMicrodevice;

public interface IMicrodeviceData
{
    bool RaiseSelf(IEventBus bus, MicrodeviceInput input, EntityUid target);
    ImmutableArray<char> AsRaw(IRobustRandom random);

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

    readonly record struct Raw(ImmutableArray<char> Data) : ITyped<Raw>
    {
        public ImmutableArray<char> AsRaw(IRobustRandom random) => Data;

        public static implicit operator Raw(ImmutableArray<char> data) => new(data);
    }

    readonly record struct SpeechAudio(string Message, string Speaker) : ITyped<SpeechAudio>
    {
        public ImmutableArray<char> AsRaw(IRobustRandom random) => Message.ToCharArray().ToImmutableArray();
    }
}
