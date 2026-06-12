using System.Collections.Generic;
using System.Linq;
using Content.Server._Sunrise.PlayerCache;
using Content.Server._Sunrise.SponsorValidation;
using Content.Server.Preferences.Managers;
using Content.Shared._Sunrise.PlayerCache;
using Content.Shared._Sunrise.SponsorInventory;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.SponsorInventory;

public sealed class SunriseInventorySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly PlayerCacheManager _playerCache = default!;
    [Dependency] private readonly SponsorValidationSystem _validation = default!;
    [Dependency] private readonly IServerPreferencesManager _preferences = default!;

    private readonly Dictionary<NetUserId, Dictionary<int, SunriseInventoryProfile>> _profiles = new();
    private ISharedSponsorsManager? _sponsors;

    public override void Initialize()
    {
        base.Initialize();

        IoCManager.Instance!.TryResolveType(out _sponsors);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeNetworkEvent<SunriseInventoryPetSelectedEvent>(OnPetSelected);
        SubscribeNetworkEvent<SunriseInventoryProfileChangedEvent>(OnInventoryProfileChanged);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        TryApplyInventory(ev.Mob, GetInventoryProfile(ev.Player.UserId), ev.JobId, ev.Player);
    }

    private void OnPetSelected(SunriseInventoryPetSelectedEvent ev, EntitySessionEventArgs args)
    {
        TrySetPetSelection(args.SenderSession.UserId, ev.SelectedPetSelection);
    }

    private void OnInventoryProfileChanged(SunriseInventoryProfileChangedEvent ev, EntitySessionEventArgs args)
    {
        TrySetInventoryProfile(args.SenderSession, ev.Slot, ev.Profile);
    }

    public bool TryApplyInventory(
        EntityUid mob,
        SunriseInventoryProfile profile,
        string? jobId,
        ICommonSession session)
    {
        if (_sponsors == null)
            return false;

        var validInventory = SunriseInventoryValidation.EnsureValid(profile, session, _prototype, _sponsors);
        var selection = SunriseInventoryValidation.GetEffectiveSelection(validInventory, jobId);
        if (selection.IsEmpty())
            return true;

        var items = _sponsors.GetSponsorInventoryConfig()
            .Items
            .Where(i => !string.IsNullOrWhiteSpace(i.Id))
            .ToDictionary(i => i.Id);

        var usedItems = new HashSet<string>();
        foreach (var (slot, itemId) in selection.SlotItems)
        {
            if (!usedItems.Add(itemId) || !items.TryGetValue(itemId, out var item))
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
            if (!usedItems.Add(itemId) || !items.TryGetValue(itemId, out var item))
                continue;

            TryStoreSponsorItem(backEntity.Value, storage, item);
        }

        return true;
    }

    public bool TrySetInventoryProfile(ICommonSession session, int slot, SunriseInventoryProfile profile)
    {
        if (!CanSetInventoryProfile(session, slot))
            return false;

        SetInventoryProfile(session, slot, profile);
        return true;
    }

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

    private void SetInventoryProfile(ICommonSession session, int slot, SunriseInventoryProfile profile)
    {
        if (_sponsors == null)
            return;

        var validProfile = SunriseInventoryValidation.EnsureValid(
            profile,
            session,
            _prototype,
            _sponsors);

        if (!_profiles.TryGetValue(session.UserId, out var profiles))
        {
            profiles = new Dictionary<int, SunriseInventoryProfile>();
            _profiles[session.UserId] = profiles;
        }

        if (validProfile.IsEmpty())
            profiles.Remove(slot);
        else
            profiles[slot] = validProfile.Clone();

        if (profiles.Count == 0)
            _profiles.Remove(session.UserId);
    }

    private SunriseInventoryProfile GetInventoryProfile(NetUserId userId)
    {
        var preferences = _preferences.GetPreferencesOrNull(userId);
        if (preferences == null ||
            !_profiles.TryGetValue(userId, out var profiles) ||
            !profiles.TryGetValue(preferences.SelectedCharacterIndex, out var profile))
        {
            return new SunriseInventoryProfile();
        }

        return profile.Clone();
    }

    private bool TryEquipSponsorItem(EntityUid mob, string slot, SponsorInventoryItemInfo item)
    {
        var spawned = Spawn(item.EntityPrototype, Transform(mob).Coordinates);
        if (_inventory.TryEquip(mob, spawned, slot, silent: true))
            return true;

        QueueDel(spawned);
        return false;
    }

    private bool TryStoreSponsorItem(EntityUid storageEntity, StorageComponent storage, SponsorInventoryItemInfo item)
    {
        var spawned = Spawn(item.EntityPrototype, Transform(storageEntity).Coordinates);
        if (_storage.Insert(storageEntity, spawned, out _, storageComp: storage, playSound: false))
            return true;

        QueueDel(spawned);
        return false;
    }
}
