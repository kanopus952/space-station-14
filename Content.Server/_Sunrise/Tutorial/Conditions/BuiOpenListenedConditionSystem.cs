using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Conditions;

namespace Content.Server._Sunrise.Tutorial.Conditions;

public sealed partial class BuiOpenListenedConditionSystem
    : EventListenedConditionSystemBase<BuiOpenListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TutorialObservableComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
    }

    private void OnBoundUIOpened(Entity<TutorialObservableComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!ent.Comp.Observers.Contains(args.Actor))
            return;

        RecordEvent(args.Actor, DefaultKey, ent);
    }
}

/// <summary>
/// Checks if the player has opened any bound user interface on an observable entity.
/// Supports any entity or a specific prototype via <see cref="EventListenedConditionBase{T}.Target"/>.
/// </summary>
public sealed partial class BuiOpenListenedCondition : EventListenedConditionBase<BuiOpenListenedCondition>
{
    public override bool ObserveAnyWithoutTarget => true;
}
