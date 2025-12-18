using System.Reflection.Metadata.Ecma335;
using Content.Shared._Sunrise.Tutorial.Conditions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;
///<summary>
/// Checks if specified item equipped into slot
/// </summary>
/// <inheritdoc cref="EquipItemConditionSystem{InventoryComponent, EquipItemCondition}"/>
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
                return;

            if (meta.EntityPrototype.ID == args.Condition.Item)
                args.Result = true;
        }
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class EquipItemCondition : TutorialConditionBase<EquipItemCondition>
{
    public EntProtoId Item;
    public SlotFlags Slot;
}
