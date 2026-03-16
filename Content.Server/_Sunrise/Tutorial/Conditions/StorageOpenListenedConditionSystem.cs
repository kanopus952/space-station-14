using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Conditions;
using Content.Shared.Storage.Components;

namespace Content.Server._Sunrise.Tutorial.Conditions;

public sealed partial class StorageOpenListenedConditionSystem
    : EventListenedConditionSystemBase<StorageOpenListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        // Lockers, crates, etc. — open physically (EntityStorageComponent)
        SubscribeLocalEvent<TutorialObservableComponent, StorageOpenAttemptEvent>(OnEntityStorageOpen);
    }

    private void OnEntityStorageOpen(Entity<TutorialObservableComponent> ent, ref StorageOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.Observers.Contains(args.User))
            return;

        RecordEvent(args.User, DefaultKey, ent);
    }
}

/// <summary>
/// Checks if the player has opened a physical storage container (locker, crate, etc.).
/// For bag/backpack BUI opens use <see cref="BuiOpenListenedCondition"/> instead.
/// Supports any storage or a specific prototype via <see cref="EventListenedConditionBase{T}.Target"/>.
/// </summary>
public sealed partial class StorageOpenListenedCondition : EventListenedConditionBase<StorageOpenListenedCondition>
{
    public override bool ObserveAnyWithoutTarget => true;
}
