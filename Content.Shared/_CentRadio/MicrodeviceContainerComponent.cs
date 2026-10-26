using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared._CentRadio;

/// Creates a microcomponent "network" in the owner entity.
[RegisterComponent]
public sealed partial class MicrodeviceContainerComponent : Component
{
    [DataField("containers", required: true)]
    public string[] ContainerIds = default!;

    [ViewVariables] public ContainerSlot[] Containers = [];

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContainerSlot? ContainerOrNull(int index) => Containers.TryGetValue(index, out var ret) ? ret : null;

    [DataField("connections")]
    public List<(
        (int containerIndex, MicrodeviceOutput) output,
        (int containerIndex, MicrodeviceInput) input
        )> RawConnections = new();

    [ViewVariables]
    public List<(
        (ContainerSlot slot, MicrodeviceOutput output) output,
        (ContainerSlot slot, MicrodeviceInput input) input
        )> Connections = new();

    [DataField]
    public int MaxQueuedEvents = 10;

    [ViewVariables]
    public readonly SwapQueue<(IMicrodeviceData, int containerIndex, MicrodeviceOutput)> Queue = new();

    public IEnumerable<(EntityUid entity, MicrodeviceInput input)> GetConnections(
        EntityUid entity,
        MicrodeviceOutput output
    )
    {
        foreach (var (connectedOutput, input) in Connections)
        {
            if (entity == connectedOutput.slot.ContainedEntity &&
                output == connectedOutput.output &&
                input.slot.ContainedEntity is { } connected)
            {
                yield return (connected, input.input);
            }
        }
    }

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
