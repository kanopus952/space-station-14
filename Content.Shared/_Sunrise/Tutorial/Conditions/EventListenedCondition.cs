// using System.Reflection.Metadata.Ecma335;
// using Content.Shared._Sunrise.Tutorial.Components;
// using Content.Shared._Sunrise.Tutorial.Conditions;
// using Content.Shared.Containers.ItemSlots;
// using Content.Shared.Examine;
// using Content.Shared.Hands.Components;
// using Content.Shared.Interaction;
// using Content.Shared.Inventory;
// using Content.Shared.Weapons.Melee.Events;
// using Robust.Shared.Prototypes;

// namespace Content.Shared._Sunrise.Tutorial.Conditions;
// ///<summary>
// /// Checks if specified item equipped into slot
// /// </summary>
// public sealed partial class EventListenedConditionSystem : TutorialConditionSystem<TutorialPlayerComponent, EventListenedCondition>
// {
//     [Dependency] private readonly InventorySystem _inventory = default!;

//     public override void Initialize()
//     {
//         base.Initialize();

//         SubscribeLocalEvent<TutorialTrackerComponent, InteractHandEvent>(OnInteract);
//         SubscribeLocalEvent<TutorialTrackerComponent, ExaminedEvent>(OnExamined);
//         SubscribeLocalEvent<TutorialTrackerComponent, MeleeHitEvent>(OnMeleeHit);
//     }
//     private void OnInteract(Entity<TutorialTrackerComponent> ent, ref InteractHandEvent args)
//     {
//         DispatchEvent(ent, TutorialEventType.Interact);
//     }

//     private void OnExamined(Entity<TutorialTrackerComponent> ent, ref ExaminedEvent args)
//     {
//         DispatchEvent(ent, TutorialEventType.Examine);
//     }

//     private void OnMeleeHit(Entity<TutorialTrackerComponent> ent, ref MeleeHitEvent args)
//     {
//         DispatchEvent(args.User, TutorialEventType.Attack);
//         args
//     }

//     public void DispatchEvent(
//         EntityUid uid,
//         TutorialEventType type,
//         EntityUid target)
//     {
//         if (!TryComp<TutorialTrackerComponent>(uid, out var comp))
//             return;

//         comp.Triggered.Add(target, true);
//     }

//     protected override void Condition(Entity<TutorialPlayerComponent> entity, ref TutorialConditionEvent<EventListenedCondition> args)
//     {
//         if (!TryComp<TutorialTrackerComponent>(entity, out var tracker))
//             AddComp<TutorialTrackerComponent>(entity);

//         if (tracker == null)
//             return;

//         if (tracker.Triggered.Count == 0)
//             return;

//         var current = tracker.Counters
//             .GetValueOrDefault(args.Condition.Event);

//         if (current >= args.Condition.Count)
//         {
//             args.Result = true;
//             RemComp<TutorialTrackerComponent>(entity);
//         }
//     }
// }
// public sealed partial class EventListenedCondition : TutorialConditionBase<EventListenedCondition>
// {
//     [DataField]
//     public EntProtoId Target;

//     [DataField(required: true)]
//     public TutorialEventType Event;

//     [DataField]
//     public int Count = 1;
// }

// public enum TutorialEventType
// {
//     Interact,
//     Attack,
//     Use,
//     Examine,
//     Equip,
//     Drop
// }

