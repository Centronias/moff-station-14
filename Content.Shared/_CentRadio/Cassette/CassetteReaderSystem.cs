namespace Content.Shared._CentRadio.Cassette;

/// Implements the behavior of <see cref="CassetteReaderComponent"/>.
public sealed partial class CassetteReaderSystem : OutputtingMicrodeviceSystem<CassetteReaderComponent>
{
    /// Forwards <see cref="PlayingCassetteDataEvent"/> raised on microdevice containers to their contained microdevices.
    [SubscribeLocalEvent]
    private void OnMicrodeviceContainerPlayingCassetteData(
        Entity<MicrodeviceContainerComponent> container,
        ref PlayingCassetteDataEvent args
    )
    {
        foreach (var microdevice in MdContainer.GetContainedMicrodevices(container))
        {
            RaiseLocalEvent(microdevice, ref args);
        }
    }

    /// Adapts <see cref="PlayingCassetteDataEvent"/> to microdevice outputs.
    [SubscribeLocalEvent]
    private void OnPlayingCassetteData(
        Entity<CassetteReaderComponent> entity,
        ref PlayingCassetteDataEvent args
    ) => Output(entity, args.Data, CassetteReaderComponent.Output);
}
