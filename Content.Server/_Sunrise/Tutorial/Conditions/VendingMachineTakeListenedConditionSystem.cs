using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Components.Trackers;
using Content.Shared._Sunrise.Tutorial.Conditions;
using Content.Shared.VendingMachines;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Tutorial.Conditions;

public sealed partial class VendingMachineTakeListenedConditionSystem
    : EventListenedConditionSystemBase<VendingMachineTakeListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.BuiEvents<TutorialObservableComponent>(VendingMachineUiKey.Key, subs =>
        {
            subs.Event<VendingMachineEjectMessage>(OnVendingEject);
        });
    }

    protected override void Condition(Entity<TutorialPlayerComponent> entity, ref TutorialConditionEvent<VendingMachineTakeListenedCondition> args)
    {
        if (args.Condition.Count <= 0)
        {
            args.Result = true;
            return;
        }

        if (!TryComp<TutorialTrackerComponent>(entity, out var tracker))
            return;

        // ItemTarget takes priority: check the specific dispensed item prototype.
        // If unset, fall back to Target (vending machine proto) or AnyTarget.
        var target = (args.Condition.ItemTarget ?? args.Condition.Target) ?? AnyTarget;
        args.Result = HasCount(tracker.Counters, args.Condition.CounterKey, target, args.Condition.Count);
    }

    private void OnVendingEject(Entity<TutorialObservableComponent> ent, ref VendingMachineEjectMessage args)
    {
        if (args.Actor is not { Valid: true } actor)
            return;

        if (!ent.Comp.Observers.Contains(actor))
            return;

        // Record against vending machine proto (for Target matching) + AnyTarget
        RecordEvent(actor, DefaultKey, ent.Owner);

        // Also record against the dispensed item proto (for ItemTarget matching).
        // RecordEvent above already ensured TutorialTrackerComponent exists.
        if (!TryComp<TutorialTrackerComponent>(actor, out var tracker))
            return;

        var key = (DefaultKey, new EntProtoId(args.ID));
        tracker.Counters.TryGetValue(key, out var count);
        tracker.Counters[key] = count + 1;
        Dirty<TutorialTrackerComponent>((actor, tracker));
    }
}

/// <summary>
/// Checks if the player has taken an item from a vending machine.
/// <list type="bullet">
/// <item><term><see cref="ItemTarget"/></term><description>Match the dispensed item prototype (e.g. <c>DrinkJuiceOrange</c>). Takes priority over <see cref="EventListenedConditionBase{T}.Target"/>.</description></item>
/// <item><term><see cref="EventListenedConditionBase{T}.Target"/></term><description>Match the vending machine prototype (e.g. <c>VendingMachineDinnerware</c>).</description></item>
/// <item><term>Neither set</term><description>Any take from any vending machine satisfies the condition.</description></item>
/// </list>
/// </summary>
public sealed partial class VendingMachineTakeListenedCondition : EventListenedConditionBase<VendingMachineTakeListenedCondition>
{
    public override bool ObserveAnyWithoutTarget => true;

    /// <summary>
    /// The prototype ID of the item that must be dispensed.
    /// When set, the condition is satisfied only when this specific item is taken.
    /// </summary>
    [DataField]
    public EntProtoId? ItemTarget;
}
