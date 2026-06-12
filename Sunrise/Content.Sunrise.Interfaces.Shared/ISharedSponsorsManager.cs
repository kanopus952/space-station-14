using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
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
    public void Update();
}

[Serializable, NetSerializable]
public sealed class SponsorInventoryConfig
{
    [JsonPropertyName("items")]
    public SponsorInventoryItemInfo[] Items { get; set; } = [];

    [JsonPropertyName("packs")]
    public SponsorInventoryPackInfo[] Packs { get; set; } = [];
}

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
}

[Serializable, NetSerializable]
public sealed class SponsorInventoryPackInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("inventoryItemIds")]
    public string[] InventoryItemIds { get; set; } = [];
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
