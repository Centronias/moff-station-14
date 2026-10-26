using Content.Shared._Moffstation.Extensions;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Content.Shared._CentRadio;

// TODO CENT Something about tape length
[RegisterComponent]
public sealed partial class CassetteComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan PositionAtLastOperation;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastOperationAt;

    [DataField]
    public CassetteOperation LastOperation = CassetteOperation.Stop;

    [DataField]
    public SortedDictionary<TimeSpan, string> Data = new();
}

[Serializable, NetSerializable]
public enum CassetteOperation
{
    Stop,
    Play,
    FastForward,
    Rewind,
}

public sealed partial class CassetteSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;

    [Dependency] private EntityQuery<CassetteComponent> _cassetteQuery;
    [Dependency] private EntityQuery<CassetteContainerComponent> _cassetteContainerQuery;

    public Entity<CassetteComponent>? GetTape<T>(Entity<T> entity) where T : BaseCassetteContainerComponent =>
        entity.Comp.Container.ContainedEntity is { } contained ? _cassetteQuery.ResolveOrNull(contained) : null;

    private TimeSpan CurrentPosition(
        Entity<CassetteComponent> entity,
        Entity<CassetteContainerComponent>? container
    )
    {
        var timeSinceLastOperation = _gameTiming.CurTime - entity.Comp.LastOperationAt;
        var operationFactor = entity.Comp.LastOperation switch
        {
            CassetteOperation.Stop => 0,
            CassetteOperation.Play => 1,
            CassetteOperation.FastForward => container?.Comp?.FastOperationSpeed ?? 0,
            CassetteOperation.Rewind => -(container?.Comp?.FastOperationSpeed ?? 0),
            var unknown => this.UnknownEnumVariant<CassetteOperation, FixedPoint2>(unknown),
        };

        var tapePlaySinceLastOperation = timeSinceLastOperation * operationFactor.Double();
        return entity.Comp.PositionAtLastOperation + tapePlaySinceLastOperation;
    }

    // TODO CENT How do we deal with overwriting data?
    public void Write(Entity<CassetteComponent> entity, string data, Entity<CassetteContainerComponent>? container)
    {
        var currentPosition = CurrentPosition(entity, container);
        entity.Comp.Data[currentPosition] = data;
    }

    public bool WriteToContained(Entity<CassetteContainerComponent?> container, string data)
    {
        if (_cassetteContainerQuery.ResolveOrNull(container) is not { } c ||
            GetTape(c) is not { } tape)
            return false;

        Write(tape, data, c);
        return true;
    }
}

public abstract partial class BaseCassetteContainerComponent : Component
{
    [DataField("container", required: true)]
    public string ContainerId = default!;

    [ViewVariables] public ContainerSlot Container = default!;
}

public sealed partial class CassetteContainerComponent : BaseCassetteContainerComponent
{
    [DataField] public FixedPoint2 FastOperationSpeed = 3;
    [DataField] public CassetteOperation State = CassetteOperation.Stop;
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
    [Dependency] private IGameTiming _gameTiming = default!;

    public override void Update(float frameTime)
    {
        foreach (var entity in EntityQueryEnumerator<CassetteReaderComponent, CassetteContainerComponent>())
        {
            switch (entity.Comp2.State)
            {
                case CassetteOperation.Play:

                    break;
                case CassetteOperation.FastForward:
                    break;
                case CassetteOperation.Rewind:
                    break;
                case CassetteOperation.Stop:
                    // Not moving, do nothing.
                    continue;
                default:
                    this.UnknownEnumVariant<CassetteOperation, int>(entity.Comp2.State);
                    continue;
            }
        }
    }
}
