using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

/// <summary>
/// Checks if specified item equipped into slot.
/// </summary>
public sealed partial class EquipItemConditionSystem : TutorialConditionSystem<InventoryComponent, EquipItemCondition>
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    protected override void Condition(Entity<InventoryComponent> entity, ref TutorialConditionEvent<EquipItemCondition> args)
    {
        foreach (var slot in entity.Comp.Slots)
        {
            if (slot.SlotFlags != args.Condition.Slot)
                continue;

            if (!_inventory.TryGetSlotEntity(entity, slot.Name, out var item))
                continue;

            if (!TryComp(item, out MetaDataComponent? meta))
                continue;

            if (meta.EntityPrototype == null)
                continue;

            if (meta.EntityPrototype.ID == args.Condition.Item)
                args.Result = true;
        }
    }
}
public sealed partial class EquipItemCondition : TutorialConditionBase<EquipItemCondition>
{
    [DataField]
    public EntProtoId Item;

    [DataField]
    public SlotFlags Slot;
}
