using Robust.Shared.GameStates;

namespace Content.Shared._CentRadio.Audio;

/// This component produces outputs from speech it hears.
[RegisterComponent, NetworkedComponent]
public sealed partial class MicrophoneComponent : Component, IMicrodevice
{
    public static readonly MicrodeviceOutput Output = new("audio");
}

/// This component produces speech from inputs its given.
[RegisterComponent, NetworkedComponent, Access(typeof(SpeakerSystem))]
public sealed partial class SpeakerComponent : Component, IMicrodevice
{
    public static readonly MicrodeviceInput Input = new("audio");
}
