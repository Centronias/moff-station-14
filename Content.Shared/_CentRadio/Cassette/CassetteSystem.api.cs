using Content.Shared._Moffstation.Extensions;
using JetBrains.Annotations;

namespace Content.Shared._CentRadio.Cassette;

public sealed partial class CassetteSystem
{
    /// Gets the entity with <see cref="CassetteContainerComponent"/> contained within <paramref name="entity"/>'s
    /// designated container, or null, if no such entity exists.
    [PublicAPI] public Entity<CassetteComponent>? GetTape(Entity<CassetteContainerComponent> entity) =>
        entity.Comp.Container.ContainedEntity is { } contained
            ? _cassetteQuery.ResolveOrNull(contained)
            : null;

    /// <summary>
    /// Returns the current position of the cassette. The "position" is what time offset from the start is currently at
    /// the "head" of the cassette.
    /// </summary>
    /// <param name="entity">The cassette whose position is being calculated</param>
    /// <param name="container">The container the cassette is in. If null, the container will be looked up.</param>
    /// <remarks>The container is used to calculate the cassette's position in the case that it's being played (or
    /// experiencing some other operation).</remarks>
    [PublicAPI] public TimeSpan GetCurrentPosition(
        Entity<CassetteComponent> entity,
        Entity<CassetteContainerComponent>? container
    ) => entity.Comp.CurrentPosition(
        _gameTiming.CurTime,
        (container ?? GetContainer(entity))?.Comp.FastOperationSpeed
    );

    /// Writes <paramref name="data"/> to the <see cref="GetCurrentPosition"/> of the <see cref="CassetteComponent">cassette</see>
    /// in <paramref name="container"/>. Does nothing if the container is <see cref="CassetteOperation.Stop">stopped</see>.
    /// <remarks>
    /// Doesn't interact with <see cref="PlayingCassetteComponent.NextData"/> -- the assumption is that written data
    /// will always be "behind the tape head" when writing and moving.
    /// </remarks>
    [PublicAPI] public void Write(Entity<CassetteContainerComponent> container, IMicrodeviceData.Raw data)
    {
        // TODO How do we deal with overwriting data?

        if (GetTape(container) is not { } tape ||
            container.Comp.State == CassetteOperation.Stop)
            return;

        var currentPosition = GetCurrentPosition(tape, container);
        tape.Comp.Data[currentPosition] = data;
        DirtyField(tape.AsNullable(), nameof(CassetteComponent.Data));
    }

    /// Causes this container to start playing.
    [PublicAPI] public void StartPlaying(Entity<CassetteContainerComponent> container) =>
        DoTapeOperation(container, CassetteOperation.Play);

    /// Causes this container to stop playing.
    [PublicAPI] public void StopPlaying(Entity<CassetteContainerComponent> container) =>
        DoTapeOperation(container, CassetteOperation.Stop);

    /// Causes this container to advance quickly.
    [PublicAPI] public void FastForward(Entity<CassetteContainerComponent> container) =>
        DoTapeOperation(container, CassetteOperation.FastForward);

    /// Causes this container to play in reverse, quickly.
    [PublicAPI] public void Rewind(Entity<CassetteContainerComponent> container) =>
        DoTapeOperation(container, CassetteOperation.Rewind);
}
