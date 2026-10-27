using System.Collections.Immutable;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;

namespace Content.Shared._CentRadio;

public abstract partial class OutputtingMicrodeviceSystem<T> : EntitySystem where T : Component, IMicrodevice
{
    [Dependency] private MicrodeviceContainerSystem _mdContainer = default!;

    protected void Output<TMicrodevice, TData>(
        Entity<TMicrodevice> source,
        TData data,
        MicrodeviceOutput output
    ) where TMicrodevice : Component, IMicrodevice where TData : IMicrodeviceData =>
        _mdContainer.Enqueue(source, data, output);
}

public abstract partial class ReceivingMicrodeviceSystem<T> : EntitySystem where T : Component, IMicrodevice
{
    [Dependency] private IGameTiming _gameTiming = default!;

    [MustCallBase]
    public override void Initialize()
    {
        SubscribeLocalEvent<T, UnhandledMicrodeviceInputEvent>(OnUnhandledMicrodeviceInput);
    }

    private void OnUnhandledMicrodeviceInput(Entity<T> entity, ref UnhandledMicrodeviceInputEvent args) =>
        OnUnhandledMicrodeviceInput(
            entity,
            args.Args.Input,
            args.Args.Data.AsRaw(SharedRandomExtensions.PredictedRandom(_gameTiming, GetNetEntity(entity)))
        );

    protected abstract void OnUnhandledMicrodeviceInput(
        Entity<T> entity,
        MicrodeviceInput input,
        ImmutableArray<char> rawData
    );
}
