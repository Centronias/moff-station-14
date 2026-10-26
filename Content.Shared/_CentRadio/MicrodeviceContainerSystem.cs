using System.Linq;
using Content.Shared._Moffstation.Extensions;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared._CentRadio;

public sealed partial class MicrodeviceContainerSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    [Dependency] private EntityQuery<MicrodeviceContainerComponent> _mdContainerQuery;

    public (Entity<MicrodeviceContainerComponent> containingEnt, int containerIndex)? ContainingMdContainerOrNull(
        EntityUid entity
    )
    {
        foreach (var container in _container.GetContainingContainers(entity))
        {
            if (_mdContainerQuery.ResolveOrNull(container.Owner, logMissing: false) is { } mdContainer &&
                mdContainer.Comp.ContainerIds.IndexOf(container.ID) is var idx &&
                idx != -1)
                return (mdContainer, idx);
        }

        return null;
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<MicrodeviceContainerComponent> entity, ref MapInitEvent args)
    {
        var containerComp = EnsureComp<ContainerManagerComponent>(entity);
        entity.Comp.Containers =
        [
            .. entity.Comp.ContainerIds.Select(id =>
                _container.EnsureContainer<ContainerSlot>(entity, id, containerComp)
            ),
        ];

        entity.Comp.Connections.Clear();
        foreach (var ((outputIndex, output), (inputIndex, input)) in entity.Comp.RawConnections)
        {
            if (entity.Comp.ContainerOrNull(outputIndex) is not { } outputSlot ||
                entity.Comp.ContainerOrNull(inputIndex) is not { } inputSlot)
                continue;

            entity.Comp.Connections.Add(((outputSlot, output), (inputSlot, input)));
        }
    }

    public bool Enqueue<TMicrodevice, TData>(
        Entity<TMicrodevice> source,
        TData data,
        MicrodeviceOutput output
    ) where TMicrodevice : Component, IMicrodevice where TData : IMicrodeviceData
    {
        if (ContainingMdContainerOrNull(source.Owner) is not var (mdContainer, idx))
            return false;

        // TODO CENT Do something more when the connection is "overloaded"
        if (mdContainer.Comp.Queue.TotalCount >= mdContainer.Comp.MaxQueuedEvents)
            return false;

        mdContainer.Comp.Queue.Enqueue((data, idx, output));
        return true;
    }

    public override void Update(float frameTime)
    {
        foreach (var entity in AllEntityQuery<MicrodeviceContainerComponent>())
        {
            foreach (var (data, idx, output) in entity.Comp.Queue.DrainAndSwap())
            {
                foreach (var (receiver, input) in entity.Comp.GetConnections(idx, output))
                {
                    var handled = data.RaiseSelf(EntityManager.EventBus, input, receiver);
                    DebugTools.Assert(handled);
                }
            }
        }
    }
}
