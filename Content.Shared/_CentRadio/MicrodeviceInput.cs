namespace Content.Shared._CentRadio;

[DataRecord]
public readonly partial record struct MicrodeviceInput(string Name);

[DataRecord]
public readonly partial record struct MicrodeviceOutput(string Name);

public interface IMicrodeviceInputEvent
{
    bool Handled { get; }
    MicrodeviceInput Input { get; }
}

[ByRefEvent]
public partial record struct MicrodeviceInputEvent(
    MicrodeviceInput Input,
    IMicrodeviceData Data,
    bool Handled = false
) : IMicrodeviceInputEvent
{
    public MicrodeviceInput Input { get; } = Input;
    public IMicrodeviceData Data { get; } = Data;
}

[ByRefEvent]
public partial record struct MicrodeviceInputEvent<T>(
    MicrodeviceInput Input,
    T Data,
    bool Handled = false
) : IMicrodeviceInputEvent where T : IMicrodeviceData
{
    public MicrodeviceInput Input { get; } = Input;
    public T Data { get; } = Data;
}

[ByRefEvent]
public partial record struct UnhandledMicrodeviceInputEvent(MicrodeviceInputEvent Args);
