using System.Collections.Generic;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Conditions;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Tutorial.Conditions;

public sealed partial class UseListenedConditionSystem : EventListenedConditionSystemBase<UseListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TutorialPlayerComponent, UserInteractUsingEvent>(OnUserInteractUsing);
        SubscribeLocalEvent<TutorialObservableComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUserInteractUsing(Entity<TutorialPlayerComponent> ent, ref UserInteractUsingEvent args)
    {
        RecordEvent(ent, args.Target, args.Used);
    }

    private void OnUseInHand(Entity<TutorialObservableComponent> ent, ref UseInHandEvent args)
    {
        if (!ent.Comp.Observers.Contains(args.User))
            return;

        RecordEvent(args.User, ent);
    }
}

/// <summary>
/// Checks if the player has used a target entity.
/// </summary>
public sealed partial class UseListenedCondition : EventListenedConditionBase<UseListenedCondition>
{
    public override bool ObserveAnyWithoutTarget => true;
}

