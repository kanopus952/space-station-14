using System.Collections.Generic;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

public sealed partial class EquipListenedConditionSystem : EventListenedConditionSystemBase<EquipListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TutorialPlayerComponent, DidEquipEvent>(OnDidEquip);
        SubscribeLocalEvent<TutorialPlayerComponent, DidEquipHandEvent>(OnDidEquipHand);
    }

    private void OnDidEquip(Entity<TutorialPlayerComponent> ent, ref DidEquipEvent args)
    {
        RecordEvent(ent, args.Equipment);
        RecordEvent(ent, EquipListenedCondition.GetSlotKey(args.SlotFlags), args.Equipment);

        if (TryComp(ent, out TutorialTrackerComponent? tracker))
            Tutorial.TryObserveEntity(ent, args.Equipment, tracker);
    }

    private void OnDidEquipHand(Entity<TutorialPlayerComponent> ent, ref DidEquipHandEvent args)
    {
        RecordEvent(ent, args.Equipped);

        if (TryComp(ent, out TutorialTrackerComponent? tracker))
            Tutorial.TryObserveEntity(ent, args.Equipped, tracker);
    }
}

/// <summary>
/// Checks if the player has equipped a target entity.
/// </summary>
public sealed partial class EquipListenedCondition : EventListenedConditionBase<EquipListenedCondition>
{
    [DataField]
    public SlotFlags? Slot;

    public override string CounterKey => Slot == null
        ? base.CounterKey
        : string.Concat(base.CounterKey, ":", Slot.Value);

    public static string GetSlotKey(SlotFlags slot)
    {
        return string.Concat(nameof(EquipListenedCondition), ":", slot);
    }
}
