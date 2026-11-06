using Robust.Shared.Serialization;

namespace Content.Shared._CentRadio;

/// A typed <see cref="string"/> representing an input "port" on a microdevice.
[DataRecord, Serializable, NetSerializable]
public readonly partial record struct MicrodeviceInput(string Name);

/// A typed <see cref="string"/> representing an output "port" on a microdevice.
[DataRecord, Serializable, NetSerializable]
public readonly partial record struct MicrodeviceOutput(string Name);

/// <summary>
/// An event which is raised to send data from an outputting microdevice to a receiving microdevice.
/// </summary>
/// <param name="Container">The container facilitating the transmission</param>
/// <param name="Input">The input "port" on the receiving microdevice</param>
/// <param name="Data">The actual content of the transmission</param>
/// <param name="Handled">Whether or not the transmission has been handled</param>
/// <remarks>
/// It is intentional that the outputting microdevice is not included! Ths information here is only what the receiving
/// microdevice would know. As an analogy, when you flip a light switch, the light does not know what switch was
/// flipped, only that it received a "signal" to turn on. <br/>
/// More advanced usecases where the outputting microdevice needs to be known should <b>encode that information in the
/// <see cref="Data"/> itself</b>!
/// </remarks>
[ByRefEvent]
public partial record struct MicrodeviceInputEvent(
    Entity<MicrodeviceContainerComponent> Container,
    MicrodeviceInput Input,
    IMicrodeviceData Data,
    bool Handled = false
);

/// Typed version of <see cref="MicrodeviceInputEvent"/>.
/// <typeparam name="T">The type of <paramref cref="Data"/></typeparam>
[ByRefEvent]
public partial record struct MicrodeviceInputEvent<T>(
    Entity<MicrodeviceContainerComponent> Container,
    MicrodeviceInput Input,
    T Data,
    bool Handled = false
);

/// A specialized variant of <see cref="MicrodeviceInputEvent"/> indicating that no intentional handler handled the
/// input, so the fallback handler should be used.
[ByRefEvent]
public partial record struct UnhandledMicrodeviceInputEvent(
    Entity<MicrodeviceContainerComponent> Container,
    MicrodeviceInput Input,
    IMicrodeviceData Data
);
