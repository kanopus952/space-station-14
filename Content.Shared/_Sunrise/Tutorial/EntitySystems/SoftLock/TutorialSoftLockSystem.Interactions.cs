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
using Content.Shared.UserInterface;

namespace Content.Shared._Sunrise.Tutorial.EntitySystems.SoftLock;

/// <summary>
///     Blocks interactions.
/// </summary>
public sealed partial class TutorialSoftLockSystem
{
    public void InitializeInteractions()
    {
        SubscribeLocalEvent<TutorialInteractSoftLockComponent, InteractionAttemptEvent>(OnHeldItemInteractionAttempt);

        SubscribeLocalEvent<TutorialSoftLockEntityComponent, ActivatableUIOpenAttemptEvent>(OnBuiOpen);
        SubscribeLocalEvent<TutorialOpenUiSoftLockComponent, TutorialShouldMarkEntityEvent>(OnOpenUiShouldMarkEntity);
    }
    private void OnHeldItemInteractionAttempt(Entity<TutorialInteractSoftLockComponent> ent, ref InteractionAttemptEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        if (!ShouldBlockInteract(ent.Comp.Targets, ent.Comp.Items, target, ent))
            return;

        args.Cancelled = true;
        ShowPopup(args.Uid, ent.Comp.Popup);
    }

    private void OnBuiOpen(Entity<TutorialSoftLockEntityComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<TutorialOpenUiSoftLockComponent>(args.User, out var softLock))
            return;

        if (!ShouldBlockInteract(softLock.Targets, null, ent, args.User))
            return;

        args.Silent = true;
        args.Cancel();
        ShowPopup(args.User, softLock.Popup);
    }

    private void OnOpenUiShouldMarkEntity(
        Entity<TutorialOpenUiSoftLockComponent> ent,
        ref TutorialShouldMarkEntityEvent args)
    {
        if (args.ShouldMark)
            return;

        if (!HasComp<ActivatableUIComponent>(args.Target))
            return;

        args.ShouldMark = IsAllowedPrototype(args.Target, ent.Comp.Targets);
    }

    private bool ShouldBlockInteract(
        List<EntProtoId>? targets,
        List<EntProtoId>? items,
        EntityUid entity,
        EntityUid item)
    {
        if (items?.Count == 0 && targets?.Count == 0)
            return false;

        if (items?.Count > 0 && !HasBlockedPrototype(item, items))
            return false;

        if (targets?.Count > 0 && !HasBlockedPrototype(entity, targets))
            return false;

        return true;
    }
}
