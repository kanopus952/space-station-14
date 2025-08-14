using Content.Shared.Input;
using Content.Shared.Rotation;
using Content.Shared.Standing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Standing;

public sealed class StandingStateSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StandingStateComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(EntityUid uid, StandingStateComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var drawDepth = !component.Standing ? (int)DrawDepth.SmallMobs : (int)DrawDepth.Mobs;

        _sprite.SetDrawDepth((uid, args.Sprite), drawDepth);
    }
}
