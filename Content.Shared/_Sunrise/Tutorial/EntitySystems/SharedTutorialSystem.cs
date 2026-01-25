using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Conditions;
using Content.Shared._Sunrise.Tutorial.Events;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Tutorial.EntitySystems;

/// <summary>
/// System for educating new players
/// </summary>
public abstract class SharedTutorialSystem : EntitySystem
{
    [Dependency] private readonly SharedTutorialConditionsSystem _tutorial = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TutorialPlayerComponent, ComponentInit>(OnComponentInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TutorialPlayerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Completed)
                continue;

            if (comp.EndTime != null && _timing.CurTime > comp.EndTime)
            {
                EndTutorial((uid, comp));
                continue;
            }

            TryCheckCondition((uid, comp));
        }
    }
    private void OnComponentInit(Entity<TutorialPlayerComponent> ent, ref ComponentInit args)
    {
        if (!TryGetCurrentStep(ent, out var step))
            return;

        ent.Comp.EndTime = _timing.CurTime + _proto.Index(ent.Comp.SequenceId).Duration;
        UpdateTimeCounter(ent, ent.Comp.EndTime);
        OnStepChanged(ent, step);
    }

    public void TryCheckCondition(Entity<TutorialPlayerComponent> ent)
    {
        if (!TryGetCurrentStep(ent, out var step))
            return;

        if (!_tutorial.TryConditions(ent, step.Preconditions.ToArray()))
        {
            ClearTutorialBubble(ent);
            if (step.PreconditionFailStep != null)
            {
                Advance(ent, step.PreconditionFailStep.Value);
                return;
            }

            Advance(ent);
            return;
        }

        if (ent.Comp.CurrentBubbleTarget == null && step.Bubble != null)
            UpdateTutorialBubble(ent, step);

        foreach (var condition in step.Conditions)
        {
            if (!_tutorial.TryCondition(ent, condition))
                return;
        }

        if (step.AnyConditions.Count > 0)
        {
            var any = false;
            foreach (var condition in step.AnyConditions)
            {
                if (_tutorial.TryCondition(ent, condition))
                {
                    any = true;
                    break;
                }
            }

            if (!any)
                return;
        }

        Advance(ent);
    }

    public void Advance(Entity<TutorialPlayerComponent> ent, ProtoId<TutorialStepPrototype>? stepId = null)
    {
        if (!_proto.TryIndex(ent.Comp.SequenceId, out var sequence))
            return;

        if (stepId == null)
        {
            ent.Comp.StepIndex++;

            if (ent.Comp.StepIndex >= sequence.Steps.Count)
                return;

            stepId = sequence.Steps.ElementAt(ent.Comp.StepIndex);
        }
        else
        {
            var index = sequence.Steps.IndexOf(stepId.Value);
            if (index < 0 || ent.Comp.StepIndex == index)
                return;

            ent.Comp.StepIndex = index;
        }

        if (!_proto.TryIndex(stepId.Value, out var step))
            return;

        OnStepChanged(ent, step);
    }

    private void OnStepChanged(Entity<TutorialPlayerComponent> ent, TutorialStepPrototype step)
    {
        ResetTracking(ent);
        var ev = new TutorialStepChangedEvent();
        RaiseLocalEvent(ent, ev);

        if (_tutorial.TryConditions(ent, step.Preconditions.ToArray()))
            UpdateTutorialBubble(ent, step);
    }

    public void EndTutorial(Entity<TutorialPlayerComponent> ent)
    {
        if (ent.Comp.Completed)
            return;

        ent.Comp.Completed = true;

        if (ent.Comp.CurrentBubbleTarget is { } oldTarget && Exists(oldTarget))
            RemComp<TutorialBubbleComponent>(oldTarget);

        ent.Comp.CurrentBubbleTarget = null;

        ClearTracking(ent);
        UpdateTimeCounter(ent, null);

        var ev = new TutorialEndedEvent();
        RaiseLocalEvent(ent, ev);
        Dirty(ent);
    }
    private void ResetTracking(Entity<TutorialPlayerComponent> ent)
    {
        var tracker = EnsureComp<TutorialTrackerComponent>(ent);
        tracker.Counters.Clear();
        UpdateObservedEntities(ent, tracker);
    }

    private void ClearTracking(Entity<TutorialPlayerComponent> ent)
    {
        if (!TryComp<TutorialTrackerComponent>(ent, out var tracker))
            return;

        foreach (var observed in tracker.ObservedEntities)
        {
            RemoveObserver(ent, observed);
        }

        tracker.ObservedEntities.Clear();
        tracker.TargetPrototypes.Clear();
        tracker.Counters.Clear();
        Dirty(ent, tracker);
    }

    private void UpdateObservedEntities(Entity<TutorialPlayerComponent> ent, TutorialTrackerComponent tracker)
    {
        foreach (var observed in tracker.ObservedEntities)
        {
            RemoveObserver(ent, observed);
        }

        tracker.ObservedEntities.Clear();
        tracker.TargetPrototypes.Clear();

        if (!TryGetCurrentStep(ent, out var step))
            return;

        foreach (var condition in step.Conditions)
        {
            if (condition is not IEventListenedCondition listened)
                continue;

            if (listened.Target != null)
            {
                tracker.TargetPrototypes.Add(listened.Target.Value);
                continue;
            }

            if (listened.ObserveAnyWithoutTarget)
                EnsureObserveAnyCounter(tracker, listened.ObserveKey);
        }

        foreach (var condition in step.AnyConditions)
        {
            if (condition is not IEventListenedCondition listened)
                continue;

            if (listened.Target != null)
            {
                tracker.TargetPrototypes.Add(listened.Target.Value);
                continue;
            }

            if (listened.ObserveAnyWithoutTarget)
                EnsureObserveAnyCounter(tracker, listened.ObserveKey);
        }

        foreach (var condition in step.Preconditions)
        {
            if (condition is not IEventListenedCondition listened)
                continue;

            if (listened.Target != null)
            {
                tracker.TargetPrototypes.Add(listened.Target.Value);
                continue;
            }

            if (listened.ObserveAnyWithoutTarget)
                EnsureObserveAnyCounter(tracker, listened.ObserveKey);
        }

        ObserveNearbyEntities(ent, tracker, step);
        ObserveEquippedEntities(ent, tracker);
    }

    private void ObserveNearbyEntities(EntityUid user, TutorialTrackerComponent tracker, TutorialStepPrototype step)
    {
        if (tracker.TargetPrototypes.Count == 0 && !ObservesAny(tracker))
            return;

        foreach (var uid in _lookupSystem.GetEntitiesInRange(user, step.ObserveRange))
        {
            TryObserveEntity(user, uid, tracker);
        }
    }

    private void ObserveEquippedEntities(EntityUid user, TutorialTrackerComponent tracker)
    {
        if (tracker.TargetPrototypes.Count == 0 && !ObservesAny(tracker))
            return;

        if (TryComp(user, out HandsComponent? hands))
        {
            foreach (var held in _hands.EnumerateHeld((user, hands)))
            {
                TryObserveEntity(user, held, tracker);
            }
        }

        if (!TryComp(user, out InventoryComponent? inventory))
            return;

        foreach (var slot in inventory.Slots)
        {
            if (!_inventory.TryGetSlotEntity(user, slot.Name, out var item, inventory))
                continue;

            TryObserveEntity(user, item.Value, tracker);
        }
    }

    public void TryObserveEntity(EntityUid user, EntityUid target, TutorialTrackerComponent tracker)
    {
        if (!ShouldObserveEntity(target, tracker))
            return;

        if (!tracker.ObservedEntities.Add(target))
            return;

        var observable = EnsureComp<TutorialObservableComponent>(target);
        observable.Observers.Add(user);
    }

    private void RemoveObserver(EntityUid user, EntityUid target)
    {
        if (!TryComp(target, out TutorialObservableComponent? observable))
            return;

        observable.Observers.Remove(user);
        if (observable.Observers.Count == 0)
            RemComp<TutorialObservableComponent>(target);
    }

    private bool ShouldObserveEntity(EntityUid target, TutorialTrackerComponent tracker)
    {
        if (ObservesAny(tracker))
            return true;

        if (!TryGetPrototypeId(target, out var protoId))
            return false;

        return tracker.TargetPrototypes.Contains(protoId);
    }

    private static bool ObservesAny(TutorialTrackerComponent tracker)
    {
        return HasAnyObserveCounter(tracker);
    }

    private static bool HasAnyObserveCounter(TutorialTrackerComponent tracker)
    {
        foreach (var (key, target) in tracker.Counters.Keys)
        {
            if (!target.Equals(default(EntProtoId)))
                continue;

            if (key.EndsWith(EventListenedConditionKeys.ObserveSuffix, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static void EnsureObserveAnyCounter(TutorialTrackerComponent tracker, string key)
    {
        var counterKey = (key, default(EntProtoId));
        if (!tracker.Counters.ContainsKey(counterKey))
            tracker.Counters[counterKey] = 0;
    }

    public bool TryGetCurrentStep(Entity<TutorialPlayerComponent> ent, [NotNullWhen(true)] out TutorialStepPrototype? step)
    {
        step = null;

        if (!_proto.TryIndex(ent.Comp.SequenceId, out var sequence))
            return false;

        if (ent.Comp.StepIndex < 0 || ent.Comp.StepIndex >= sequence.Steps.Count)
            return false;

        var stepId = sequence.Steps[ent.Comp.StepIndex];
        return _proto.TryIndex(stepId, out step);
    }
    public bool TryGetPrototypeId(EntityUid? uid, out EntProtoId protoId)
    {
        if (uid is not { } target)
        {
            protoId = default;
            return false;
        }

        var proto = Prototype(target);

        if (proto == null)
        {
            protoId = default;
            return false;
        }

        protoId = proto.ID;
        return true;
    }
    private void UpdateTutorialBubble(Entity<TutorialPlayerComponent> ent, TutorialStepPrototype step)
    {
        if (step.Bubble == null)
            return;

        var target = step.Bubble.Target.Type switch
        {
            TutorialBubbleTargetType.Self => ent,

            TutorialBubbleTargetType.Entity =>
                TryFindBubbleEntity(ent, step),

            _ => null
        };

        if (target == null || !Exists(target.Value))
        {
            ent.Comp.CurrentBubbleTarget = null;
            return;
        }

        if (ent.Comp.CurrentBubbleTarget is { } oldTarget && oldTarget == target.Value)
        {
            var existing = EnsureComp<TutorialBubbleComponent>(oldTarget);
            existing.Instruction = step.Bubble.Text;
            Dirty(oldTarget, existing);
            Dirty(ent);
            return;
        }

        if (ent.Comp.CurrentBubbleTarget is { } previous && Exists(previous))
            RemComp<TutorialBubbleComponent>(previous);

        var bubble = EnsureComp<TutorialBubbleComponent>(target.Value);
        bubble.Instruction = step.Bubble.Text;

        ent.Comp.CurrentBubbleTarget = target;

        Dirty(target.Value, bubble);
        Dirty(ent);
    }

    private void ClearTutorialBubble(Entity<TutorialPlayerComponent> ent)
    {
        if (ent.Comp.CurrentBubbleTarget is { } oldTarget && Exists(oldTarget))
            RemComp<TutorialBubbleComponent>(oldTarget);

        ent.Comp.CurrentBubbleTarget = null;
    }
    private EntityUid? TryFindBubbleEntity(EntityUid uid, TutorialStepPrototype proto)
    {
        if (proto.Bubble == null)
            return null;

        var targetProtoId = proto.Bubble.Target.Prototype;

        var origin = _transform.GetMapCoordinates(uid);
        var best = EntityUid.Invalid;
        var bestDistSq = float.MaxValue;

        foreach (var ent in _lookupSystem.GetEntitiesInRange(uid, proto.ObserveRange))
        {
            var meta = MetaData(ent);

            if (meta.EntityPrototype == null)
                continue;

            if (meta.EntityPrototype.ID != targetProtoId)
                continue;

            var pos = _transform.GetMapCoordinates(ent);
            var d = (pos.Position - origin.Position).LengthSquared();

            if (d < bestDistSq)
            {
                bestDistSq = d;
                best = ent;
            }
        }

        return best;
    }
    public virtual void UpdateTimeCounter(Entity<TutorialPlayerComponent> ent, TimeSpan? endTime)
    {
    }
}
