using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared.Hands;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

public sealed partial class PickupListenedConditionSystem : EventListenedConditionSystemBase<PickupListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TutorialPlayerComponent, DidEquipHandEvent>(OnDidEquipHand);
    }

    private void OnDidEquipHand(Entity<TutorialPlayerComponent> ent, ref DidEquipHandEvent args)
    {
        RecordEvent(ent, DefaultKey, args.Equipped);

        if (TryComp(ent, out TutorialTrackerComponent? tracker))
            Tutorial.TryObserveEntity(ent, args.Equipped, tracker);
    }
}

/// <summary>
/// Checks if the player has picked up a target entity into hands.
/// </summary>
public sealed partial class PickupListenedCondition : EventListenedConditionBase<PickupListenedCondition>
{
}
