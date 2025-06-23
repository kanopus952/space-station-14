using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Mech;

/// <inheritdoc/>
public sealed class MechSystem : SharedMechSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(EntityUid uid, MechComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var state = component.BaseState;
        var drawDepth = DrawDepth.Mobs;

        if (component.BrokenState != null
            && _appearance.TryGetData<bool>(uid, MechVisuals.Broken, out var broken)
            && broken)
        {
            state = component.BrokenState;
            drawDepth = DrawDepth.SmallMobs;
        }
        else if (component.OpenState != null
                 && _appearance.TryGetData<bool>(uid, MechVisuals.Open, out var open)
                 && open)
        {
            state = component.OpenState;
            drawDepth = DrawDepth.SmallMobs;
        }

        _sprite.LayerSetRsiState((uid, sprite), MechVisualLayers.Base, state);
        _sprite.SetDrawDepth((uid, sprite), (int)drawDepth);
    }
}
