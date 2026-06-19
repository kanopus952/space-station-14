using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server._Sunrise.PlayerCache;
using Content.Server._Sunrise.SponsorValidation;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.PlayerCache;
using Content.Shared._Sunrise.SponsorInventory;
using Content.Shared.Clothing;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.SponsorInventory;

/// <summary>
/// Authoritative server-side application and persistence flow for sponsor inventory profile data.
/// </summary>
public sealed class SunriseInventorySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly StationSpawningSystem _spawn = default!;
    [Dependency] private readonly PlayerCacheManager _playerCache = default!;
    [Dependency] private readonly SponsorValidationSystem _validation = default!;
    [Dependency] private readonly IServerPreferencesManager _preferences = default!;

    private const int MaxInventoryPurchaseIdLength = 128;
    private const int MaxPetSelectionIdLength = 128;

    private readonly Dictionary<NetUserId, Dictionary<int, SunriseInventoryProfile>> _profiles = new();
    private ISharedSponsorsManager? _sponsors;

    public override void Initialize()
    {
        base.Initialize();

        IoCManager.Instance!.TryResolveType(out _sponsors);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeNetworkEvent<SunriseInventoryInitialDataRequestEvent>(OnInitialDataRequest);
        SubscribeNetworkEvent<SunriseInventoryPetSelectedEvent>(OnPetSelected);
        SubscribeNetworkEvent<SunriseInventoryProfileChangedEvent>(OnInventoryProfileChanged);
        SubscribeNetworkEvent<SunriseInventoryPurchaseRequestEvent>(OnPurchaseRequest);
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

    /// <summary>
    /// Applies a validated sponsor inventory profile to the spawned mob for the selected job.
    /// </summary>
    public bool TryApplyInventory(
        EntityUid mob,
        SunriseInventoryProfile profile,
        string? jobId,
        ICommonSession session)
    {
        if (_sponsors == null)
            return false;

        profile ??= new SunriseInventoryProfile();

        var validInventory = SunriseInventoryValidation.EnsureValid(profile, session, _prototype, _sponsors);
        var selection = SunriseInventoryValidation.GetEffectiveSelection(validInventory, jobId);
        if (selection.IsEmpty())
            return true;

        var config = _sponsors.GetSponsorInventoryConfig();
        var items = config?.Items ?? [];
        var itemLookup = items
            .Where(i => i != null && !string.IsNullOrWhiteSpace(i.Id))
            .ToDictionary(i => i.Id);

        var usedItems = new HashSet<string>();
        foreach (var (slot, itemId) in selection.SlotItems)
        {
            if (!usedItems.Add(itemId) || !itemLookup.TryGetValue(itemId, out var item))
                continue;

            TryEquipSponsorItem(mob, slot, item);
        }

        if (!_inventory.TryGetSlotEntity(mob, "back", out var backEntity) ||
            !TryComp<StorageComponent>(backEntity, out var storage))
        {
            return true;
        }

        foreach (var itemId in selection.BagItems)
        {
            if (!usedItems.Add(itemId) || !itemLookup.TryGetValue(itemId, out var item))
                continue;

            TryStoreSponsorItem(backEntity.Value, storage, item);
        }

        return true;
    }

    /// <summary>
    /// Validates and persists a sponsor inventory profile for one character slot.
    /// </summary>
    public async Task<bool> TrySetInventoryProfileAsync(ICommonSession session, int slot, SunriseInventoryProfile profile)
    {
        if (_sponsors == null || profile == null || !CanSetInventoryProfile(session, slot))
            return false;

        await SetInventoryProfile(session, slot, profile);
        return true;
    }

    /// <summary>
    /// Saves the selected sponsor pet if the player is allowed to use it.
    /// </summary>
    public bool TrySetPetSelection(NetUserId userId, string? petSelection)
    {
        if (!CanSetPetSelection(userId, petSelection))
            return false;

        SetPetSelection(userId, petSelection);
        return true;
    }

    private bool CanSetPetSelection(NetUserId userId, string? petSelection)
    {
        return string.IsNullOrEmpty(petSelection) ||
               petSelection.Length <= MaxPetSelectionIdLength &&
               _validation.ValidatePetSelection(petSelection, userId);
    }

    private void SetPetSelection(NetUserId userId, string? petSelection)
    {
        if (!_playerCache.TryGetCache(userId, out var cache))
            cache = new PlayerCacheData();

        cache.Pet = petSelection ?? string.Empty;
        _playerCache.SetCache(userId, cache);
    }

    private bool CanSetInventoryProfile(ICommonSession session, int slot)
    {
        return slot >= 0 &&
               _preferences.TryGetCachedPreferences(session.UserId, out var preferences) &&
               preferences.Characters.ContainsKey(slot);
    }

    private async Task SendInitialData(ICommonSession session)
    {
        var initialData = _sponsors != null
            ? await _sponsors.GetSponsorInventoryInitialDataAsync(session.UserId)
            : new SponsorInventoryInitialData();
        initialData ??= new SponsorInventoryInitialData();

        var profiles = new Dictionary<int, SunriseInventoryProfile>();
        if (_preferences.TryGetCachedPreferences(session.UserId, out var preferences))
        {
            foreach (var slot in preferences.Characters.Keys)
            {
                var profile = await GetInventoryProfileAsync(session.UserId, slot, initialData);
                var validProfile = _sponsors != null
                    ? SunriseInventoryValidation.EnsureValid(profile, session, _prototype, _sponsors)
                    : new SunriseInventoryProfile();
                validProfile = EnsureValidForCharacter(session, slot, validProfile);

                if (!validProfile.IsEmpty())
                    profiles[slot] = validProfile;

                SetInventoryProfileCache(session.UserId, slot, validProfile);
            }
        }

        var config = _sponsors?.GetSponsorInventoryConfig() ?? new SponsorInventoryConfig();
        config.Items ??= [];
        config.Packs ??= [];
        var catalogVersion = string.IsNullOrWhiteSpace(initialData.CatalogVersion)
            ? config.Version ?? string.Empty
            : initialData.CatalogVersion;

        var ev = new SunriseInventoryInitialDataEvent(
            config,
            catalogVersion,
            initialData.SponsorTier,
            (initialData.OwnedItemIds ?? []).ToList(),
            profiles,
            initialData.Balance,
            initialData.Revision ?? string.Empty);

        RaiseNetworkEvent(ev, session);
    }

    private async Task SetInventoryProfile(ICommonSession session, int slot, SunriseInventoryProfile profile)
    {
        if (_sponsors == null)
            return;

        var validProfile = SunriseInventoryValidation.EnsureValid(
            profile,
            session,
            _prototype,
            _sponsors);
        validProfile = EnsureValidForCharacter(session, slot, validProfile);

        var saved = await _sponsors.SaveSponsorInventoryProfileAsync(session.UserId, slot, validProfile.ToInfo());
        var savedProfile = SunriseInventoryProfile.FromInfo(saved.Profile);
        var syncedProfile = SunriseInventoryValidation.EnsureValid(
            savedProfile,
            session,
            _prototype,
            _sponsors);
        syncedProfile = EnsureValidForCharacter(session, slot, syncedProfile);

        SetInventoryProfileCache(session.UserId, slot, syncedProfile);
        RaiseNetworkEvent(new SunriseInventoryProfileSyncedEvent(slot, syncedProfile, saved.Revision), session);
    }

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
            var config = _sponsors?.GetSponsorInventoryConfig();
            var items = config?.Items ?? [];
            var itemLookup = items
                .Where(i => i != null && !string.IsNullOrWhiteSpace(i.Id))
                .ToDictionary(i => i.Id);
            var usedItems = new HashSet<string>();

            foreach (var (slot, itemId) in selection.SlotItems)
            {
                if (!usedItems.Add(itemId) ||
                    !itemLookup.TryGetValue(itemId, out var item))
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
                    !itemLookup.TryGetValue(itemId, out var item))
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
        if (!_prototype.TryIndex<SpeciesPrototype>(profile.Species, out var species))
            return null;

        var dummy = Spawn(species.DollPrototype, MapCoordinates.Nullspace);
        _humanoid.LoadProfile(dummy, profile);

        if (jobId == null || !_prototype.TryIndex<JobPrototype>(jobId, out var job))
            return dummy;

        var jobLoadoutId = LoadoutSystem.GetJobPrototype(job.ID);
        var effectiveJobLoadoutId = LoadoutSystem.GetEffectiveRolePrototype(jobLoadoutId, _prototype);
        if (_prototype.TryIndex<RoleLoadoutPrototype>(effectiveJobLoadoutId, out var roleProto))
        {
            var sponsorPrototypes = GetSponsorLoadoutPrototypes(session);
            profile.Loadouts.TryGetValue(jobLoadoutId, out var loadout);
            loadout ??= new RoleLoadout(jobLoadoutId);
            loadout.SetDefault(profile, session, _prototype, sponsorPrototypes);
            _spawn.EquipRoleLoadout(dummy, loadout, roleProto);
        }

        if (job.StartingGear != null)
            _spawn.EquipStartingGear(dummy, job.StartingGear, raiseEvent: false);

        return dummy;
    }

    private string[] GetSponsorLoadoutPrototypes(ICommonSession session)
    {
        if (_sponsors != null &&
            _sponsors.TryGetPrototypes(session.UserId, out var prototypes) &&
            prototypes != null)
        {
            return prototypes.ToArray();
        }

        return [];
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

    private async Task TryPurchaseInventoryItem(ICommonSession session, SunriseInventoryPurchaseRequestEvent ev)
    {
        var sponsors = _sponsors;
        if (sponsors == null)
            return;

        if (!TryBuildPurchaseRequest(sponsors, ev, out var request, out var error))
        {
            RaiseNetworkEvent(new SunriseInventoryPurchaseResultEvent(new SponsorInventoryPurchaseResult
            {
                Success = false,
                Error = error,
            }), session);
            return;
        }

        var result = await sponsors.PurchaseSponsorInventoryItemAsync(
            session.UserId,
            request);

        RaiseNetworkEvent(new SunriseInventoryPurchaseResultEvent(result), session);

        if (result.Success)
            await SendInitialData(session);
    }

    private bool TryBuildPurchaseRequest(
        ISharedSponsorsManager sponsors,
        SunriseInventoryPurchaseRequestEvent ev,
        out SponsorInventoryPurchaseRequest request,
        out string error)
    {
        request = new SponsorInventoryPurchaseRequest();
        error = "invalid-request";

        var hasItem = !string.IsNullOrWhiteSpace(ev.ItemId);
        var hasPack = !string.IsNullOrWhiteSpace(ev.PackId);
        if (hasItem == hasPack)
            return false;

        if (!Guid.TryParseExact(ev.IdempotencyKey, "N", out _))
        {
            error = "invalid-idempotency-key";
            return false;
        }

        var config = sponsors.GetSponsorInventoryConfig();
        if (hasItem)
        {
            if (!IsReasonablePurchaseId(ev.ItemId) ||
                !CatalogContainsItem(config, ev.ItemId))
            {
                error = "unknown-item";
                return false;
            }
        }

        if (hasPack)
        {
            if (!IsReasonablePurchaseId(ev.PackId) ||
                !CatalogContainsPack(config, ev.PackId))
            {
                error = "unknown-pack";
                return false;
            }
        }

        request = new SponsorInventoryPurchaseRequest
        {
            ItemId = hasItem ? ev.ItemId : null,
            PackId = hasPack ? ev.PackId : null,
            IdempotencyKey = ev.IdempotencyKey,
        };
        return true;
    }

    private static bool IsReasonablePurchaseId(string? id)
    {
        return !string.IsNullOrWhiteSpace(id) && id.Length <= MaxInventoryPurchaseIdLength;
    }

    private static bool CatalogContainsItem(SponsorInventoryConfig config, string? itemId)
    {
        foreach (var item in config.Items ?? [])
        {
            if (item?.Id == itemId)
                return true;
        }

        return false;
    }

    private static bool CatalogContainsPack(SponsorInventoryConfig config, string? packId)
    {
        foreach (var pack in config.Packs ?? [])
        {
            if (pack?.Id == packId)
                return true;
        }

        return false;
    }

    private async ValueTask<SunriseInventoryProfile> GetSelectedInventoryProfileAsync(NetUserId userId)
    {
        var preferences = _preferences.GetPreferencesOrNull(userId);
        if (preferences == null)
            return new SunriseInventoryProfile();

        return await GetInventoryProfileAsync(userId, preferences.SelectedCharacterIndex);
    }

    private async ValueTask<SunriseInventoryProfile> GetInventoryProfileAsync(
        NetUserId userId,
        int slot,
        SponsorInventoryInitialData? initialData = null)
    {
        if (initialData?.Profiles != null &&
            initialData.Profiles.TryGetValue(slot, out var initialDataProfile))
        {
            return SunriseInventoryProfile.FromInfo(initialDataProfile);
        }

        if (_profiles.TryGetValue(userId, out var profiles) &&
            profiles.TryGetValue(slot, out var profile))
            return profile.Clone();

        if (_sponsors == null)
            return new SunriseInventoryProfile();

        return SunriseInventoryProfile.FromInfo(await _sponsors.GetSponsorInventoryProfileAsync(userId, slot));
    }

    private void SetInventoryProfileCache(NetUserId userId, int slot, SunriseInventoryProfile profile)
    {
        if (!_profiles.TryGetValue(userId, out var profiles))
        {
            profiles = new Dictionary<int, SunriseInventoryProfile>();
            _profiles[userId] = profiles;
        }

        if (profile.IsEmpty())
            profiles.Remove(slot);
        else
            profiles[slot] = profile.Clone();

        if (profiles.Count == 0)
            _profiles.Remove(userId);
    }

    private bool TryEquipSponsorItem(EntityUid mob, string slot, SponsorInventoryItemInfo item)
    {
        var spawned = Spawn(item.EntityPrototype, Transform(mob).Coordinates);

        if (!_inventory.CanEquip(mob, spawned, slot, out _))
        {
            QueueDel(spawned);
            return false;
        }

        List<EntityUid>? previousStorageItems = null;
        EntityUid? previousItem = null;
        if (_inventory.TryGetSlotEntity(mob, slot, out var existing))
        {
            previousItem = existing.Value;
            previousStorageItems = TakeStorageItems(previousItem.Value);

            if (!_inventory.TryUnequip(mob, slot, out _, silent: true, force: true, reparent: false))
            {
                MoveStorageItemsOrDelete(previousItem.Value, previousStorageItems);
                QueueDel(spawned);
                return false;
            }
        }

        if (_inventory.TryEquip(mob, spawned, slot, silent: true))
        {
            if (previousStorageItems != null && !TryMoveStorageItems(spawned, previousStorageItems))
            {
                _inventory.TryUnequip(mob, slot, out _, silent: true, force: true, reparent: false);

                if (previousItem != null)
                {
                    _inventory.TryEquip(mob, previousItem.Value, slot, silent: true, force: true);
                    MoveStorageItemsOrDelete(previousItem.Value, previousStorageItems);
                }

                QueueDel(spawned);
                return false;
            }

            if (previousItem != null)
                QueueDel(previousItem.Value);

            return true;
        }

        if (previousItem != null)
        {
            _inventory.TryEquip(mob, previousItem.Value, slot, silent: true, force: true);

            if (previousStorageItems != null)
                MoveStorageItemsOrDelete(previousItem.Value, previousStorageItems);
        }

        QueueDel(spawned);
        return false;
    }

    private bool TryStoreSponsorItem(EntityUid storageEntity, StorageComponent storage, SponsorInventoryItemInfo item)
    {
        var spawned = Spawn(item.EntityPrototype, Transform(storageEntity).Coordinates);
        if (!TryComp<ItemComponent>(spawned, out var itemComp))
        {
            QueueDel(spawned);
            return false;
        }

        if (_storage.TryGetAvailableGridSpace((storageEntity, storage), (spawned, itemComp), out var location) &&
            _storage.InsertAt((storageEntity, storage), (spawned, itemComp), location.Value, out _, playSound: false, stackAutomatically: false))
        {
            return true;
        }

        QueueDel(spawned);
        return false;
    }

    private List<EntityUid> TakeStorageItems(EntityUid storageEntity)
    {
        var items = new List<EntityUid>();
        if (!TryComp<StorageComponent>(storageEntity, out var storage))
            return items;

        foreach (var contained in storage.Container.ContainedEntities.ToArray())
        {
            if (!_container.Remove(contained, storage.Container, reparent: false, force: true))
                continue;

            items.Add(contained);
        }

        return items;
    }

    private bool TryMoveStorageItems(EntityUid storageEntity, List<EntityUid> items)
    {
        if (!TryComp<StorageComponent>(storageEntity, out var storage))
            return items.Count == 0;

        var inserted = new List<EntityUid>();
        foreach (var item in items)
        {
            if (!TryComp<ItemComponent>(item, out var itemComp) ||
                !_storage.TryGetAvailableGridSpace((storageEntity, storage), (item, itemComp), out var location) ||
                !_storage.InsertAt((storageEntity, storage), (item, itemComp), location.Value, out _, playSound: false, stackAutomatically: false))
            {
                foreach (var insertedItem in inserted)
                {
                    _container.Remove(insertedItem, storage.Container, reparent: false, force: true);
                }

                return false;
            }

            inserted.Add(item);
        }

        return true;
    }

    private void MoveStorageItemsOrDelete(EntityUid storageEntity, List<EntityUid> items)
    {
        if (TryMoveStorageItems(storageEntity, items))
            return;

        foreach (var item in items)
        {
            QueueDel(item);
        }
    }
}
