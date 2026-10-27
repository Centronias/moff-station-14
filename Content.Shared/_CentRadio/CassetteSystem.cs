using System.Net.Quic;
using Content.Shared._Moffstation.Extensions;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._CentRadio;

// Subscriptions and stuff
public sealed partial class CassetteSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;

    [Dependency] private SharedContainerSystem _container = default!;

    [Dependency] private EntityQuery<CassetteComponent> _cassetteQuery;
    [Dependency] private EntityQuery<CassetteContainerComponent> _cassetteContainerQuery;

    // TODO CENT If a cassette is ever written to while playing, we need to update the next-data because whatever's
    //  newly written could take that place.

    public override void Update(float frameTime)
    {
        // TODO CENT Get cassettes that are playing and raise events on the tick when we go past their "next data at"
        //  We'll also need to update the "next data at", so probably pull the component init stuff out into a new component.

        foreach (var entity in EntityQueryEnumerator<PlayingCassetteComponent, CassetteComponent>())
        {
            if (_gameTiming.CurTime >= entity.Comp1.NextData)
            {
                // It's tiiiiime!
                // TODO CENT Get the data, send the event.

                RecalculateNextData((entity, entity, entity));
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnPlayingCassetteComponentInit(Entity<PlayingCassetteComponent> entity, ref ComponentInit args)
    {
        if (_cassetteQuery.ResolveOrNull(entity) is not { } cassette)
            return;

        RecalculateNextData((entity, cassette, entity));
    }

    private void RecalculateNextData(Entity<CassetteComponent, PlayingCassetteComponent> entity)
    {
        var operationFactor =
            entity.Comp1.LastOperation.GetOperationFactor(GetContainer(entity)?.Comp.FastOperationSpeed);
        if (operationFactor == 0)
        {
            Log.AssertOrLogError(
                "Operation factor is 0, implying this cassette isn't actually playing. This shouldn't happen.");
            return;
        }

        var currentPosition = CurrentPosition(entity, null);
        var (below, above) = entity.Comp1.Data.GetContainingRange(currentPosition);
        var tapeTimeOfNextData = operationFactor > 0 ? above?.Key : below?.Key;
        var timeUntil = (tapeTimeOfNextData - currentPosition) / operationFactor.Double();
        entity.Comp2.NextData = _gameTiming.CurTime + timeUntil;
        Dirty(entity);
    }

    /// Tries to find a <see cref="CassetteContainerComponent"/> containing this cassette.
    private Entity<CassetteContainerComponent>? GetContainer(Entity<CassetteComponent> entity) =>
        _container.GetContainingEntity(entity.Owner, _cassetteContainerQuery);
}

// API Parts
public sealed partial class CassetteSystem
{
    [PublicAPI] public Entity<CassetteComponent>? GetTape<T>(
        Entity<T> entity
    ) where T : BaseCassetteContainerComponent => entity.Comp.Container.ContainedEntity is { } contained
        ? _cassetteQuery.ResolveOrNull(contained)
        : null;

    [PublicAPI] private TimeSpan CurrentPosition(
        Entity<CassetteComponent> entity,
        Entity<CassetteContainerComponent>? container
    )
    {
        var timeSinceLastOperation = _gameTiming.CurTime - entity.Comp.LastOperationAt;
        var fastOperationSpeed = (container ?? GetContainer(entity))?.Comp.FastOperationSpeed;
        var operationFactor = entity.Comp.LastOperation.GetOperationFactor(fastOperationSpeed).Double();
        return entity.Comp.PositionAtLastOperation + timeSinceLastOperation * operationFactor;
    }

    [PublicAPI] public void Write(
        Entity<CassetteComponent> entity,
        string data,
        Entity<CassetteContainerComponent>? container
    )
    {
        // TODO CENT How do we deal with overwriting data?

        var currentPosition = CurrentPosition(entity, container);
        entity.Comp.Data[currentPosition] = data;
    }

    [PublicAPI] public bool WriteToContained(Entity<CassetteContainerComponent?> container, string data)
    {
        if (_cassetteContainerQuery.ResolveOrNull(container) is not { } c ||
            GetTape(c) is not { } tape)
            return false;

        Write(tape, data, c);
        return true;
    }

    [PublicAPI] public void StartPlaying(Entity<CassetteContainerComponent> container) =>
        DoTapeOperation(container, CassetteOperation.Play, playing: true);

    [PublicAPI] public void StopPlaying(Entity<CassetteContainerComponent> container) =>
        DoTapeOperation(container, CassetteOperation.Stop, playing: false);

    [PublicAPI] public void FastForward(Entity<CassetteContainerComponent> container) =>
        DoTapeOperation(container, CassetteOperation.FastForward, playing: true);

    [PublicAPI] public void Rewind(Entity<CassetteContainerComponent> container) =>
        DoTapeOperation(container, CassetteOperation.Rewind, playing: true);

    private void DoTapeOperation(
        Entity<CassetteContainerComponent> container,
        CassetteOperation operation,
        bool playing
    )
    {
        container.Comp.State = operation;
        DirtyField(container.AsNullable(), nameof(CassetteContainerComponent.State));

        if (GetTape(container) is not { } tape)
            return;

        // Get the current position before modifying fields.
        var currentPosition = CurrentPosition(tape, container);

        tape.Comp.LastOperation = CassetteOperation.Play;
        tape.Comp.PositionAtLastOperation = currentPosition;
        tape.Comp.LastOperationAt = _gameTiming.CurTime;
        DirtyFields(
            tape.AsNullable(),
            meta: null,
            nameof(CassetteComponent.LastOperation),
            nameof(CassetteComponent.PositionAtLastOperation),
            nameof(CassetteComponent.LastOperationAt)
        );

        if (playing)
        {
            EnsureComp<PlayingCassetteComponent>(tape);
        }
        else
        {
            RemCompDeferred<PlayingCassetteComponent>(tape);
        }
    }
}
