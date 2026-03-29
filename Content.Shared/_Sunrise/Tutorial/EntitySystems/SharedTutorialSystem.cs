using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Components.Trackers;
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
            if (comp.EndTime != null && _timing.CurTime > comp.EndTime)
            {
                EndTutorial((uid, comp));
                continue;
            }

            CheckCondition((uid, comp));
        }
    }

    private void OnComponentInit(Entity<TutorialPlayerComponent> ent, ref ComponentInit args)
    {
        // SequenceId may not be set yet if the component was added before the caller
        // could configure it (e.g. EnsureComp fires ComponentInit synchronously).
        // In that case the server calls InitializeTutorial() explicitly after setup.
        InitializeTutorial(ent);
    }

    /// <summary>
    /// Performs first-time setup for a tutorial session: starts the timer, initialises
    /// the first step, and fires all related side-effects.
    /// Must be called after <see cref="TutorialPlayerComponent.SequenceId"/> and
    /// <see cref="TutorialPlayerComponent.Grid"/> are fully configured.
    /// </summary>
    public void InitializeTutorial(Entity<TutorialPlayerComponent> ent)
    {
        if (!TryGetCurrentStep(ent, out var step))
            return;

        ent.Comp.EndTime = _timing.CurTime + _proto.Index(ent.Comp.SequenceId).Duration;
        UpdateTimeCounter(ent, ent.Comp.EndTime);
        OnStepChanged(ent, step);
    }

    private void CheckCondition(Entity<TutorialPlayerComponent> ent)
    {
        if (!TryGetCurrentStep(ent, out var step))
            return;

        if (!_tutorial.TryConditions(ent, step.Preconditions.ToArray()))
        {
            ClearTutorialBubble(ent);
            Advance(ent, step.PreconditionFailStep);
            return;
        }

        if (!_tutorial.TryConditions(ent, step.Conditions.ToArray()))
            return;

        if (step.AnyConditions.Count > 0 && !_tutorial.TryAnyCondition(ent, step.AnyConditions.ToArray()))
            return;

        Advance(ent);
    }

    private void Advance(Entity<TutorialPlayerComponent> ent, ProtoId<TutorialStepPrototype>? stepId = null)
    {
        if (!_proto.TryIndex(ent.Comp.SequenceId, out var sequence))
            return;

        if (stepId == null)
        {
            var nextIndex = ent.Comp.StepIndex + 1;

            UpdateProgressBar(ent, nextIndex);

            if (nextIndex >= sequence.Steps.Count)
            {
                CompleteTutorial(ent, sequence);
                return;
            }

            stepId = sequence.Steps[nextIndex];
        }
        else
        {
            var index = sequence.Steps.IndexOf(stepId.Value);
            if (index < 0 || ent.Comp.StepIndex == index)
                return;

            UpdateProgressBar(ent, index);
        }

        if (!_proto.TryIndex(stepId.Value, out var step))
            return;

        OnStepChanged(ent, step);
    }

    private void UpdateProgressBar(Entity<TutorialPlayerComponent> ent, int index)
    {
        var progressBar = EnsureComp<TutorialProgressBarComponent>(ent);
        progressBar.CurrentStepIndex = index;
        ent.Comp.StepIndex = index;
        Dirty(ent, progressBar);
    }

    private void OnStepChanged(Entity<TutorialPlayerComponent> ent, TutorialStepPrototype step)
    {
        ResetTracking(ent);
        ClearTutorialBubble(ent);
        ent.Comp.Target = null;
        Dirty(ent, ent.Comp);

        RaiseLocalEvent(ent, new TutorialStepChangedEvent());

        if (_tutorial.TryConditions(ent, step.Preconditions.ToArray()))
            UpdateTutorialBubble(ent, step);
    }

    public void EndTutorial(Entity<TutorialPlayerComponent> ent)
    {
        ClearTutorialBubble(ent);
        ent.Comp.Target = null;

        ClearTracking(ent);
        UpdateTimeCounter(ent, null);

        RaiseLocalEvent(ent, new TutorialEndedEvent());
        Dirty(ent);
    }

    public void CompleteTutorial(Entity<TutorialPlayerComponent> ent, TutorialSequencePrototype sequence)
    {
        ent.Comp.StepIndex = sequence.Steps.Count;
        ent.Comp.Target = null;

        ClearTutorialBubble(ent);
        ClearTracking(ent);

        RaiseLocalEvent(ent, new TutorialStepsCompletedEvent());
        Dirty(ent, ent.Comp);
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

        CollectObservedConditions(tracker, step.Conditions);
        CollectObservedConditions(tracker, step.AnyConditions);
        CollectObservedConditions(tracker, step.Preconditions);

        ObserveNearbyEntities(ent, tracker, step);
        ObserveEquippedEntities(ent, tracker);
    }

    private static void CollectObservedConditions(
        TutorialTrackerComponent tracker,
        IEnumerable<TutorialCondition> conditions)
    {
        foreach (var condition in conditions)
        {
            if (condition is not IEventListenedCondition listened)
                continue;

            if (listened.Target != null)
            {
                tracker.TargetPrototypes.Add(listened.Target.Value);
                continue;
            }

            if (listened.ObserveAnyWithoutTarget)
                tracker.Counters.TryAdd((listened.ObserveKey, default), 0);
        }
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
            if (_inventory.TryGetSlotEntity(user, slot.Name, out var item, inventory))
            {
                TryObserveEntity(user, item.Value, tracker);
            }
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

        return TryGetPrototypeId(target, out var protoId) && tracker.TargetPrototypes.Contains(protoId);
    }

    private static bool ObservesAny(TutorialTrackerComponent tracker)
    {
        return tracker.Counters.Keys.Any(k =>
            k.Target.Equals(default(EntProtoId)) &&
            k.Key.EndsWith(EventListenedConditionKeys.ObserveSuffix, StringComparison.Ordinal));
    }

    public bool TryGetCurrentStep(Entity<TutorialPlayerComponent> ent, [NotNullWhen(true)] out TutorialStepPrototype? step)
    {
        step = null;

        if (!_proto.TryIndex(ent.Comp.SequenceId, out var sequence))
            return false;

        if (ent.Comp.StepIndex < 0 || ent.Comp.StepIndex >= sequence.Steps.Count)
            return false;

        return _proto.TryIndex(sequence.Steps[ent.Comp.StepIndex], out step);
    }

    public bool TryGetPrototypeId(EntityUid? uid, out EntProtoId protoId)
    {
        protoId = default;

        if (uid is not { } target)
            return false;

        var proto = Prototype(target);
        if (proto == null)
            return false;

        protoId = proto.ID;
        return true;
    }

    private void UpdateTutorialBubble(Entity<TutorialPlayerComponent> ent, TutorialStepPrototype step)
    {
        var targetEntity = step.Target != null
            ? TryFindNearestTargetEntity(ent, step.Target, step)
            : null;

        ent.Comp.Target = targetEntity;

        if (step.Bubble == null)
        {
            Dirty(ent, ent.Comp);
            return;
        }

        var bubbleTarget = step.Bubble.AttachToTarget ? targetEntity : ent;

        if (bubbleTarget == null || !Exists(bubbleTarget.Value))
        {
            ClearTutorialBubble(ent);
            Dirty(ent, ent.Comp);
            return;
        }

        if (ent.Comp.CurrentBubbleTarget is { } previous && previous != bubbleTarget.Value && Exists(previous))
            RemComp<TutorialBubbleComponent>(previous);

        var bubble = EnsureComp<TutorialBubbleComponent>(bubbleTarget.Value);
        bubble.Instruction = step.Bubble.Text;
        Dirty(bubbleTarget.Value, bubble);

        ent.Comp.CurrentBubbleTarget = bubbleTarget;
        Dirty(ent, ent.Comp);
    }

    private void ClearTutorialBubble(Entity<TutorialPlayerComponent> ent)
    {
        if (ent.Comp.CurrentBubbleTarget is { } oldTarget && Exists(oldTarget))
            RemComp<TutorialBubbleComponent>(oldTarget);

        ent.Comp.CurrentBubbleTarget = null;
    }

    private EntityUid? TryFindNearestTargetEntity(EntityUid uid, EntProtoId? target, TutorialStepPrototype proto)
    {
        var origin = _transform.GetMapCoordinates(uid);
        var best = EntityUid.Invalid;
        var bestDistSq = float.MaxValue;

        foreach (var ent in _lookupSystem.GetEntitiesInRange(uid, proto.ObserveRange))
        {
            var meta = MetaData(ent);
            if (meta.EntityPrototype?.ID != target)
                continue;

            var distSq = (_transform.GetMapCoordinates(ent).Position - origin.Position).LengthSquared();
            if (distSq >= bestDistSq)
                continue;

            bestDistSq = distSq;
            best = ent;
        }

        return best;
    }

    protected virtual void UpdateTimeCounter(Entity<TutorialPlayerComponent> ent, TimeSpan? endTime)
    {
    }
}
