using Robust.Shared.Random;

namespace Content.Shared._CentRadio;

public interface IMicrodevice;

public interface IMicrodeviceData
{
    bool RaiseSelf(IEventBus bus, MicrodeviceInput input, EntityUid target);
    char[] AsRaw(IRobustRandom random);

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

    readonly record struct SpeechAudio(string Message, string Speaker) : ITyped<SpeechAudio>
    {
        public char[] AsRaw(IRobustRandom random) => Message.ToCharArray();
    }
}
