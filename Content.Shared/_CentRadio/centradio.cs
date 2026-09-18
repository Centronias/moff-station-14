using System.Linq;
using Content.Shared.Chat;
using Content.Shared.Speech;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared._CentRadio;

/*
 * Cent Radio - Microdevice radio devices for depth
 */

// MICRODEVICE COMPONENTS BELOW

public interface IMicrodevice
{
    // TODO CENT Generic "I don't know what this is" serial data for use in "I don't know what this is" handlers.
}

[DataRecord]
public readonly partial record struct MicrodeviceInput(string Name);

[DataRecord]
public readonly partial record struct MicrodeviceOutput(string Name);

[ByRefEvent]
public readonly record struct MicrodeviceInputEvent(MicrodeviceInput Input, IMicrodeviceData Data)
{
    [ByRefEvent]
    public readonly record struct Typed<T>(MicrodeviceInput Input, T Data) where T : IMicrodeviceData;
}

public interface IMicrodeviceData
{
    // TODO Including the entity here is probably bad
    readonly record struct SpeechAudio(string Message, EntityUid? Speaker) : IMicrodeviceData;
}

public abstract partial class BaseMicrodeviceSystem : EntitySystem
{
    [Dependency] protected MicrodeviceContainerSystem _mdContainer = default!;

    public void Output<TMicrodevice, TData>(
        Entity<TMicrodevice> source,
        TData data,
        MicrodeviceOutput output
    )
        where TMicrodevice : Component, IMicrodevice where TData : IMicrodeviceData
    {
        if (_mdContainer.ContainingMdContainerOrNull<TMicrodevice>((source, source)) is not var (mdContainer, idx))
            return;

        foreach (var (receiver, input) in mdContainer.Comp.GetConnections(idx, output))
        {
            var typedEv = new MicrodeviceInputEvent.Typed<TData>(input, data);
            RaiseLocalEvent(receiver, ref typedEv);

            var ev = new MicrodeviceInputEvent(input, data);
            RaiseLocalEvent(receiver, ref ev);
        }
    }
}

/// Creates a microcomponent "network" in the owner entity.
[RegisterComponent]
public sealed partial class MicrodeviceContainerComponent : Component
{
    [DataField("containers", required: true)]
    public string[] ContainerIds = default!;

    [ViewVariables] public ContainerSlot[] Containers = [];

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContainerSlot? ContainerOrNull(int index) => Containers.TryGetValue(index, out var ret) ? ret : null;

    [DataField("connections")]
    public List<(
        (int containerIndex, MicrodeviceOutput) output,
        (int containerIndex, MicrodeviceInput) input
        )> RawConnections = new();

    [ViewVariables]
    public List<(
        (ContainerSlot slot, MicrodeviceOutput output) output,
        (ContainerSlot slot, MicrodeviceInput input) input
        )> Connections = new();

    [ViewVariables]
    public readonly Queue<(IMicrodeviceData, int containerIndex, MicrodeviceOutput)> Queue = new();
}

public sealed partial class MicrodeviceContainerSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    [Dependency] private EntityQuery<MicrodeviceContainerComponent> _mdContainerQuery;

    public (Entity<MicrodeviceContainerComponent> containingEnt, int containerIndex)? ContainingMdContainerOrNull<T>(
        Entity<T, MetaDataComponent?, TransformComponent?> entity
    ) where T : Component, IMicrodevice
    {
        if (!Resolve(entity, ref entity.Comp2) ||
            !_container.IsEntityInContainer(entity, entity.Comp2) ||
            !Resolve(entity, ref entity.Comp3) ||
            entity.Comp3.ParentUid is not { Valid: true } parent ||
            !_container.TryGetContainingContainer(parent, entity, out var container) ||
            !_mdContainerQuery.TryComp(parent, out var mdContainer) ||
            mdContainer.ContainerIds.IndexOf(container.ID) is var idx && idx == -1)
            return null;

        return ((parent, mdContainer), idx);
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

    public void Enqueue<TMicrodevice, TData>(
        Entity<TMicrodevice> source,
        TData data,
        MicrodeviceOutput output
    ) where TMicrodevice : Component, IMicrodevice where TData : IMicrodeviceData
    {
        if (ContainingMdContainerOrNull<TMicrodevice>((source, source)) is not var (mdContainer, idx))
            return;

        mdContainer.Comp.Queue.Enqueue((data, idx, output));
    }

    public override void Update(float frameTime)
    {
        foreach (var (receiver, input) in mdContainer.Comp.GetConnections(idx, output))
        {
            var typedEv = new MicrodeviceInputEvent.Typed<TData>(input, data);
            RaiseLocalEvent(receiver, ref typedEv);

            var ev = new MicrodeviceInputEvent(input, data);
            RaiseLocalEvent(receiver, ref ev);
        }
    }
}

public sealed partial class MicrodeviceContainerComponent
{
    public IEnumerable<(EntityUid entity, MicrodeviceInput input)> GetConnections(
        EntityUid entity,
        MicrodeviceOutput output
    )
    {
        foreach (var (connectedOutput, input) in Connections)
        {
            if (entity == connectedOutput.slot.ContainedEntity &&
                output == connectedOutput.output &&
                input.slot.ContainedEntity is { } connected)
            {
                yield return (connected, input.input);
            }
        }
    }

    public IEnumerable<(EntityUid entity, MicrodeviceInput input)> GetConnections(
        int containerIndex,
        MicrodeviceOutput output
    )
    {
        if (ContainerOrNull(containerIndex) is not { } container)
            yield break;

        foreach (var (connectedOutput, input) in Connections)
        {
            if (container == connectedOutput.slot &&
                output == connectedOutput.output &&
                input.slot.ContainedEntity is { } connected)
            {
                yield return (connected, input.input);
            }
        }
    }
}

// COMMON COMPONENTS BELOW

[RegisterComponent]
public sealed partial class MicrophoneComponent : Component, IMicrodevice
{
    public static readonly MicrodeviceOutput Output = new("audio");
}

public sealed partial class MicrophoneSystem : BaseMicrodeviceSystem
{
    // TODO ListenAttemptEvent?

    [SubscribeLocalEvent]
    private void OnListen(Entity<MicrophoneComponent> entity, ref ListenEvent args)
    {
        if (_mdContainer.ContainingMdContainerOrNull<MicrophoneComponent>((entity, entity)) is (var container, _))
        {
            if (container.Owner == args.Source)
            {
                // No feedback loops
                return;
            }
        }
        else
        {
            Output(entity, new IMicrodeviceData.SpeechAudio(args.Message, args.Source), MicrophoneComponent.Output);
        }
    }
}

/// This component produces speech
[RegisterComponent]
public sealed partial class SpeakerComponent : Component, IMicrodevice
{
    public static readonly MicrodeviceInput Input = new("audio");
}

public sealed partial class SpeakerSystem : EntitySystem
{
    [Dependency] private SharedChatSystem _chat = default!;

    [SubscribeLocalEvent]
    private void OnMicrodeviceInput(
        Entity<SpeakerComponent> entity,
        ref MicrodeviceInputEvent.Typed<IMicrodeviceData.SpeechAudio> args
    )
    {
        if (args.Input != SpeakerComponent.Input)
            // TODO CENT Complain
            return;

        string name;
        if (args.Data.Speaker is { } speaker)
        {
            var nameEv = new TransformSpeakerNameEvent(speaker, Name(speaker));
            RaiseLocalEvent(speaker, nameEv);

            name = Loc.GetString(
                "speech-name-relay",
                ("speaker", Name(entity)),
                ("originalName", nameEv.VoiceName)
            );
        }
        else
        {
            name = Name(entity);
        }

        // log to chat so people can identity the speaker/source, but avoid clogging ghost chat if there are many radios
        _chat.TrySendInGameICMessage(
            entity,
            args.Data.Message,
            InGameICChatType.Whisper,
            ChatTransmitRange.GhostRangeLimit,
            nameOverride: name,
            checkRadioPrefix: false
        );
    }
}

// CASSETTE COMPONENTS BELOW

/// Kinda gross component that exists to make a system tie the microdevices together.
[RegisterComponent]
public sealed partial class CassetteManagerComponent : Component;

[RegisterComponent]
public sealed partial class CassetteWriterComponent : Component, IMicrodevice
{
    [DataField("container", required: true)]
    public string ContainerId = default!;

    [ViewVariables] public ContainerSlot Container = default!;
}

[RegisterComponent]
public sealed partial class CassetteReaderComponent : Component, IMicrodevice
{
    [DataField("container", required: true)]
    public string ContainerId = default!;

    [ViewVariables] public ContainerSlot Container = default!;
}
