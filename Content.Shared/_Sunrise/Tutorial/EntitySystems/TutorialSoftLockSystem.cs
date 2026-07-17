using System;
using System.Collections.Generic;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.Events;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Item;

namespace Content.Shared._Sunrise.Tutorial.EntitySystems;

/// <summary>
///     Blocks tutorial-breaking actions while a tutorial step effect is active.
/// </summary>
public sealed class TutorialSoftLockSystem : EntitySystem
{
    private static readonly TimeSpan PopupCooldown = TimeSpan.FromSeconds(1);

    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _lastPopupTimes = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TutorialDropSoftLockComponent, DropAttemptEvent>(OnDropAttempt);
        SubscribeLocalEvent<TutorialPickupSoftLockComponent, PickupAttemptEvent>(OnPickupAttempt);

        SubscribeLocalEvent<TutorialEquipSoftLockComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<TutorialEquipSoftLockComponent, IsEquippingTargetAttemptEvent>(OnEquipTargetAttempt);

        SubscribeLocalEvent<TutorialUnequipSoftLockComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
        SubscribeLocalEvent<TutorialUnequipSoftLockComponent, IsUnequippingTargetAttemptEvent>(OnUnequipTargetAttempt);

        SubscribeLocalEvent<TutorialStorageSoftLockComponent, StorageCloseAttemptEvent>(OnStorageCloseAttempt);
        SubscribeLocalEvent<TutorialStorageSoftLockComponent, StorageInsertFailedEvent>(OnStorageInsertFailed);
        SubscribeLocalEvent<TutorialStorageSoftLockComponent, InteractionAttemptEvent>(OnHeldItemInteractionAttempt);
        SubscribeLocalEvent<TutorialStorageSoftLockComponent, ContainerGettingInsertedAttemptEvent>(OnContainerGettingInsertedAttempt);

        SubscribeLocalEvent<TutorialStorageSoftLockTrackerComponent, DidEquipHandEvent>(OnHandEquipped);
        SubscribeLocalEvent<TutorialStorageSoftLockTrackerComponent, ComponentShutdown>(OnTrackerShutdown);
    }

    private void OnDropAttempt(Entity<TutorialDropSoftLockComponent> ent, ref DropAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancel();
        ShowPopup(ent, ent.Comp.Popup);
    }

    private void OnPickupAttempt(Entity<TutorialPickupSoftLockComponent> ent, ref PickupAttemptEvent args)
    {
        if (args.Cancelled || !ent.Comp.BlockAll && IsAllowedPrototype(args.Item, ent.Comp.AllowedItems))
            return;

        args.Cancel();
        if (args.ShowPopup)
            ShowPopup(ent, ent.Comp.Popup);
    }

    private void OnEquipAttempt(Entity<TutorialEquipSoftLockComponent> ent, ref IsEquippingAttemptEvent args)
    {
        if (_timing.ApplyingState)
            return;

        TryCancelEquip(ent, args);
    }

    private void OnEquipTargetAttempt(Entity<TutorialEquipSoftLockComponent> ent, ref IsEquippingTargetAttemptEvent args)
    {
        if (_timing.ApplyingState)
            return;

        TryCancelEquip(ent, args);
    }

    private void TryCancelEquip(Entity<TutorialEquipSoftLockComponent> ent, EquipAttemptBase args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.Cancelled)
            return;

        if ((args.SlotFlags & ent.Comp.Slot) != 0 && IsAllowedPrototype(args.Equipment, ent.Comp.Item))
            return;

        args.Reason = ent.Comp.Popup;
        args.Cancel();
    }

    private void OnUnequipAttempt(Entity<TutorialUnequipSoftLockComponent> ent, ref IsUnequippingAttemptEvent args)
    {
        if (_timing.ApplyingState)
            return;

        TryCancelUnequip(ent, args);
    }

    private void OnUnequipTargetAttempt(Entity<TutorialUnequipSoftLockComponent> ent, ref IsUnequippingTargetAttemptEvent args)
    {
        if (_timing.ApplyingState)
            return;

        TryCancelUnequip(ent, args);
    }

    private void OnStorageCloseAttempt(Entity<TutorialStorageSoftLockComponent> ent, ref StorageCloseAttemptEvent args)
    {
        if (args.Cancelled || args.User is not { } user)
            return;

        if (!ent.Comp.Players.Contains(user))
            return;

        if (!TryComp<TutorialStorageCloseSoftLockComponent>(user, out var softLock))
            return;

        if (!IsAllowedPrototype(ent, softLock.Targets))
            return;

        args.Cancelled = true;
        ShowPopup(user, softLock.Popup);
    }

    private void OnStorageInsertFailed(Entity<TutorialStorageSoftLockComponent> ent, ref StorageInsertFailedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (!ent.Comp.Players.Contains(args.Player) ||
            !TryComp<TutorialStorageInsertSoftLockComponent>(args.Player, out var softLock))
            return;

        if (!_hands.TryGetActiveItem(args.Player, out var activeItem))
            return;

        if (!ShouldBlockStorageInsert(softLock, ent, activeItem.Value))
            return;

        ShowPopup(args.Player, softLock.Popup);
    }

    private void OnHeldItemInteractionAttempt(Entity<TutorialStorageSoftLockComponent> ent, ref InteractionAttemptEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        if (!ent.Comp.Players.Contains(args.Uid))
            return;

        if (!TryComp<TutorialStorageInteractSoftLockComponent>(args.Uid, out var softLock))
            return;

        if (!ShouldBlockStorageInteract(softLock, target, ent.Owner))
            return;

        args.Cancelled = true;
        ShowPopup(args.Uid, softLock.Popup);
    }

    private void OnContainerGettingInsertedAttempt(
        Entity<TutorialStorageSoftLockComponent> ent, ref
        ContainerGettingInsertedAttemptEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.Cancelled)
            return;

        foreach (var player in ent.Comp.Players)
        {
            if (!TryComp<TutorialStorageInsertSoftLockComponent>(player, out var softLock))
                continue;

            if (!TryComp<HandsComponent>(player, out var hands))
                continue;

            if (!_hands.IsHolding((player, hands), args.EntityUid))
                continue;

            if (!ShouldBlockStorageInsert(softLock, args.Container.Owner, args.EntityUid))
                continue;

            args.Cancel();
            ShowPopup(player, softLock.Popup);
            return;
        }
    }

    private void OnHandEquipped(Entity<TutorialStorageSoftLockTrackerComponent> ent, ref DidEquipHandEvent args)
    {
        if (_timing.ApplyingState)
            return;

        TryMarkEntity(ent.Owner, args.Equipped, ent.Comp);
    }

    private void OnTrackerShutdown(Entity<TutorialStorageSoftLockTrackerComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        foreach (var target in ent.Comp.LinkedEntities)
        {
            if (!TryComp<TutorialStorageSoftLockComponent>(target, out var softLock) ||
                !softLock.Players.Remove(ent.Owner))
            {
                continue;
            }

            if (softLock.Players.Count == 0)
            {
                RemComp<TutorialStorageSoftLockComponent>(target);
                continue;
            }

            Dirty(target, softLock);
        }
    }

    public void ApplyStepSoftLocks(Entity<TutorialPlayerComponent> ent, TutorialStepPrototype step)
    {
        if (_timing.ApplyingState)
            return;

        ClearStepSoftLocks(ent);
        if (!HasStorageSoftLocks(ent))
            return;

        var tracker = EnsureComp<TutorialStorageSoftLockTrackerComponent>(ent);
        foreach (var target in _lookup.GetEntitiesInRange(ent, step.ObserveRange))
        {
            TryMarkEntity(ent.Owner, target, tracker);
        }

        MarkEquippedEntities(ent, tracker);
    }

    public void ClearStepSoftLocks(Entity<TutorialPlayerComponent> ent)
    {
        RemComp<TutorialStorageSoftLockTrackerComponent>(ent);
    }

    private void MarkEquippedEntities(
        Entity<TutorialPlayerComponent> ent,
        TutorialStorageSoftLockTrackerComponent tracker)
    {
        if (TryComp<HandsComponent>(ent, out var hands))
        {
            foreach (var held in _hands.EnumerateHeld((ent, hands)))
            {
                TryMarkEntity(ent.Owner, held, tracker);
            }
        }

        if (!TryComp<InventoryComponent>(ent, out var inventory))
            return;

        foreach (var slot in inventory.Slots)
        {
            if (_inventory.TryGetSlotEntity(ent, slot.Name, out var item, inventory))
                TryMarkEntity(ent.Owner, item.Value, tracker);
        }
    }

    private void TryMarkEntity(
        EntityUid player,
        EntityUid target,
        TutorialStorageSoftLockTrackerComponent tracker)
    {
        if (!ShouldMarkStorageTarget(player, target) && !ShouldMarkStorageSource(player, target))
            return;

        if (!tracker.LinkedEntities.Add(target))
            return;

        var softLock = EnsureComp<TutorialStorageSoftLockComponent>(target);
        if (softLock.Players.Add(player))
            Dirty(target, softLock);
    }

    private bool HasStorageSoftLocks(EntityUid player)
    {
        return HasComp<TutorialStorageCloseSoftLockComponent>(player) ||
               HasComp<TutorialStorageInsertSoftLockComponent>(player) ||
               HasComp<TutorialStorageInteractSoftLockComponent>(player);
    }

    private bool ShouldMarkStorageTarget(EntityUid player, EntityUid target)
    {
        if (HasComp<EntityStorageComponent>(target) &&
            TryComp<TutorialStorageCloseSoftLockComponent>(player, out var closeSoftLock) &&
            IsAllowedPrototype(target, closeSoftLock.Targets))
        {
            return true;
        }

        if (!HasComp<StorageComponent>(target) ||
            !TryComp<TutorialStorageInsertSoftLockComponent>(player, out var insertSoftLock))
        {
            return false;
        }

        return IsStorageTargetAllowed(target, insertSoftLock);
    }

    private bool ShouldMarkStorageSource(EntityUid player, EntityUid target)
    {
        if (TryComp<TutorialStorageInsertSoftLockComponent>(player, out var insertSoftLock) &&
            IsStorageSourceAllowed(target, insertSoftLock.Items, insertSoftLock.Targets))
        {
            return true;
        }

        return TryComp<TutorialStorageInteractSoftLockComponent>(player, out var interactSoftLock) &&
               IsStorageSourceAllowed(target, interactSoftLock.Items, interactSoftLock.Targets);
    }

    private bool IsStorageTargetAllowed(EntityUid target, TutorialStorageInsertSoftLockComponent softLock)
    {
        if (softLock.Items.Count == 0 && softLock.Targets.Count == 0)
            return false;

        return softLock.Targets.Count == 0 || HasBlockedPrototype(target, softLock.Targets);
    }

    private bool IsStorageSourceAllowed(EntityUid target, List<EntProtoId> items, List<EntProtoId> targets)
    {
        if (items.Count == 0)
            return targets.Count > 0;

        return HasBlockedPrototype(target, items);
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
        return ShouldBlockStorage(component.Items, component.Targets, storage, item);
    }

    private bool ShouldBlockStorageInteract(
        TutorialStorageInteractSoftLockComponent component,
        EntityUid storage,
        EntityUid item)
    {
        return ShouldBlockStorage(component.Items, component.Targets, storage, item);
    }

    private bool ShouldBlockStorage(
        List<EntProtoId> items,
        List<EntProtoId> targets,
        EntityUid storage,
        EntityUid item)
    {
        if (items.Count == 0 && targets.Count == 0)
            return false;

        if (items.Count > 0 && !HasBlockedPrototype(item, items))
            return false;

        if (targets.Count > 0 && !HasBlockedPrototype(storage, targets))
            return false;

        return true;
    }

    private bool IsAllowedPrototype(EntityUid uid, EntProtoId prototype)
    {
        var entityPrototype = Prototype(uid);
        return entityPrototype != null && entityPrototype.ID == prototype;
    }

    private bool IsAllowedPrototype(EntityUid uid, List<EntProtoId> prototypes)
    {
        if (prototypes.Count == 0)
            return true;

        var entityPrototype = Prototype(uid);
        if (entityPrototype == null)
            return false;

        for (var i = 0; i < prototypes.Count; i++)
        {
            if (entityPrototype.ID == prototypes[i])
                return true;
        }

        return false;
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
        if (_lastPopupTimes.TryGetValue(user, out var lastPopup) &&
            _timing.CurTime - lastPopup < PopupCooldown)
        {
            return;
        }

        _lastPopupTimes[user] = _timing.CurTime;
        _popup.PopupClient(Loc.GetString(popup), user, user);
    }
}
