using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

public sealed partial class AttackListenedConditionSystem : EventListenedConditionSystemBase<AttackListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TutorialObservableComponent, AttackedEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<TutorialObservableComponent> ent, ref AttackedEvent args)
    {
        if (!ent.Comp.Observers.Contains(args.User))
            return;

        RecordEvent(args.User, ent, args.Used);
    }
}

public sealed partial class AttackListenedCondition : EventListenedConditionBase<AttackListenedCondition>
{
    public override bool ObserveAnyWithoutTarget => true;
}
