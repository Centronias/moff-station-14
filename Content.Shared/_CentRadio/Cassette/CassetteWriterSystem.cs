using Content.Shared._Moffstation.Extensions;

namespace Content.Shared._CentRadio.Cassette;

/// Implements the behavior of <see cref="CassetteWriterComponent"/>.
public sealed partial class CassetteWriterSystem : ReceivingMicrodeviceSystem<CassetteWriterComponent>
{
    [Dependency] private CassetteSystem _cassette = default!;

    [Dependency] private EntityQuery<CassetteContainerComponent> _cassetteContainerQuery;

    [SubscribeLocalEvent]
    private void OnMicrodeviceInput(Entity<CassetteWriterComponent> entity, ref MicrodeviceInputEvent args)
    {
        if (args.Input != CassetteWriterComponent.Input)
        {
            Log.AssertOrLogError($"Unexpected input: \"{args.Input}\"");
            return;
        }

        if ((_cassetteContainerQuery.ResolveOrNull(entity.Owner, logMissing: false) ??
             _cassetteContainerQuery.ResolveOrNull(args.Container, logMissing: false)) is { } cassetteContainer)
        {
            _cassette.Write(cassetteContainer, Raw(entity, args.Data));
        }

        args.Handled = true;
    }

    protected override void OnUnhandledMicrodeviceInput(
        Entity<CassetteWriterComponent> entity,
        MicrodeviceInput input,
        IMicrodeviceData.Raw rawData,
        Entity<MicrodeviceContainerComponent> container
    ) => Log.AssertOrLogError($"Input should have been handled by {nameof(OnMicrodeviceInput)}");
}
