using System.Collections.Generic;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared.Interaction;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

public sealed partial class InteractListenedConditionSystem : EventListenedConditionSystemBase<InteractListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TutorialPlayerComponent, UserInteractHandEvent>(OnUserInteractHand);
    }

    private void OnUserInteractHand(Entity<TutorialPlayerComponent> ent, ref UserInteractHandEvent args)
    {
        RecordEvent(ent, args.Target);
    }
}

/// <summary>
/// Checks if the player has interacted with a target entity.
/// </summary>
public sealed partial class InteractListenedCondition : EventListenedConditionBase<InteractListenedCondition>
{
}
