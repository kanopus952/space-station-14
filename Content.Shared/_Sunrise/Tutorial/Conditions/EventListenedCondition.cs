using System.Collections.Generic;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

/// <summary>
/// Base system for event-listened tutorial conditions.
/// </summary>
public abstract partial class EventListenedConditionSystemBase<TCondition> : TutorialConditionSystem<TutorialPlayerComponent, TCondition>
    where TCondition : EventListenedConditionBase<TCondition>
{
    [Dependency] protected readonly SharedTutorialSystem Tutorial = default!;
    protected static readonly EntProtoId AnyTarget = default;
    protected static readonly string CounterKey = typeof(TCondition).Name;

    protected override void Condition(Entity<TutorialPlayerComponent> entity, ref TutorialConditionEvent<TCondition> args)
    {
        if (args.Condition.Count <= 0)
        {
            args.Result = true;
            return;
        }

        if (!TryComp<TutorialTrackerComponent>(entity, out var tracker))
            return;

        var target = args.Condition.Target ?? AnyTarget;
        args.Result = HasCount(tracker.Counters, args.Condition.CounterKey, target, args.Condition.Count);
    }

    protected void RecordEvent(EntityUid user, EntityUid? primaryTarget = null, EntityUid? secondaryTarget = null)
    {
        RecordEvent(user, CounterKey, primaryTarget, secondaryTarget);
    }

    protected void RecordEvent(EntityUid user, string key, EntityUid? primaryTarget = null, EntityUid? secondaryTarget = null)
    {
        var tracker = EnsureComp<TutorialTrackerComponent>(user);
        IncrementConditionCounters((user, tracker), key, primaryTarget, secondaryTarget);
    }

    private void IncrementConditionCounters(Entity<TutorialTrackerComponent> ent,
        string key,
        EntityUid? primaryTarget,
        EntityUid? secondaryTarget)
    {
        IncrementCounter(ent.Comp.Counters, key, AnyTarget);

        if (Tutorial.TryGetPrototypeId(primaryTarget, out var primaryProto))
            IncrementCounter(ent.Comp.Counters, key, primaryProto);

        if (Tutorial.TryGetPrototypeId(secondaryTarget, out var secondaryProto) && secondaryProto != primaryProto)
            IncrementCounter(ent.Comp.Counters, key, secondaryProto);

        Dirty(ent);
    }

    private static void IncrementCounter(Dictionary<(string Key, EntProtoId Target), int> counters, string key, EntProtoId target)
    {
        var dictKey = (key, target);
        counters.TryGetValue(dictKey, out var count);
        counters[dictKey] = count + 1;
    }

    protected static bool HasCount(Dictionary<(string Key, EntProtoId Target), int> counters, string key, EntProtoId target, int count)
    {
        return counters.TryGetValue((key, target), out var value) && value >= count;
    }
}
