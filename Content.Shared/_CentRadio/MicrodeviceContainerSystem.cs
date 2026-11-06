using System.Collections.Immutable;
using System.Linq;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._CentRadio;

public sealed partial class MicrodeviceContainerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private SharedContainerSystem _container = default!;

    [Dependency] private EntityQuery<MicrodeviceExposingComponentsOnContainerComponent> _exposingQuery;
    [Dependency] private EntityQuery<MicrodeviceContainerComponent> _mdContainerQuery;

    /// <summary>
    /// Enqueues a <see cref="IMicrodevice"/>'s output, causing it to be received by connected microdevices.
    /// </summary>
    /// <param name="source">The outputting microdevice</param>
    /// <param name="data">The output data</param>
    /// <param name="output">Which output the data is coming off of</param>
    /// <typeparam name="TMicrodevice">The type of microdevice</typeparam>
    /// <typeparam name="TData">The type of <paramref name="data"/></typeparam>
    /// <returns>True if the data was enqueued, false otherwise</returns>
    [PublicAPI]
    public bool Enqueue<TMicrodevice, TData>(
        Entity<TMicrodevice> source,
        TData data,
        MicrodeviceOutput output
    ) where TMicrodevice : Component, IMicrodevice where TData : IMicrodeviceData
    {
        if (GetContainingMdContainerOrNull(source.Owner) is not var (mdContainer, idx))
            return false;

        // TODO CENT Do something more when the connection is "overloaded"
        if (mdContainer.Comp.Queue.TotalCount >= mdContainer.Comp.MaxQueuedEvents)
            return false;

        mdContainer.Comp.Queue.Enqueue(new(data, idx, output));
        return true;
    }

    /// Gets the entity with <see cref="MicrodeviceContainerComponent"/> which contains the given microdevice
    /// <paramref name="entity"/>. Also returns the index of the container it's in, for other calls.
    [PublicAPI]
    public (Entity<MicrodeviceContainerComponent> containingEnt, int containerIndex)? GetContainingMdContainerOrNull(
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

    [PublicAPI]
    public IEnumerable<EntityUid> GetContainedMicrodevices(Entity<MicrodeviceContainerComponent> container)
    {
        foreach (var cont in container.Comp.Containers)
        {
            if (cont.ContainedEntity is { } e)
            {
                yield return e;
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnComponentStartup(Entity<MicrodeviceContainerComponent> entity, ref ComponentStartup args)
    {
        var containerComp = EnsureComp<ContainerManagerComponent>(entity);

        {
            var builder = ImmutableArray.CreateBuilder<ContainerSlot>();
            builder.AddRange(entity.Comp.ContainerIds.Select(id =>
                _container.EnsureContainer<ContainerSlot>(entity, id, containerComp)
            ));
            entity.Comp.Containers = builder.ToImmutable();
        }

        {
            var builder = ImmutableArray.CreateBuilder<(
                (ContainerSlot slot, MicrodeviceOutput output) output,
                (ContainerSlot slot, MicrodeviceInput input) input
                )>();
            foreach (var ((outputIndex, output), (inputIndex, input)) in entity.Comp.RawConnections)
            {
                if (entity.Comp.ContainerOrNull(outputIndex) is not { } outputSlot)
                {
                    Log.AssertOrLogError($"Container {nameof(outputIndex)}=#{outputIndex} does not exist");
                    continue;
                }

                if (entity.Comp.ContainerOrNull(inputIndex) is not { } inputSlot)
                {
                    Log.AssertOrLogError($"Container {nameof(inputIndex)}=#{inputIndex} does not exist");
                    continue;
                }

                builder.Add(((outputSlot, output), (inputSlot, input)));
            }

            entity.Comp.Connections = builder.ToImmutable();
        }
    }

    [SubscribeLocalEvent]
    private void OnExposingGotInserted(
        Entity<MicrodeviceContainerComponent> entity,
        ref EntInsertedIntoContainerMessage args
    )
    {
        if (_timing.ApplyingState ||
            _exposingQuery.ResolveOrNull(args.Entity, logMissing: false)?.Comp.Components is not { } comps)
            return;

        EntityManager.AddComponents(entity, comps, removeExisting: true);
    }

    [SubscribeLocalEvent]
    private void OnExposingGotRemoved(
        Entity<MicrodeviceContainerComponent> entity,
        ref EntRemovedFromContainerMessage args
    )
    {
        if (_timing.ApplyingState ||
            _exposingQuery.ResolveOrNull(args.Entity, logMissing: false)?.Comp.Components is not { } comps)
            return;

        EntityManager.RemoveComponents(entity, comps);
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        // Processes enqueued data, raising events on contained microdevices which should receive the data.
        foreach (var entity in EntityQueryEnumerator<MicrodeviceContainerComponent>())
        {
            // Avoid allocating an iterator if there's nothing to iterate over.
            if (entity.Comp.Queue.TotalCount == 0)
                continue;

            foreach (var (data, idx, output) in entity.Comp.Queue.DrainAndSwap())
            {
                foreach (var (receiver, input) in entity.Comp.GetConnections(idx, output))
                {
                    data.RaiseSelf(EntityManager.EventBus, entity, input, receiver);
                }
            }
        }
    }
}
