using Content.Shared._Moffstation.Extensions;
using Content.Shared.Administration.Logs;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._CentRadio.Cassette;

// Subscriptions and stuff
public sealed partial class CassetteSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;

    [Dependency] private SharedContainerSystem _container = default!;

    [Dependency] private EntityQuery<CassetteComponent> _cassetteQuery;
    [Dependency] private EntityQuery<CassetteContainerComponent> _cassetteContainerQuery;

    public override void Update(float frameTime)
    {
        var cassettes =
            EntityQueryEnumerator<PlayingCassetteComponent, CassetteComponent, TransformComponent, MetaDataComponent>();
        foreach (var entity in cassettes)
        {
            // If a `PlayingCassette` isn't in a container, it can't be playing. Stop it.
            if (GetContainer((entity.Owner, entity.Comp2), entity.Comp3, entity.Comp4) is not { } container)
            {
                SetTapeOperation((entity, entity), container: null, CassetteOperation.Stop);
                continue;
            }

            var needUpdate = false;
            needUpdate |= EmitNextData(entity, container);
            needUpdate |= StopIfTapeEnd(entity, container);

            if (needUpdate)
            {
                RecalculateNextDataAndStopAt(entity, container);
            }
        }

        // Deals with `PlayingCassetteComponent.NextData`
        bool EmitNextData(
            Entity<PlayingCassetteComponent, CassetteComponent> entity,
            Entity<CassetteContainerComponent> container
        )
        {
            if (entity.Comp1.NextData is not var (nextDataAt, nextData) ||
                _gameTiming.CurTime < nextDataAt)
                return false;

            entity.Comp1.NextData = null;
            DirtyFields(
                entity,
                entity.Comp1,
                meta: null,
                fields: PlayingCassetteComponent.NextDataFieldNames
            );

            var ev = new PlayingCassetteDataEvent((entity, entity), container, nextData);
            RaiseLocalEvent(entity, ref ev);
            RaiseLocalEvent(container, ref ev);

            return true;
        }

        // Deals with `PlayingCassetteComponent.StopAt`
        bool StopIfTapeEnd(
            Entity<PlayingCassetteComponent, CassetteComponent> entity,
            Entity<CassetteContainerComponent> container
        )
        {
            if (entity.Comp1.StopAt is not { } stopAt ||
                _gameTiming.CurTime < stopAt)
                return false;

            StopPlaying(container);
            return true;
        }
    }

    [SubscribeLocalEvent]
    private void OnCassetteContainerInit(Entity<CassetteContainerComponent> entity, ref ComponentInit args) =>
        _container.EnsureContainer<CassetteContainerComponent, ContainerSlot>(entity);

    [SubscribeLocalEvent]
    private void OnCassetteContainerExamined(Entity<CassetteContainerComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using var group = args.PushGroup(nameof(CassetteContainerComponent));

        if (entity.Comp.State != CassetteOperation.Stop)
        {
            // Display non-stopped operational state.
            args.PushMarkup(Loc.GetString($"cent_loc-cassette-container-examine-{entity.Comp.State.ToLocParam()}"));
        }

        if (GetTape(entity) is { } tape)
        {
            // Display tape status
            args.PushMarkup(GetTapeExamineText(tape, entity));
        }
    }

    [SubscribeLocalEvent]
    private void OnCassetteExamined(Entity<CassetteComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(GetTapeExamineText(entity, null));
    }

    private string GetTapeExamineText(
        Entity<CassetteComponent> entity,
        Entity<CassetteContainerComponent>? container
    ) => Loc.GetString(
        "cent_loc-cassette-position-examine",
        ("position", GetCurrentPosition(entity, container).ToMMSSString()),
        ("capacity", entity.Comp.Capacity.ToMMSSString())
    );

    // Pass in `precalcContainer` if you have it, otherwise we'll get it in here.
    private void RecalculateNextDataAndStopAt(
        Entity<PlayingCassetteComponent, CassetteComponent> entity,
        Entity<CassetteContainerComponent>? precalcContainer
    )
    {
        // If this playing cassette isn't in a container, don't worry about any of this.
        // We'll clean up the component in a subsequent `Update`.
        var cassette = new Entity<CassetteComponent>(entity, entity.Comp2);
        if ((precalcContainer ?? GetContainer(cassette)) is not { } container)
            return;

        // If the current operation isn't actually moving tape, don't worry about this.
        var operationFactor = entity.Comp2.LastOperation.GetOperationFactor(container.Comp.FastOperationSpeed);
        if (operationFactor == 0)
        {
            // Prediction means this gets tripped incorrectly...
            // Log.AssertOrLogError(
            //     "Operation factor is 0, implying this cassette isn't actually playing. This shouldn't happen.");
            return;
        }

        var currentPosition = GetCurrentPosition(cassette, container);

        // Update nextData.
        var sorted = new SortedDictionary<TimeSpan, IMicrodeviceData.Raw>(entity.Comp2.Data);
        var (below, above) = sorted.GetContainingRange(currentPosition);
        var nextData = operationFactor > 0 ? above : below;
        var timeUntil = (nextData?.Key - currentPosition) / operationFactor.Double();
        entity.Comp1.NextData = _gameTiming.CurTime + timeUntil is { } at && nextData?.Value is { } data
            ? (at, data)
            : null;

        // Update stopAt
        var tapeEnd = operationFactor.Double() > 0 ? entity.Comp2.Capacity : TimeSpan.Zero;
        entity.Comp1.StopAt = _gameTiming.CurTime + (tapeEnd - currentPosition) / operationFactor.Double();

        // Dirty just the playing component.
        Dirty(entity.Owner, entity.Comp1);
    }

    [SubscribeLocalEvent]
    private void OnGetInteractionVerbs(
        Entity<CassetteContainerComponent> entity,
        ref GetVerbsEvent<InteractionVerb> args
    )
    {
        if (!args.CanAccess || !args.CanComplexInteract)
            return;

        // Speed operations are available only while a cassette is inserted
        var hasTape = GetTape(entity) != null;

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("cent_loc-cassette-container-verb-play"),
            Act = () => StartPlaying(entity),
            Disabled = !hasTape || entity.Comp.State == CassetteOperation.Play,
        });

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("cent_loc-cassette-container-verb-stop"),
            Act = () => StopPlaying(entity),
            Disabled = !hasTape || entity.Comp.State == CassetteOperation.Stop,
        });

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("cent_loc-cassette-container-verb-fastforward"),
            Act = () => FastForward(entity),
            Disabled = !hasTape || entity.Comp.State == CassetteOperation.FastForward,
        });

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("cent_loc-cassette-container-verb-rewind"),
            Act = () => Rewind(entity),
            Disabled = !hasTape || entity.Comp.State == CassetteOperation.Rewind,
        });
    }

    [SubscribeLocalEvent]
    private void OnCassetteRemovedFromContainer(
        Entity<CassetteContainerComponent> container,
        ref EntRemovedFromContainerMessage args
    )
    {
        // If it's not the container we manage, ignore it.
        if (_gameTiming.ApplyingState || args.Container.ID != container.Comp.ContainerId)
            return;

        StopPlaying(container);

        // Forcibly stop the cassette. We need to explicitly do this since the cassette has already been removed from
        // the container.
        if (_cassetteQuery.ResolveOrNull(args.Entity) is { } cassette)
            SetTapeOperation(cassette, container, CassetteOperation.Stop);
    }

    /// Tries to find a <see cref="CassetteContainerComponent"/> containing this cassette.
    /// <remarks>
    /// <paramref name="xform"/> and <paramref name="meta"/> are included for the hot-path of the update loop.
    /// </remarks>
    private Entity<CassetteContainerComponent>? GetContainer(
        Entity<CassetteComponent> entity,
        TransformComponent? xform = null,
        MetaDataComponent? meta = null
    ) => _container.GetContainingEntity((entity, xform, meta), _cassetteContainerQuery);

    /// Sets operation state on the given container and its contained tape, if one exists.
    private void DoTapeOperation(Entity<CassetteContainerComponent> container, CassetteOperation operation)
    {
        if (GetTape(container) is { } tape)
        {
            container.Comp.State = operation;
            DirtyField(container.AsNullable(), nameof(CassetteContainerComponent.State));

            SetTapeOperation(tape, container, operation);
        }
        else if (operation == CassetteOperation.Stop)
        {
            // No tape, the only allowed operation change is stopping.
            container.Comp.State = operation;
            DirtyField(container.AsNullable(), nameof(CassetteContainerComponent.State));
        }
    }

    /// <summary>Sets <see cref="CassetteComponent.LastOperation"/> and related fields.</summary>
    /// <remarks>If the cassette is in a container, you should call <see cref="DoTapeOperation"/> instead.</remarks>
    private void SetTapeOperation(
        Entity<CassetteComponent> entity,
        Entity<CassetteContainerComponent>? container,
        CassetteOperation operation
    )
    {
        // Get the current position before modifying fields.
        var currentPosition = GetCurrentPosition(entity, container);

        entity.Comp.LastOperation = operation;
        entity.Comp.PositionAtLastOperation = currentPosition;
        entity.Comp.LastOperationAt = _gameTiming.CurTime;
        DirtyFields(
            entity.AsNullable(),
            meta: null,
            nameof(CassetteComponent.LastOperation),
            nameof(CassetteComponent.PositionAtLastOperation),
            nameof(CassetteComponent.LastOperationAt)
        );

        if (operation == CassetteOperation.Stop)
        {
            RemCompDeferred<PlayingCassetteComponent>(entity);
        }
        else
        {
            RecalculateNextDataAndStopAt((entity, EnsureComp<PlayingCassetteComponent>(entity), entity), null);
        }
    }
}
