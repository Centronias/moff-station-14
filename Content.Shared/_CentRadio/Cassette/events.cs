namespace Content.Shared._CentRadio.Cassette;

/// Raised on <see cref="Cassette"/> and <see cref="Container"/> when a non-<see cref="CassetteOperation.Stop"/>'ed
/// cassette is played (or rewinded, etc.) over a data chunk on the cassette.
[ByRefEvent]
public readonly record struct PlayingCassetteDataEvent(
    Entity<CassetteComponent> Cassette,
    Entity<CassetteContainerComponent> Container,
    IMicrodeviceData.Raw Data
);
