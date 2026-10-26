using Content.Shared.Chat;
using Content.Shared.Speech;

namespace Content.Shared._CentRadio;

[RegisterComponent]
public sealed partial class MicrophoneComponent : Component, IMicrodevice
{
    public static readonly MicrodeviceOutput Output = new("audio");
}

public sealed partial class MicrophoneSystem : OutputtingMicrodeviceSystem<MicrophoneComponent>
{
    [Dependency] private MicrodeviceContainerSystem _mdContainer = default!;

    // TODO ListenAttemptEvent?

    [SubscribeLocalEvent]
    private void OnListen(Entity<MicrophoneComponent> entity, ref ListenEvent args)
    {
        if (entity.Owner == args.Source ||
            _mdContainer.ContainingMdContainerOrNull(entity) is var ((microphoneContainer, _), _) &&
            _mdContainer.ContainingMdContainerOrNull(args.Source) is var ((sourceContainer, _), _) &&
            microphoneContainer == sourceContainer)
        {
            // No feedback loops
            return;
        }

        var nameEv = new TransformSpeakerNameEvent(args.Source, Name(args.Source));
        RaiseLocalEvent(args.Source, nameEv);

        var name = Loc.GetString(
            "speech-name-relay",
            ("speaker", Name(entity)),
            ("originalName", nameEv.VoiceName)
        );

        Output(entity, new IMicrodeviceData.SpeechAudio(args.Message, name), MicrophoneComponent.Output);
    }
}
