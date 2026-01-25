using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

/// <summary>
/// Checks if the player is holding a specific item in their hands.
/// </summary>
public sealed partial class HoldInHandsConditionSystem : TutorialConditionSystem<HandsComponent, HoldInHandsCondition>
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    protected override void Condition(Entity<HandsComponent> entity, ref TutorialConditionEvent<HoldInHandsCondition> args)
    {
        foreach (var held in _hands.EnumerateHeld(entity.Owner))
        {
            var proto = Prototype(held);

            if (proto?.ID == null)
                return;

            if (proto.ID == args.Condition.Item)
            {
                args.Result = true;
                return;
            }
        }
    }
}

public sealed partial class HoldInHandsCondition : TutorialConditionBase<HoldInHandsCondition>
{
    [DataField]
    public EntProtoId Item;
}
