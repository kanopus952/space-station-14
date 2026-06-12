using System.Collections.Generic;
using System.Linq;
using Content.Shared.Roles;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.SponsorInventory;

public static class SunriseInventoryValidation
{
    public static SunriseInventoryProfile EnsureValid(
        SunriseInventoryProfile profile,
        ICommonSession session,
        IPrototypeManager prototype,
        ISharedSponsorsManager? sponsors)
    {
        if (sponsors == null)
            return new SunriseInventoryProfile();

        var config = sponsors.GetSponsorInventoryConfig();
        if (config.Items.Length == 0)
            return new SunriseInventoryProfile();

        var items = new Dictionary<string, SponsorInventoryItemInfo>();
        foreach (var item in config.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                continue;

            items[item.Id] = item;
        }

        if (items.Count == 0)
            return new SunriseInventoryProfile();

        var purchasedItems = GetPurchasedItems(session, sponsors);
        var sponsorTier = sponsors.GetSponsorTier(session.UserId);

        var valid = new SunriseInventoryProfile
        {
            Global = EnsureValidSelection(
                profile.Global,
                null,
                items,
                purchasedItems,
                sponsorTier,
                prototype),
        };

        foreach (var (jobId, selection) in profile.Jobs)
        {
            if (string.IsNullOrWhiteSpace(jobId) || !prototype.HasIndex<JobPrototype>(jobId))
                continue;

            var validSelection = EnsureValidSelection(
                selection,
                jobId,
                items,
                purchasedItems,
                sponsorTier,
                prototype);

            if (!validSelection.IsEmpty())
                valid.Jobs[jobId] = validSelection;
        }

        return valid;
    }

    public static bool CanUseItem(
        string inventoryItemId,
        string? jobId,
        ICommonSession session,
        IPrototypeManager prototype,
        ISharedSponsorsManager? sponsors)
    {
        if (sponsors == null)
            return false;

        var config = sponsors.GetSponsorInventoryConfig();
        SponsorInventoryItemInfo? item = null;

        foreach (var inventoryItem in config.Items)
        {
            if (inventoryItem.Id != inventoryItemId)
                continue;

            item = inventoryItem;
            break;
        }

        if (item == null)
            return false;

        return CanUseItem(
            item,
            jobId,
            GetPurchasedItems(session, sponsors),
            sponsors.GetSponsorTier(session.UserId),
            prototype);
    }

    public static SunriseInventorySelection GetEffectiveSelection(SunriseInventoryProfile profile, string? jobId)
    {
        var selection = profile.Global.Clone();

        if (jobId == null || !profile.Jobs.TryGetValue(jobId, out var jobSelection))
            return selection;

        foreach (var (slot, itemId) in jobSelection.SlotItems)
        {
            selection.SlotItems[slot] = itemId;
        }

        selection.BagItems.AddRange(jobSelection.BagItems);
        return selection;
    }

    private static SunriseInventorySelection EnsureValidSelection(
        SunriseInventorySelection selection,
        string? jobId,
        Dictionary<string, SponsorInventoryItemInfo> items,
        HashSet<string> purchasedItems,
        int sponsorTier,
        IPrototypeManager prototype)
    {
        var valid = new SunriseInventorySelection();
        var usedItems = new HashSet<string>();

        foreach (var (slot, itemId) in selection.SlotItems)
        {
            if (string.IsNullOrWhiteSpace(slot) ||
                usedItems.Contains(itemId) ||
                !items.TryGetValue(itemId, out var item) ||
                !CanUseItem(item, jobId, purchasedItems, sponsorTier, prototype))
            {
                continue;
            }

            valid.SlotItems[slot] = itemId;
            usedItems.Add(itemId);
        }

        foreach (var itemId in selection.BagItems)
        {
            if (usedItems.Contains(itemId) ||
                !items.TryGetValue(itemId, out var item) ||
                !CanUseItem(item, jobId, purchasedItems, sponsorTier, prototype))
            {
                continue;
            }

            valid.BagItems.Add(itemId);
            usedItems.Add(itemId);
        }

        return valid;
    }

    private static bool CanUseItem(
        SponsorInventoryItemInfo item,
        string? jobId,
        HashSet<string> purchasedItems,
        int sponsorTier,
        IPrototypeManager prototype)
    {
        if (string.IsNullOrWhiteSpace(item.EntityPrototype) ||
            !prototype.HasIndex<EntityPrototype>(item.EntityPrototype))
        {
            return false;
        }

        if (!IsJobAllowed(item, jobId))
            return false;

        if (purchasedItems.Contains(item.Id))
            return true;

        return item.SponsorLevel != null && sponsorTier >= item.SponsorLevel.Value;
    }

    private static bool IsJobAllowed(SponsorInventoryItemInfo item, string? jobId)
    {
        if (item.AvailableJobs is not { Length: > 0 })
            return true;

        if (jobId == null)
            return false;

        return item.AvailableJobs.Contains(jobId);
    }

    private static HashSet<string> GetPurchasedItems(ICommonSession session, ISharedSponsorsManager sponsors)
    {
        if (sponsors.TryGetPurchasedInventoryItems(session.UserId, out var serverItems) && serverItems != null)
            return serverItems.ToHashSet();

        return sponsors.GetClientPurchasedInventoryItems().ToHashSet();
    }
}
