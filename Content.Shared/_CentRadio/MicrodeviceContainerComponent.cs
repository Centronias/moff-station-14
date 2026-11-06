using System.Collections.Immutable;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._CentRadio;

/// Creates a microcomponent "network" in the owner entity.
// TODO CENT Field deltas
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(MicrodeviceContainerSystem))]
public sealed partial class MicrodeviceContainerComponent : Component
{
    /// The IDs of containers which comprise this "network".
    [DataField("containers", required: true), AutoNetworkedField]
    public string[] ContainerIds = default!;

    /// <see cref="ContainerIds"/>, instantiated.
    [ViewVariables] public ImmutableArray<ContainerSlot> Containers = ImmutableArray<ContainerSlot>.Empty;

    /// The description of connections between <see cref="Containers"/>.
    [DataField("connections"), AutoNetworkedField]
    public List<(
        (int containerIndex, MicrodeviceOutput) output,
        (int containerIndex, MicrodeviceInput) input
        )> RawConnections = new();

    /// <see cref="RawConnections"/>, but with the actual containers resolved.
    [ViewVariables]
    public ImmutableArray<(
        (ContainerSlot slot, MicrodeviceOutput output) output,
        (ContainerSlot slot, MicrodeviceInput input) input
        )> Connections = ImmutableArray<(
        (ContainerSlot slot, MicrodeviceOutput output) output,
        (ContainerSlot slot, MicrodeviceInput input) input
        )>.Empty;

    /// THe maximum number of events that can be put into <see cref="Queue"/> before they start getting dropped.
    /// <remarks>This is used to avoid overloading the game engine and also try to prevent infinite loops.</remarks>
    [DataField, AutoNetworkedField]
    public int MaxQueuedEvents = 10;

    /// The data current queued to be sent to other microdevices in this network.
    [ViewVariables]
    public readonly SwapQueue<QueueOutput> Queue = new();

    /// The type of elements in <see cref="Queue"/>.
    public readonly record struct QueueOutput(
        IMicrodeviceData Data,
        int SourceContainerIndex,
        MicrodeviceOutput Output
    );

    /// Retrieves the container at <paramref name="index"/>.
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContainerSlot? ContainerOrNull(int index) => Containers.TryGetValue(index, out var ret) ? ret : null;

    /// Retrieves the microdevices connected to the given microdevice container via the given output.
    public IEnumerable<(EntityUid entity, MicrodeviceInput input)> GetConnections(
        int containerIndex,
        MicrodeviceOutput output
    )
    {
        if (ContainerOrNull(containerIndex) is not { } container)
            yield break;

        foreach (var (connectedOutput, input) in Connections)
        {
            if (container == connectedOutput.slot &&
                output == connectedOutput.output &&
                input.slot.ContainedEntity is { } connected)
            {
                yield return (connected, input.input);
            }
        }
    }
}
