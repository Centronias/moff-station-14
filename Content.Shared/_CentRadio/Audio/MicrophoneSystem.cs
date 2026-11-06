using Content.Shared.Chat;
using Content.Shared.Speech;

namespace Content.Shared._CentRadio.Audio;

public sealed partial class MicrophoneSystem : OutputtingMicrodeviceSystem<MicrophoneComponent>
{
    // TODO ListenAttemptEvent?

    [SubscribeLocalEvent]
    private void OnListen(Entity<MicrophoneComponent> entity, ref ListenEvent args)
    {
        if (entity.Owner == args.Source ||
            MdContainer.GetContainingMdContainerOrNull(entity) is var ((microphoneContainer, _), _) &&
            MdContainer.GetContainingMdContainerOrNull(args.Source) is var ((sourceContainer, _), _) &&
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
