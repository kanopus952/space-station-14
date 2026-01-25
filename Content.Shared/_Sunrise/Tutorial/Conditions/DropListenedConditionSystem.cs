using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared.Interaction.Events;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

public sealed partial class DropListenedConditionSystem : EventListenedConditionSystemBase<DropListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TutorialObservableComponent, DroppedEvent>(OnDropped);
    }

    private void OnDropped(Entity<TutorialObservableComponent> ent, ref DroppedEvent args)
    {
        if (!ent.Comp.Observers.Contains(args.User))
            return;

        RecordEvent(args.User, ent);
    }
}

/// <summary>
/// Checks if the player has dropped a target entity.
/// </summary>
public sealed partial class DropListenedCondition : EventListenedConditionBase<DropListenedCondition>
{
    public override bool ObserveAnyWithoutTarget => true;
}
