using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Sunrise.Interfaces.Shared;

public interface ISharedSponsorsManager
{
    public void Initialize();

    public event Action? LoadedSponsorInfo;
    public event Action<List<SponsorInfo>>? LoadedSponsorTiers;
    public event Action<SponsorInventoryConfig>? LoadedSponsorInventoryConfig
    {
        add { }
        remove { }
    }

    // Client
    public List<string> GetClientPrototypes();
    public bool ClientAllowedRespawn();
    public bool ClientAllowedFlavor();
    public int ClientGetSizeFlavor();

    public bool ClientIsSponsor();
    public List<SponsorInfo> GetSponsorTiers();
    public SponsorInventoryConfig GetSponsorInventoryConfig() => new();
    public List<string> GetClientPurchasedInventoryItems() => [];
    public SponsorInventoryInitialData GetClientSponsorInventoryInitialData() => new();

    // Server
    public bool TryGetPrototypes(NetUserId userId, [NotNullWhen(true)] out List<string>? prototypes);
    public bool TryGetOocTitle(NetUserId userId, [NotNullWhen(true)] out string? title);
    public bool TryGetOocColor(NetUserId userId, [NotNullWhen(true)] out Color? color);
    public bool TryGetSpawnEquipment(NetUserId userId, [NotNullWhen(true)] out string? spawnEquipment);
    public bool TryGetGhostThemes(NetUserId userId, [NotNullWhen(true)] out List<string>? ghostTheme);
    public bool TryGetBypassRoles(NetUserId userId, [NotNullWhen(true)] out List<string>? bypassRoles);
    public int GetSizeFlavor(NetUserId userId);
    public bool IsAllowedFlavor(NetUserId userId);
    public int GetExtraCharSlots(NetUserId userId);
    public bool HavePriorityJoin(NetUserId userId);
    public bool IsSponsor(NetUserId userId);
    public bool IsAllowedRespawn(NetUserId userId);
    public List<ICommonSession> PickPrioritySessions(List<ICommonSession> sessions, string roleId);
    public NetUserId? PickRoleSession(HashSet<NetUserId> users, string roleId);
    public bool TryGetPriorityGhostRoles(NetUserId userId, [NotNullWhen(true)] out List<string>? priorityAntags);
    public bool TryGetPriorityAntags(NetUserId userId, [NotNullWhen(true)] out List<string>? priorityAntags);
    public bool TryGetPriorityRoles(NetUserId userId, [NotNullWhen(true)] out List<string>? priorityRoles);
    public bool TryGetPets(NetUserId userId, [NotNullWhen(true)] out List<string>? petSelections);
    public int GetSponsorTier(NetUserId userId) => 0;

    public bool TryGetPurchasedInventoryItems(NetUserId userId, [NotNullWhen(true)] out List<string>? inventoryItems)
    {
        inventoryItems = null;
        return false;
    }

    /// <summary>
    /// Loads catalog, ownership, profile, and revision data for the sponsor inventory UI.
    /// </summary>
    public ValueTask<SponsorInventoryInitialData> GetSponsorInventoryInitialDataAsync(
        NetUserId userId,
        CancellationToken cancel = default)
    {
        var config = GetSponsorInventoryConfig();
        var ownedItemIds = TryGetPurchasedInventoryItems(userId, out var items) && items != null
            ? items.ToArray()
            : Array.Empty<string>();

        return ValueTask.FromResult(new SponsorInventoryInitialData
        {
            CatalogVersion = config.Version,
            SponsorTier = GetSponsorTier(userId),
            OwnedItemIds = ownedItemIds,
        });
    }

    /// <summary>
    /// Loads a saved sponsor inventory profile for a character slot.
    /// </summary>
    public ValueTask<SponsorInventoryProfileInfo?> GetSponsorInventoryProfileAsync(
        NetUserId userId,
        int slot,
        CancellationToken cancel = default)
    {
        return ValueTask.FromResult<SponsorInventoryProfileInfo?>(null);
    }

    /// <summary>
    /// Persists a server-validated sponsor inventory profile for a character slot.
    /// </summary>
    public ValueTask<SponsorInventoryProfileSaveResult> SaveSponsorInventoryProfileAsync(
        NetUserId userId,
        int slot,
        SponsorInventoryProfileInfo profile,
        CancellationToken cancel = default)
    {
        return ValueTask.FromResult(new SponsorInventoryProfileSaveResult
        {
            Profile = profile,
        });
    }

    /// <summary>
    /// Purchases one sponsor inventory item or pack through the backing sponsor service.
    /// </summary>
    public ValueTask<SponsorInventoryPurchaseResult> PurchaseSponsorInventoryItemAsync(
        NetUserId userId,
        SponsorInventoryPurchaseRequest request,
        CancellationToken cancel = default)
    {
        return ValueTask.FromResult(new SponsorInventoryPurchaseResult
        {
            Success = false,
            Error = "not-implemented",
        });
    }

    public void Update();
}

/// <summary>
/// Sponsor inventory catalog sent to clients and used by server validation.
/// </summary>
[Serializable, NetSerializable]
public sealed class SponsorInventoryConfig
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public SponsorInventoryItemInfo[] Items { get; set; } = [];

    [JsonPropertyName("packs")]
    public SponsorInventoryPackInfo[] Packs { get; set; } = [];
}

/// <summary>
/// Catalog entry for a single sponsor inventory item.
/// </summary>
[Serializable, NetSerializable]
public sealed class SponsorInventoryItemInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("entityPrototype")]
    public string EntityPrototype { get; set; } = string.Empty;

    [JsonPropertyName("availableJobs")]
    public string[]? AvailableJobs { get; set; }

    [JsonPropertyName("sponsorLevel")]
    public int? SponsorLevel { get; set; }

    [JsonPropertyName("price")]
    public int Price { get; set; }
}

/// <summary>
/// Catalog entry for a purchasable sponsor inventory pack.
/// </summary>
[Serializable, NetSerializable]
public sealed class SponsorInventoryPackInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("previewEntityPrototype")]
    public string? PreviewEntityPrototype { get; set; }

    [JsonPropertyName("inventoryItemIds")]
    public string[] InventoryItemIds { get; set; } = [];

    [JsonPropertyName("price")]
    public int Price { get; set; }
}

/// <summary>
/// Initial sponsor inventory snapshot for one player session.
/// </summary>
[Serializable, NetSerializable]
public sealed class SponsorInventoryInitialData
{
    [JsonPropertyName("catalogVersion")]
    public string CatalogVersion { get; set; } = string.Empty;

    [JsonPropertyName("sponsorTier")]
    public int SponsorTier { get; set; }

    [JsonPropertyName("ownedItemIds")]
    public string[] OwnedItemIds { get; set; } = [];

    [JsonPropertyName("profiles")]
    public Dictionary<int, SponsorInventoryProfileInfo> Profiles { get; set; } = new();

    [JsonPropertyName("balance")]
    public int? Balance { get; set; }

    [JsonPropertyName("revision")]
    public string Revision { get; set; } = string.Empty;
}

/// <summary>
/// Flexible response shape accepted from the external sponsor inventory initial-data endpoint.
/// </summary>
[Serializable]
public sealed class SponsorInventoryInitialDataApiResponse
{
    [JsonPropertyName("config")]
    public SponsorInventoryConfig? Config { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("catalogVersion")]
    public string? CatalogVersion { get; set; }

    [JsonPropertyName("items")]
    public SponsorInventoryItemInfo[]? Items { get; set; }

    [JsonPropertyName("packs")]
    public SponsorInventoryPackInfo[]? Packs { get; set; }

    [JsonPropertyName("sponsorTier")]
    public int? SponsorTier { get; set; }

    [JsonPropertyName("ownedItemIds")]
    public string[]? OwnedItemIds { get; set; }

    [JsonPropertyName("profiles")]
    public Dictionary<int, SponsorInventoryProfileInfo>? Profiles { get; set; }

    [JsonPropertyName("balance")]
    public int? Balance { get; set; }

    [JsonPropertyName("revision")]
    public string? Revision { get; set; }

    public SponsorInventoryConfig ToConfig()
    {
        var config = Config ?? new SponsorInventoryConfig
        {
            Items = Items ?? [],
            Packs = Packs ?? [],
        };

        if (string.IsNullOrWhiteSpace(config.Version))
            config.Version = !string.IsNullOrWhiteSpace(CatalogVersion)
                ? CatalogVersion
                : Version ?? string.Empty;

        config.Items ??= [];
        config.Packs ??= [];
        return config;
    }

    public SponsorInventoryInitialData ToInfo(SponsorInventoryConfig config, int fallbackSponsorTier)
    {
        return new SponsorInventoryInitialData
        {
            CatalogVersion = !string.IsNullOrWhiteSpace(CatalogVersion)
                ? CatalogVersion
                : config.Version,
            SponsorTier = SponsorTier ?? fallbackSponsorTier,
            OwnedItemIds = OwnedItemIds ?? [],
            Profiles = Profiles ?? new Dictionary<int, SponsorInventoryProfileInfo>(),
            Balance = Balance,
            Revision = Revision ?? string.Empty,
        };
    }
}

/// <summary>
/// External sponsor inventory profile DTO.
/// </summary>
[Serializable, NetSerializable]
public sealed class SponsorInventoryProfileInfo
{
    [JsonPropertyName("global")]
    public SponsorInventorySelectionInfo Global { get; set; } = new();

    [JsonPropertyName("jobs")]
    public Dictionary<string, SponsorInventorySelectionInfo> Jobs { get; set; } = new();
}

/// <summary>
/// External sponsor inventory selection DTO for one global or job-specific layer.
/// </summary>
[Serializable, NetSerializable]
public sealed class SponsorInventorySelectionInfo
{
    [JsonPropertyName("slotItems")]
    public Dictionary<string, string> SlotItems { get; set; } = new();

    [JsonPropertyName("bagItems")]
    public List<string> BagItems { get; set; } = [];
}

/// <summary>
/// Purchase request for exactly one sponsor inventory item or pack.
/// </summary>
[Serializable, NetSerializable]
public sealed class SponsorInventoryPurchaseRequest
{
    [JsonPropertyName("itemId")]
    public string? ItemId { get; set; }

    [JsonPropertyName("packId")]
    public string? PackId { get; set; }

    [JsonPropertyName("idempotencyKey")]
    public string IdempotencyKey { get; set; } = string.Empty;
}

/// <summary>
/// API request used to save a server-validated sponsor inventory profile.
/// </summary>
[Serializable]
public sealed class SponsorInventoryProfileSaveApiRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("serverId")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("slot")]
    public int Slot { get; set; }

    [JsonPropertyName("profile")]
    public SponsorInventoryProfileInfo Profile { get; set; } = new();
}

/// <summary>
/// Result returned after saving a sponsor inventory profile.
/// </summary>
[Serializable, NetSerializable]
public sealed class SponsorInventoryProfileSaveResult
{
    [JsonPropertyName("profile")]
    public SponsorInventoryProfileInfo Profile { get; set; } = new();

    [JsonPropertyName("revision")]
    public string Revision { get; set; } = string.Empty;
}

/// <summary>
/// Result returned after a sponsor inventory purchase attempt.
/// </summary>
[Serializable, NetSerializable]
public sealed class SponsorInventoryPurchaseResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("ownedItemIds")]
    public string[] OwnedItemIds { get; set; } = [];

    [JsonPropertyName("balance")]
    public int? Balance { get; set; }

    [JsonPropertyName("revision")]
    public string Revision { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// API request used to purchase one sponsor inventory item or pack.
/// </summary>
[Serializable]
public sealed class SponsorInventoryPurchaseApiRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("serverId")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("itemId")]
    public string? ItemId { get; set; }

    [JsonPropertyName("packId")]
    public string? PackId { get; set; }

    [JsonPropertyName("idempotencyKey")]
    public string IdempotencyKey { get; set; } = string.Empty;
}

[Serializable, NetSerializable]
public sealed class SponsorInfo
{
    [JsonPropertyName("tier")]
    public int Tier { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("oocColor")]
    public string? OOCColor { get; set; }

    [JsonPropertyName("priorityJoin")]
    public bool HavePriorityJoin { get; set; } = false;

    [JsonPropertyName("extraSlots")]
    public int ExtraSlots { get; set; }

    [JsonPropertyName("allowedRespawn")]
    public bool AllowedRespawn { get; set; } = false;

    [JsonPropertyName("allowedFlavor")]
    public bool AllowedFlavor { get; set; } = false;

    [JsonPropertyName("sizeFlavor")]
    public int SizeFlavor { get; set; }

    [JsonPropertyName("ghostThemes")]
    public string[] GhostThemes { get; set; } = [];

    [JsonPropertyName("pets")]
    public string[] Pets { get; set; } = [];

    [JsonPropertyName("spawnEquipment")]
    public string? SpawnEquipment { get; set; }

    [JsonPropertyName("allowedMarkings")]
    public string[] AllowedMarkings { get; set; } = [];

    [JsonPropertyName("allowedVoices")]
    public string[] AllowedVoices { get; set; } = [];

    [JsonPropertyName("allowedLoadouts")]
    public string[] AllowedLoadouts { get; set; } = [];

    [JsonPropertyName("allowedSpecies")]
    public string[] AllowedSpecies { get; set; } = [];

    [JsonPropertyName("openAntags")]
    public string[] OpenAntags { get; set; } = [];

    [JsonPropertyName("openRoles")]
    public string[] OpenRoles { get; set; } = [];

    [JsonPropertyName("openGhostRoles")]
    public string[] OpenGhostRoles { get; set; } = [];

    [JsonPropertyName("priorityAntags")]
    public string[] PriorityAntags { get; set; } = [];

    [JsonPropertyName("priorityRoles")]
    public string[] PriorityRoles { get; set; } = [];

    [JsonPropertyName("priorityGhostRoles")]
    public string[] PriorityGhostRoles { get; set; } = [];

    [JsonPropertyName("BypassRoles")]
    public string[] BypassRoles { get; set; } = [];
}
