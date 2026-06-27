using System.Collections.Generic;
using System.Linq;
using Content.Shared._Sunrise.SponsorInventory;
using Content.Shared.Clothing;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.SponsorInventory;

public sealed partial class SunriseInventorySystem
{
    /*
     * Validates profiles against a concrete character through a temporary dummy mob.
     */
    private SunriseInventoryProfile EnsureValidForCharacter(
        ICommonSession session,
        int slot,
        SunriseInventoryProfile profile)
    {
        if (!_preferences.TryGetCachedPreferences(session.UserId, out var preferences) ||
            !preferences.Characters.TryGetValue(slot, out var character) ||
            character is not HumanoidCharacterProfile humanoid)
        {
            return new SunriseInventoryProfile();
        }

        var valid = new SunriseInventoryProfile
        {
            Global = EnsureValidSelectionForCharacter(session, humanoid, null, profile.Global, validateBag: false),
        };

        foreach (var (jobId, selection) in profile.Jobs ?? new Dictionary<string, SunriseInventorySelection>())
        {
            if (selection == null || !_prototype.HasIndex<JobPrototype>(jobId))
                continue;

            var effectiveProfile = new SunriseInventoryProfile
            {
                Global = valid.Global.Clone(),
                Jobs = { [jobId] = selection.Clone() },
            };
            var effectiveSelection = SunriseInventoryValidation.GetEffectiveSelection(effectiveProfile, jobId);
            var validEffectiveSelection = EnsureValidSelectionForCharacter(session, humanoid, jobId, effectiveSelection, validateBag: true);
            var validSelection = GetJobSpecificSelection(selection, validEffectiveSelection);
            if (!validSelection.IsEmpty())
                valid.Jobs[jobId] = validSelection;
        }

        return valid;
    }

    private SunriseInventorySelection EnsureValidSelectionForCharacter(
        ICommonSession session,
        HumanoidCharacterProfile profile,
        string? jobId,
        SunriseInventorySelection selection,
        bool validateBag)
    {
        var valid = new SunriseInventorySelection();
        selection ??= new SunriseInventorySelection();

        var dummy = SpawnValidationDummy(session, profile, jobId);
        if (dummy == null)
            return valid;

        try
        {
            var items = BuildSponsorItemLookup(_sponsors?.GetSponsorInventoryConfig());
            var usedItems = new HashSet<string>();

            foreach (var (slot, itemId) in selection.SlotItems)
            {
                if (!usedItems.Add(itemId) ||
                    !items.TryGetValue(itemId, out var item))
                    continue;

                if (!TryEquipSponsorItem(dummy.Value, slot, item))
                {
                    usedItems.Remove(itemId);
                    continue;
                }

                valid.SlotItems[slot] = itemId;
            }

            if (!validateBag ||
                !_inventory.TryGetSlotEntity(dummy.Value, "back", out var backEntity) ||
                !TryComp<StorageComponent>(backEntity, out var storage))
            {
                return valid;
            }

            foreach (var itemId in selection.BagItems)
            {
                if (!usedItems.Add(itemId) ||
                    !items.TryGetValue(itemId, out var item))
                    continue;

                if (!TryStoreSponsorItem(backEntity.Value, storage, item))
                {
                    usedItems.Remove(itemId);
                    continue;
                }

                valid.BagItems.Add(itemId);
            }

            return valid;
        }
        finally
        {
            QueueDel(dummy.Value);
        }
    }

    private EntityUid? SpawnValidationDummy(
        ICommonSession session,
        HumanoidCharacterProfile profile,
        string? jobId)
    {
        if (!_prototype.TryIndex(profile.Species, out var species))
            return null;

        var dummy = Spawn(species.DollPrototype, MapCoordinates.Nullspace);
        _humanoid.LoadProfile(dummy, profile);

        if (jobId == null || !_prototype.TryIndex<JobPrototype>(jobId, out var job))
            return dummy;

        var jobLoadoutId = LoadoutSystem.GetJobPrototype(job.ID);
        var effectiveJobLoadoutId = LoadoutSystem.GetEffectiveRolePrototype(jobLoadoutId, _prototype);
        if (_prototype.TryIndex(effectiveJobLoadoutId, out var roleProto))
        {
            profile.Loadouts.TryGetValue(jobLoadoutId, out var loadout);
            loadout ??= new RoleLoadout(jobLoadoutId);
            loadout.SetDefault(profile, session, _prototype);
            _spawn.EquipRoleLoadout(dummy, loadout, roleProto);
        }

        if (job.StartingGear != null)
            _spawn.EquipStartingGear(dummy, job.StartingGear, raiseEvent: false);

        return dummy;
    }

    private static SunriseInventorySelection GetJobSpecificSelection(
        SunriseInventorySelection requestedSelection,
        SunriseInventorySelection validEffectiveSelection)
    {
        var selection = new SunriseInventorySelection();

        foreach (var (slot, itemId) in requestedSelection.SlotItems)
        {
            if (validEffectiveSelection.SlotItems.TryGetValue(slot, out var validItemId) &&
                validItemId == itemId)
            {
                selection.SlotItems[slot] = itemId;
            }
        }

        var validBagItems = validEffectiveSelection.BagItems.ToHashSet();
        foreach (var itemId in requestedSelection.BagItems)
        {
            if (!validBagItems.Remove(itemId))
                continue;

            selection.BagItems.Add(itemId);
        }

        return selection;
    }
}
