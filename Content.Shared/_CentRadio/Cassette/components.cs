using Content.Shared._Moffstation.Extensions;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Content.Shared._CentRadio.Cassette;

/// This component causes an entity to be a microdevice cassette tape. It can store data.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(CassetteSystem))]
public sealed partial class CassetteComponent : Component
{
    /// How much playtime this cassette has.
    [DataField]
    public TimeSpan Capacity = TimeSpan.FromMinutes(3);

    /// The cassette's most recent operation.
    [DataField, AutoNetworkedField]
    public CassetteOperation LastOperation = CassetteOperation.Stop;

    /// Where in the cassette's playtime it was when its operation was most recently changed.
    [DataField, AutoNetworkedField]
    public TimeSpan PositionAtLastOperation;

    /// When, in game-time, the cassette's operation was last changed.
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastOperationAt;

    /// The data written to this cassette, stored as data "at" positions in the cassette's playtime.
    [DataField, AutoNetworkedField]
    public Dictionary<TimeSpan, IMicrodeviceData.Raw> Data = new();

    /// Calculates the current position of this cassette, assuming its been undergoing <see cref="LastOperation"/> since
    /// <see cref="LastOperationAt"/>.
    /// <remarks>
    /// By using linear regression, we can keep track of the cassette's position without needing to literally update its
    /// state on every game tick.
    /// </remarks>
    public TimeSpan CurrentPosition(TimeSpan currentGameTime, FixedPoint2? fastOperationSpeed)
    {
        var timeSinceLastOperation = currentGameTime - LastOperationAt;
        var operationFactor = LastOperation.GetOperationFactor(fastOperationSpeed).Double();
        return (PositionAtLastOperation + timeSinceLastOperation * operationFactor).Clamp(TimeSpan.Zero, Capacity);
    }
}

/// This component marks a <see cref="CassetteComponent"/> as being actively played. It stores state about when events
/// will occur regarding the cassette.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(CassetteSystem))]
public sealed partial class PlayingCassetteComponent : Component
{
    /// When this cassette will emit <see cref="NextDataData"/> in game-time.
    /// <remarks>Don't try to use this directly, use it through <see cref="NextData"/></remarks>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    [Access] // No touching
    public TimeSpan? NextDataAt;

    /// What data will be available at <see cref="NextDataAt"/>.
    /// <remarks>Don't try to use this directly, use it through <see cref="NextData"/></remarks>
    [DataField, AutoNetworkedField]
    [Access] // No touching
    public IMicrodeviceData.Raw? NextDataData;

    /// When and what this cassette will emit next, if it's being read.
    /// This is a property to enforce the invariant that <see cref="NextDataAt"/> and <see cref="NextDataData"/> should
    /// either both be set or both be null.
    public (TimeSpan at, IMicrodeviceData.Raw data)? NextData
    {
        get => NextDataAt is { } at && NextDataData is { } data ? (at, data) : null;
        set
        {
            if (value is var (at, data))
            {
                NextDataAt = at;
                NextDataData = data;
            }
            else
            {
                NextDataAt = null;
                NextDataData = null;
            }
        }
    }

    public static readonly string[] NextDataFieldNames = [nameof(NextDataAt), nameof(NextDataData)];

    /// When this cassette will reach the end of its capacity in game-time.
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? StopAt;
}

/// This component makes an entity into a player container for entities with <see cref="CassetteComponent"/>. It does
/// not read or write to the cassette on its own, but it can move the current position of the cassette.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), Access(typeof(CassetteSystem))]
public sealed partial class CassetteContainerComponent : Component, IManagedContainer<ContainerSlot>
{
    /// When performing fast <see cref="CassetteOperation"/>s, how many tape-seconds are traverse per game-world-seconds.
    [DataField, AutoNetworkedField] public FixedPoint2 FastOperationSpeed = 3;

    /// The current <see cref="CassetteOperation"/> this container is performing.
    [DataField, AutoNetworkedField] public CassetteOperation State = CassetteOperation.Stop;

    /// The ID of the <see cref="ContainerSlot"/> which will hold a cassette for this component to operate on.
    [DataField("container", required: true), AutoNetworkedField]
    public string ContainerId = default!;

    /// <see cref="ContainerId"/>, but instantiated.
    [ViewVariables] public ContainerSlot Container = default!;

    // Below is just implementation to use `IManagedContainer`

    /// <inheritdoc/>
    string IManagedContainer<ContainerSlot>.ContainerId => ContainerId;

    /// <inheritdoc/>
    ContainerSlot IManagedContainer<ContainerSlot>.Container { set => Container = value; }
}

/// This component causes an entity with <see cref="CassetteContainerComponent"/> to write data to its contained
/// cassette when receiving microdevice inputs.
[RegisterComponent, NetworkedComponent, Access(typeof(CassetteWriterSystem))]
public sealed partial class CassetteWriterComponent : Component, IMicrodevice
{
    /// The input "port" on which this component receives data to be made to write to its cassette.
    public static readonly MicrodeviceInput Input = new("input");
}

/// This component causes an entity with <see cref="CassetteContainerComponent"/> to output microdevice data when it
/// reads cassette data.
[RegisterComponent, NetworkedComponent, Access(typeof(CassetteReaderSystem))]
public sealed partial class CassetteReaderComponent : Component, IMicrodevice
{
    /// The output "port" which this component sends data to.
    public static readonly MicrodeviceOutput Output = new("output");
}
