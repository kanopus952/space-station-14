using System.Threading.Tasks;
using Content.Server._Sunrise.TTS;
using Content.Server.Chat.Managers;
using Content.Shared._Sunrise.TTS;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.EntitySystems;
using Content.Shared.Chat;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Tutorial;

/// <summary>
/// System for educating new players
/// </summary>
public sealed class TutorialSystem : SharedTutorialSystem
{
    [Dependency] private readonly TTSSystem _tts = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    private ISawmill _sawmill = default!;
    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("tutorial");
        SubscribeLocalEvent<TutorialPlayerComponent, TutorialStepChangedEvent>(OnStepChanged);
    }
    private async void OnStepChanged(EntityUid uid, TutorialPlayerComponent comp, TutorialStepChangedEvent args)
    {
        if (!_player.TryGetSessionByEntity(uid, out var session))
            return;

        if (!TryGetCurrentStep((uid, comp), out var step))
            return;

        if (!_proto.TryIndex(step.VoiceId, out var voice))
            return;

        var message = Loc.GetString(step.Sender) + Loc.GetString(step.ChatMessage);
        _chat.ChatMessageToOne(ChatChannel.Emotes, message, message, EntityUid.Invalid, false, session.Channel);

        var tts = await GenerateTtsForTutorial(step.TTSMessage, voice);

        if (tts == null)
            return;

        var ev = new PlayTTSEvent(tts);
        RaiseNetworkEvent(ev, Filter.SinglePlayer(session));
    }

    private async Task<byte[]?> GenerateTtsForTutorial(string text, TTSVoicePrototype voicePrototype)
    {
        try
        {
            return await _tts.GenerateTTS(text, voicePrototype, null);
        }
        catch (Exception e)
        {
            _sawmill.Error($"TTS System error in tutorial generation: {e.Message}");
        }
        return null;
    }
    public override void UpdateTimeCounter(Entity<TutorialPlayerComponent> ent, TimeSpan? endTime)
    {
        base.UpdateTimeCounter(ent, endTime);

        if (endTime == null)
        {
            RemComp<TutorialTimeCounterComponent>(ent);
            return;
        }

        var counter = EnsureComp<TutorialTimeCounterComponent>(ent);
        counter.EndTime = endTime;
        Dirty(ent, counter);
    }

}
