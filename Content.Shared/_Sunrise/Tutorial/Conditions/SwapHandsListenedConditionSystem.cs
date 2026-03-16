using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared.Hands;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

public sealed partial class SwapHandsListenedConditionSystem : EventListenedConditionSystemBase<SwapHandsListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TutorialObservableComponent, HandSelectedEvent>(OnHandSelected);
    }

    private void OnHandSelected(Entity<TutorialObservableComponent> ent, ref HandSelectedEvent args)
    {
        if (!ent.Comp.Observers.Contains(args.User))
            return;

        RecordEvent(args.User, DefaultKey, ent);
    }
}

/// <summary>
/// Checks if the player has swapped their active hand (selecting a held item in the other hand).
/// </summary>
public sealed partial class SwapHandsListenedCondition : EventListenedConditionBase<SwapHandsListenedCondition>
{
    public override bool ObserveAnyWithoutTarget => true;
}
