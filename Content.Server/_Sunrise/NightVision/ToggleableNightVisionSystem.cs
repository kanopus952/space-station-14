using Content.Shared._Sunrise.NightVision.Components;
using Content.Shared._Sunrise.NightVision.Events;
using Content.Shared.Actions;

namespace Content.Server_Sunrise.NightVision;

public sealed class ToggleableNightVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ToggleableNightVisionComponent, ComponentInit>(OnVisionInit);
        SubscribeLocalEvent<ToggleableNightVisionComponent, ComponentShutdown>(OnVisionShutdown);
        SubscribeLocalEvent<ToggleableNightVisionComponent, ToggleNightVisionEvent>(OnToggleNightVision);
    }

    private void OnVisionInit(Entity<ToggleableNightVisionComponent> ent, ref ComponentInit args)
    {
        _actionsSystem.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnVisionShutdown(Entity<ToggleableNightVisionComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Comp.ActionEntity);
        RemComp<NightVisionComponent>(ent);
    }

    private void OnToggleNightVision(Entity<ToggleableNightVisionComponent> ent, ref ToggleNightVisionEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.Active = !ent.Comp.Active;

        if (ent.Comp.Active)
            EnsureComp<NightVisionComponent>(ent);
        else
            RemComp<NightVisionComponent>(ent);

        args.Handled = true;
    }
}
