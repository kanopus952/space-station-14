using Content.Shared.Chat;
using Content.Server.Chat.Systems;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.Animations;

namespace Content.Server.Speech;

public sealed partial class EmotesMenuSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<PlayEmoteMessage>(OnPlayEmote);
    }

    private void OnPlayEmote(PlayEmoteMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;
        if (!player.HasValue)
            return;

        if (!_prototypeManager.Resolve(msg.ProtoId, out var proto) || proto.ChatTriggers.Count == 0)
            return;

        // Sunrise-start
        if (!HasComp<EmoteAnimationComponent>(player))
            return;
        // Sunrise end

        _chat.TryEmoteWithChat(player.Value, msg.ProtoId);
    }
}
