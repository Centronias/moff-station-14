using System.Collections.Immutable;
using System.Text;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Chat;

namespace Content.Shared._CentRadio;

/// This component produces speech
[RegisterComponent]
public sealed partial class SpeakerComponent : Component, IMicrodevice
{
    public static readonly MicrodeviceInput Input = new("audio");
}

public sealed partial class SpeakerSystem : ReceivingMicrodeviceSystem<SpeakerComponent>
{
    [Dependency] private SharedChatSystem _chat = default!;

    protected override void OnUnhandledMicrodeviceInput(
        Entity<SpeakerComponent> entity,
        MicrodeviceInput input,
        IMicrodeviceData.Raw rawData
    )
    {
        if (input != SpeakerComponent.Input)
            return;

        _chat.TrySendInGameICMessage(
            entity,
            Encoding.ASCII.GetString(rawData.Data.AsSpan()),
            InGameICChatType.Whisper,
            ChatTransmitRange.GhostRangeLimit,
            checkRadioPrefix: false
        );
    }

    [SubscribeLocalEvent]
    private void OnMicrodeviceInput(
        Entity<SpeakerComponent> entity,
        ref MicrodeviceInputEvent<IMicrodeviceData.SpeechAudio> args
    )
    {
        if (args.Handled)
            return;

        if (args.Input != SpeakerComponent.Input)
        {
            Log.AssertOrLogError($"Unexpected input: \"{args.Input}\"");
            return;
        }

        // log to chat so people can identity the speaker/source, but avoid clogging ghost chat if there are many radios
        _chat.TrySendInGameICMessage(
            entity,
            args.Data.Message,
            InGameICChatType.Whisper,
            ChatTransmitRange.GhostRangeLimit,
            nameOverride: args.Data.Speaker,
            checkRadioPrefix: false
        );

        args.Handled = true;
    }
}
