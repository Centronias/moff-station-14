using Content.Shared._Moffstation.Extensions;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Content.Shared._CentRadio;

// TODO CENT Something about tape length
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class CassetteComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan PositionAtLastOperation;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan LastOperationAt;

    [DataField, AutoNetworkedField]
    public CassetteOperation LastOperation = CassetteOperation.Stop;

    [DataField, AutoNetworkedField]
    public SortedDictionary<TimeSpan, string> Data = new();
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlayingCassetteComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan? NextData;
}

[Serializable, NetSerializable]
public enum CassetteOperation
{
    Stop,
    Play,
    FastForward,
    Rewind,
}

public static class CassetteOperationExt
{
    extension(CassetteOperation op)
    {
        /// Returns how many tape-time-seconds per second are moved when a cassette is undergoing <paramref name="op"/>
        /// in a <see cref="CassetteContainerComponent"/> with <paramref name="fastOperationSpeed"/>.
        public FixedPoint2 GetOperationFactor(FixedPoint2? fastOperationSpeed) => op switch
        {
            CassetteOperation.Stop => 0,
            CassetteOperation.Play => 1,
            CassetteOperation.FastForward => fastOperationSpeed ?? 0,
            CassetteOperation.Rewind => -(fastOperationSpeed ?? 0),
            _ => op.UnknownEnumVariant<CassetteOperation, FixedPoint2>(),
        };
    }
}

[ByRefEvent]
public readonly record struct PlayingCassetteDataEvent(
    Entity<CassetteComponent> Cassette,
    Entity<CassetteContainerComponent> Container,
    string Data
);

public abstract partial class BaseCassetteContainerComponent : Component
{
    [DataField("container", required: true)]
    public string ContainerId = default!;

    [ViewVariables] public ContainerSlot Container = default!;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class CassetteContainerComponent : BaseCassetteContainerComponent
{
    [DataField, AutoNetworkedField] public FixedPoint2 FastOperationSpeed = 3;
    [DataField, AutoNetworkedField] public CassetteOperation State = CassetteOperation.Stop;
}

[RegisterComponent]
public sealed partial class CassetteWriterComponent : BaseCassetteContainerComponent, IMicrodevice
{
    public static readonly MicrodeviceInput Input = new("Input");
}

public sealed partial class CassetteWriterSystem : ReceivingMicrodeviceSystem<CassetteWriterComponent>
{
    [Dependency] private CassetteSystem _cassette = default!;

    protected override void OnUnhandledMicrodeviceInput(
        Entity<CassetteWriterComponent> entity,
        MicrodeviceInput input,
        char[] rawData
    )
    {
        if (input != CassetteWriterComponent.Input)
        {
            Log.AssertOrLogError($"Unexpected input: \"{input}\"");
            return;
        }

        _cassette.WriteToContained(entity.Owner, new string(rawData));
    }
}

[RegisterComponent]
public sealed partial class CassetteReaderComponent : BaseCassetteContainerComponent, IMicrodevice
{
    public static readonly MicrodeviceInput Input = new("Output");
}

public sealed partial class CassetteReaderSystem : OutputtingMicrodeviceSystem<CassetteReaderComponent>
{
}
