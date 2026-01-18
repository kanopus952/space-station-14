using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Conditions;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
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
            if (_timing.CurTime > comp.EndTime)
            {
                comp.Completed = true;
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

        foreach (var condition in step.Conditions)
        {
            if (!_tutorial.TryCondition(ent, condition))
                return;
        }

        Advance(ent);
    }

    public void Advance(Entity<TutorialPlayerComponent> ent)
    {
        if (!_proto.TryIndex(ent.Comp.SequenceId, out var sequence))
            return;

        ent.Comp.StepIndex++;

        if (ent.Comp.StepIndex >= sequence.Steps.Count)
            return;

        var stepId = sequence.Steps.ElementAt(ent.Comp.StepIndex);

        if (!_proto.TryIndex(stepId, out var step))
            return;

        OnStepChanged(ent, step);
    }

    private void OnStepChanged(Entity<TutorialPlayerComponent> ent, TutorialStepPrototype step)
    {
        ResetTracking(ent);
        var ev = new TutorialStepChangedEvent();
        RaiseLocalEvent(ent, ev);

        UpdateTutorialBubble(ent, step);
    }

    private void EndTutorial(Entity<TutorialPlayerComponent> ent)
    {
        OnTutorialCompleted(ent);
        RemCompDeferred<TutorialPlayerComponent>(ent);
        UpdateTimeCounter(ent, null);
    }

    protected virtual void OnTutorialCompleted(Entity<TutorialPlayerComponent> ent)
    {
    }
    private void ResetTracking(Entity<TutorialPlayerComponent> ent)
    {
        var tracker = EnsureComp<TutorialTrackerComponent>(ent.Owner);
        tracker.Counters.Clear();
        UpdateObservedEntities(ent, tracker);
    }

    private void UpdateObservedEntities(Entity<TutorialPlayerComponent> ent, TutorialTrackerComponent tracker)
    {
        foreach (var observed in tracker.ObservedEntities)
        {
            RemoveObserver(ent.Owner, observed);
        }

        tracker.ObservedEntities.Clear();
        tracker.TargetPrototypes.Clear();
        tracker.ObserveAnyUseInHand = false;
        tracker.ObserveAnyDrop = false;
        tracker.ObserveAnyAttack = false;
        tracker.ObserveAnyExamine = false;

        if (!TryGetCurrentStep(ent, out var step))
            return;

        foreach (var condition in step.Conditions)
        {
            if (condition is not EventListenedCondition listened)
                continue;

            if (listened.Target != null)
            {
                tracker.TargetPrototypes.Add(listened.Target.Value);
                continue;
            }

            switch (listened.Event)
            {
                case TutorialEventType.Use:
                    tracker.ObserveAnyUseInHand = true;
                    break;
                case TutorialEventType.Drop:
                    tracker.ObserveAnyDrop = true;
                    break;
                case TutorialEventType.Attack:
                    tracker.ObserveAnyAttack = true;
                    break;
                case TutorialEventType.Examine:
                    tracker.ObserveAnyExamine = true;
                    break;
            }
        }

        ObserveNearbyEntities(ent.Owner, tracker, step);
        ObserveEquippedEntities(ent.Owner, tracker);
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
        return tracker.ObserveAnyUseInHand || tracker.ObserveAnyDrop || tracker.ObserveAnyAttack || tracker.ObserveAnyExamine;
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
        if (ent.Comp.CurrentBubbleTarget is { } oldTarget && Exists(oldTarget))
            RemComp<TutorialBubbleComponent>(oldTarget);

        ent.Comp.CurrentBubbleTarget = null;

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

        var bubble = EnsureComp<TutorialBubbleComponent>(target.Value);
        bubble.Instruction = step.Bubble.Text;

        ent.Comp.CurrentBubbleTarget = target;

        Dirty(target.Value, bubble);
        Dirty(ent);
    }

    public virtual void UpdateTimeCounter(Entity<TutorialPlayerComponent> ent, TimeSpan? endTime)
    {
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
}

/// <summary>
/// Event that is raised whenever an implant is removed from someone.
/// Raised on the the implant entity.
/// </summary>

[NetSerializable, Serializable]
public sealed class TutorialStepChangedEvent() : EntityEventArgs
{
}
