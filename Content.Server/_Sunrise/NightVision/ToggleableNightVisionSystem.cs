using Content.Shared._Sunrise.NightVision.Components;
using Content.Shared._Sunrise.NightVision.Events;
using Content.Shared.Actions;
using Content.Shared.NightVision;
using Content.Shared.Overlays;

namespace Content.Server._Sunrise.NightVision;

public sealed partial class ToggleableNightVisionSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedNightVisionSystem _nightVision = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleableNightVisionComponent, ComponentInit>(OnVisionInit);
        SubscribeLocalEvent<ToggleableNightVisionComponent, ComponentStartup>(OnVisionStartup);
        SubscribeLocalEvent<ToggleableNightVisionComponent, ComponentShutdown>(OnVisionShutdown);
        SubscribeLocalEvent<ToggleableNightVisionComponent, ToggleNightVisionEvent>(OnToggleNightVision);
    }

    private void OnVisionInit(Entity<ToggleableNightVisionComponent> ent, ref ComponentInit args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnVisionShutdown(Entity<ToggleableNightVisionComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.ActionEntity);
        _nightVision.SetEnabled((ent.Owner, null), false);
    }

    private void OnVisionStartup(Entity<ToggleableNightVisionComponent> ent, ref ComponentStartup args)
    {
        SetEnabled(ent, ent.Comp.Active);
    }

    private void OnToggleNightVision(Entity<ToggleableNightVisionComponent> ent, ref ToggleNightVisionEvent args)
    {
        if (args.Handled)
            return;

        SetEnabled(ent, !ent.Comp.Active);

        args.Handled = true;
    }

    private void SetEnabled(Entity<ToggleableNightVisionComponent> ent, bool enabled)
    {
        var vision = EnsureComp<NightVisionComponent>(ent);
        ent.Comp.Active = enabled;
        Dirty(ent);
        _nightVision.SetEnabled((ent.Owner, vision), enabled);
        _actions.SetToggled(ent.Comp.ActionEntity, enabled);
    }
}
