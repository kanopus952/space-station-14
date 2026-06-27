using System;
using Content.Shared._Sunrise.SponsorInventory;
using Content.Shared.GameTicking;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.SponsorInventory;

public sealed partial class SunriseInventorySystem
{
    /*
     * Handles incoming network events and session cache cleanup.
     */
    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.Disconnected)
            return;

        _profiles.Remove(args.Session.UserId);
    }

    private async void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        try
        {
            TryApplyInventory(ev.Mob, await GetSelectedInventoryProfileAsync(ev.Player.UserId), ev.JobId, ev.Player);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to apply sponsor inventory for {ev.Player.UserId}: {e}");
        }
    }

    private async void OnInitialDataRequest(SunriseInventoryInitialDataRequestEvent ev, EntitySessionEventArgs args)
    {
        try
        {
            await SendInitialData(args.SenderSession);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to send sponsor inventory initial data for {args.SenderSession.UserId}: {e}");
        }
    }

    private void OnPetSelected(SunriseInventoryPetSelectedEvent ev, EntitySessionEventArgs args)
    {
        TrySetPetSelection(args.SenderSession.UserId, ev.SelectedPetSelection);
    }

    private async void OnInventoryProfileChanged(SunriseInventoryProfileChangedEvent ev, EntitySessionEventArgs args)
    {
        try
        {
            await TrySetInventoryProfileAsync(args.SenderSession, ev.Slot, ev.Profile);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save sponsor inventory profile for {args.SenderSession.UserId}: {e}");
        }
    }

    private async void OnPurchaseRequest(SunriseInventoryPurchaseRequestEvent ev, EntitySessionEventArgs args)
    {
        try
        {
            await TryPurchaseInventoryItem(args.SenderSession, ev);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to purchase sponsor inventory item for {args.SenderSession.UserId}: {e}");
        }
    }
}
