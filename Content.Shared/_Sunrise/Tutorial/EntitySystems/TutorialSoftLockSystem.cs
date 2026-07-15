using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.Events;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.Events;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.EntitySystems;

/// <summary>
///     Blocks tutorial-breaking actions while a tutorial step effect is active.
/// </summary>
public sealed class TutorialSoftLockSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TutorialDropSoftLockComponent, DropAttemptEvent>(OnDropAttempt);
        SubscribeLocalEvent<TutorialUnequipSoftLockComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
        SubscribeLocalEvent<TutorialUnequipSoftLockComponent, IsUnequippingTargetAttemptEvent>(OnUnequipTargetAttempt);
        SubscribeLocalEvent<StorageComponent, StorageInsertFailedEvent>(OnStorageInsertFailed);
        SubscribeLocalEvent<ContainerGettingInsertedAttemptEvent>(OnContainerGettingInsertedAttempt);
    }

    private void OnDropAttempt(Entity<TutorialDropSoftLockComponent> ent, ref DropAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancel();
        ShowPopup(ent, ent.Comp.Popup);
    }

    private void OnUnequipAttempt(Entity<TutorialUnequipSoftLockComponent> ent, ref IsUnequippingAttemptEvent args)
    {
        TryCancelUnequip(ent, args);
    }

    private void OnUnequipTargetAttempt(Entity<TutorialUnequipSoftLockComponent> ent, ref IsUnequippingTargetAttemptEvent args)
    {
        TryCancelUnequip(ent, args);
    }

    private void OnStorageInsertFailed(Entity<StorageComponent> ent, ref StorageInsertFailedEvent args)
    {
        if (!TryComp<TutorialStorageInsertSoftLockComponent>(args.Player, out var softLock))
            return;

        if (!_hands.TryGetActiveItem(args.Player, out var activeItem))
            return;

        if (!ShouldBlockStorageInsert(softLock, ent, activeItem.Value))
            return;

        ShowPopup(args.Player, softLock.Popup);
    }

    private void OnContainerGettingInsertedAttempt(ContainerGettingInsertedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var query = EntityQueryEnumerator<TutorialStorageInsertSoftLockComponent, HandsComponent>();
        while (query.MoveNext(out var uid, out var softLock, out var hands))
        {
            if (!_hands.IsHolding((uid, hands), args.EntityUid))
                continue;

            if (!ShouldBlockStorageInsert(softLock, args.Container.Owner, args.EntityUid))
                continue;

            args.Cancel();
            ShowPopup(uid, softLock.Popup);
            return;
        }
    }

    private bool TryCancelUnequip(Entity<TutorialUnequipSoftLockComponent> ent, UnequipAttemptEventBase args)
    {
        if (args.Cancelled || (args.SlotFlags & ent.Comp.Slots) == 0)
            return false;

        if (ent.Comp.Items.Count > 0 && !HasBlockedPrototype(args.Equipment, ent.Comp.Items))
            return false;

        args.Reason = ent.Comp.Popup;
        args.Cancel();
        return true;
    }

    private bool ShouldBlockStorageInsert(TutorialStorageInsertSoftLockComponent component, EntityUid storage, EntityUid item)
    {
        if (component.Items.Count == 0 && component.Targets.Count == 0)
            return false;

        if (component.Items.Count > 0 && !HasBlockedPrototype(item, component.Items))
            return false;

        if (component.Targets.Count > 0 && !HasBlockedPrototype(storage, component.Targets))
            return false;

        return true;
    }

    private bool HasBlockedPrototype(EntityUid uid, List<EntProtoId> prototypes)
    {
        var prototype = Prototype(uid);
        if (prototype == null)
            return false;

        for (var i = 0; i < prototypes.Count; i++)
        {
            if (prototypes[i] == prototype.ID)
                return true;
        }

        return false;
    }

    private void ShowPopup(EntityUid user, string popup)
    {
        _popup.PopupClient(Loc.GetString(popup), user, user);
    }
}
