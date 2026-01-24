using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

/// <summary>
/// Tracks player actions and validates tutorial conditions that depend on them.
/// </summary>
public sealed partial class EventListenedConditionSystem : TutorialConditionSystem<TutorialPlayerComponent, EventListenedCondition>
{
    [Dependency] private readonly SharedTutorialSystem _tutorial = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TutorialPlayerComponent, UserInteractHandEvent>(OnUserInteractHand);
        SubscribeLocalEvent<TutorialPlayerComponent, UserInteractUsingEvent>(OnUserInteractUsing);
        SubscribeLocalEvent<TutorialPlayerComponent, DidEquipEvent>(OnDidEquip);
        SubscribeLocalEvent<TutorialPlayerComponent, DidEquipHandEvent>(OnDidEquipHand);
        SubscribeLocalEvent<TutorialObservableComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<TutorialObservableComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<TutorialObservableComponent, AttackedEvent>(OnMeleeHit);
        SubscribeLocalEvent<TutorialObservableComponent, ExaminedEvent>(OnExamined);
    }

    protected override void Condition(Entity<TutorialPlayerComponent> entity, ref TutorialConditionEvent<EventListenedCondition> args)
    {
        if (args.Condition.Count <= 0)
        {
            args.Result = true;
            return;
        }

        if (!TryComp<TutorialTrackerComponent>(entity.Owner, out var tracker))
            return;

        var key = (args.Condition.Event, args.Condition.Target);
        if (tracker.Counters.TryGetValue(key, out var count) && count >= args.Condition.Count)
            args.Result = true;
    }

    private void OnUserInteractHand(Entity<TutorialPlayerComponent> ent, ref UserInteractHandEvent args)
    {
        RecordEvent(ent.Owner, TutorialEventType.Interact, args.Target);
    }

    private void OnUserInteractUsing(Entity<TutorialPlayerComponent> ent, ref UserInteractUsingEvent args)
    {
        RecordEvent(ent.Owner, TutorialEventType.Use, args.Target, args.Used);
    }

    private void OnDidEquip(Entity<TutorialPlayerComponent> ent, ref DidEquipEvent args)
    {
        RecordEvent(ent.Owner, TutorialEventType.Equip, args.Equipment);

        if (!TryComp<TutorialTrackerComponent>(ent, out var tracker))
            return;

        _tutorial.TryObserveEntity(ent, args.Equipment, tracker);
    }

    private void OnDidEquipHand(Entity<TutorialPlayerComponent> ent, ref DidEquipHandEvent args)
    {
        RecordEvent(ent.Owner, TutorialEventType.Equip, args.Equipped);

        if (!TryComp<TutorialTrackerComponent>(ent, out var tracker))
            return;

        _tutorial.TryObserveEntity(ent, args.Equipped, tracker);
    }

    private void OnUseInHand(Entity<TutorialObservableComponent> ent, ref UseInHandEvent args)
    {
        if (!HasComp<TutorialTrackerComponent>(args.User))
            return;

        if (!ent.Comp.Observers.Contains(args.User))
            return;

        RecordEvent(args.User, TutorialEventType.Use, ent.Owner);
    }

    private void OnDropped(Entity<TutorialObservableComponent> ent, ref DroppedEvent args)
    {
        if (!HasComp<TutorialTrackerComponent>(args.User))
            return;

        if (!ent.Comp.Observers.Contains(args.User))
            return;

        RecordEvent(args.User, TutorialEventType.Drop, ent.Owner);
    }

    private void OnExamined(Entity<TutorialObservableComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<TutorialTrackerComponent>(args.Examiner))
            return;

        if (!ent.Comp.Observers.Contains(args.Examiner))
            return;

        RecordEvent(args.Examiner, TutorialEventType.Examine, args.Examined);
    }

    private void OnMeleeHit(Entity<TutorialObservableComponent> ent, ref AttackedEvent args)
    {
        if (!HasComp<TutorialTrackerComponent>(args.User))
            return;

        if (!ent.Comp.Observers.Contains(args.User))
            return;

        RecordEvent(args.User, TutorialEventType.Attack, args.Used);
    }

    private void RecordEvent(EntityUid user, TutorialEventType type, EntityUid? primaryTarget = null, EntityUid? secondaryTarget = null)
    {
        var tracker = EnsureComp<TutorialTrackerComponent>(user);

        IncrementConditionCounters((user, tracker), type, null);

        if (_tutorial.TryGetPrototypeId(primaryTarget, out var primaryProto))
            IncrementConditionCounters((user, tracker), type, primaryProto);

        if (_tutorial.TryGetPrototypeId(secondaryTarget, out var secondaryProto) && secondaryProto != primaryProto)
            IncrementConditionCounters((user, tracker), type, secondaryProto);
    }

    private void RecordEvent(EntityUid user, TutorialEventType type, IReadOnlyList<EntityUid> targets)
    {
        var tracker = EnsureComp<TutorialTrackerComponent>(user);

        IncrementConditionCounters((user, tracker), type, null);

        foreach (var target in targets)
        {
            if (_tutorial.TryGetPrototypeId(target, out var protoId))
                IncrementConditionCounters((user, tracker), type, protoId);
        }
    }

    private void IncrementConditionCounters(Entity<TutorialTrackerComponent> ent, TutorialEventType type, EntProtoId? target)
    {
        var key = (type, target);
        ent.Comp.Counters.TryGetValue(key, out var count);
        ent.Comp.Counters[key] = count + 1;
        Dirty(ent);
    }
}

/// <summary>
/// Checks if the player has performed an action a required number of times.
/// </summary>
public sealed partial class EventListenedCondition : TutorialConditionBase<EventListenedCondition>
{
    [DataField]
    public EntProtoId? Target;

    [DataField(required: true)]
    public TutorialEventType Event;

    [DataField]
    public int Count = 1;
}

public enum TutorialEventType
{
    Interact,
    Attack,
    Use,
    Examine,
    Equip,
    Drop
}
