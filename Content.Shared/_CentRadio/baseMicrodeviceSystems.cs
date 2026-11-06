using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;

namespace Content.Shared._CentRadio;

/// <summary>
/// An abstract system to help with defining <see cref="IMicrodevice"/>s which output information.
/// </summary>
/// <typeparam name="T">The type of <see cref="IMicrodevice"/> this system implements.</typeparam>
public abstract partial class OutputtingMicrodeviceSystem<T> : EntitySystem where T : Component, IMicrodevice
{
    [Dependency] protected MicrodeviceContainerSystem MdContainer = default!;

    /// <summary>
    /// Outputs a <see cref="IMicrodeviceData"/> value, causing other <see cref="IMicrodevice"/>s in the same container
    /// which are connected to <paramref name="source"/> to receive the data.
    /// </summary>
    /// <param name="source">The microdevice which is outputting data</param>
    /// <param name="data">The data being output</param>
    /// <param name="output">The output "port" on <paramref name="source"/> which is producing the data</param>
    /// <typeparam name="TMicrodevice">The type of <paramref name="source"/></typeparam>
    /// <typeparam name="TData">The type of <paramref name="data"/></typeparam>
    protected void Output<TMicrodevice, TData>(
        Entity<TMicrodevice> source,
        TData data,
        MicrodeviceOutput output
    ) where TMicrodevice : Component, IMicrodevice where TData : IMicrodeviceData =>
        MdContainer.Enqueue(source, data, output);
}

/// <summary>
/// An abstract system to help with defining <see cref="IMicrodevice"/>s which receiver information.
/// </summary>
/// <typeparam name="T">The type of <see cref="IMicrodevice"/> this system implements.</typeparam>
public abstract partial class ReceivingMicrodeviceSystem<T> : EntitySystem where T : Component, IMicrodevice
{
    [Dependency] private IGameTiming _gameTiming = default!;

    /// <summary>
    /// Transmutes <paramref name="data"/> to <see cref="IMicrodeviceData.Raw"/>, usually used for <see cref="OnUnhandledMicrodeviceInput(Entity{T},ref UnhandledMicrodeviceInputEvent)"/>.
    /// </summary>
    /// <param name="entity">"this" microdevice. Used to get <see cref="SharedRandomExtensions.PredictedRandom"/></param>
    /// <param name="data">The data that's being raw-ified</param>
    protected IMicrodeviceData.Raw Raw(Entity<T> entity, IMicrodeviceData data) =>
        data.AsRaw(SharedRandomExtensions.PredictedRandom(_gameTiming, GetNetEntity(entity)));

    [MustCallBase]
    public override void Initialize()
    {
        SubscribeLocalEvent<T, UnhandledMicrodeviceInputEvent>(OnUnhandledMicrodeviceInput);
    }

    private void OnUnhandledMicrodeviceInput(Entity<T> entity, ref UnhandledMicrodeviceInputEvent args) =>
        OnUnhandledMicrodeviceInput(
            entity,
            args.Input,
            Raw(entity, args.Data),
            args.Container
        );

    /// <summary>
    /// Handler for data which is not handled otherwise. Generally, this should produce some strange behavior, eg. a
    /// speaker will attempt to speak whatever the data is, regardless of it being or not being speech.
    /// </summary>
    /// <param name="entity">The receiver microdevice</param>
    /// <param name="input">The input "port" on which <paramref name="entity"/> received the data</param>
    /// <param name="rawData">The data itself</param>
    /// <param name="container">The container which contains <paramref name="entity"/></param>
    protected abstract void OnUnhandledMicrodeviceInput(
        Entity<T> entity,
        MicrodeviceInput input,
        IMicrodeviceData.Raw rawData,
        Entity<MicrodeviceContainerComponent> container
    );
}
