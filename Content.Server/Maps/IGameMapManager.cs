using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Server.Maps;

/// <summary>
/// Manages which station map will be used for the next round.
/// </summary>
public interface IGameMapManager
{
    void Initialize();

    /// <summary>
    /// Returns all maps eligible to be played right now.
    /// </summary>
    /// <returns>enumerator of map prototypes</returns>
    IEnumerable<GameMapPrototype> CurrentlyEligibleMaps();
    // Sunrise-Start
    IEnumerable<string> CurrentlyExcludedMaps();
    void ClearExcludedMaps();
    void AddExcludedMap(string mapId);
    // Prison-specific excluded maps to avoid cross-contamination between station and prison rotations
    IEnumerable<string> CurrentlyExcludedPrisonMaps();
    void ClearExcludedPrisonMaps();
    void AddExcludedPrisonMap(string mapId);
    void AddPrisonMap();
    /// <summary>
    /// Enqueue a played prison map into the prison rotation memory.
    /// </summary>
    void EnqueuePrisonMap(string mapProtoName);

    /// <summary>
    /// Returns prison maps ordered by rotation priority (least recently played first).
    /// </summary>
    IEnumerable<GameMapPrototype> PrisonMapsOrderedByRotation();
    /// <summary>
    /// Set the next prison map selection (used by votes) which will be consumed when prison is spawned.
    /// </summary>
    void SetNextPrisonMap(string mapProtoName);

    /// <summary>
    /// Try to consume the next set prison map selection. Returns true and outputs the proto name if one was set.
    /// </summary>
    bool TryConsumeNextPrisonMap(out ProtoId<GameMapPrototype>? mapProto);
    // Sunrise-End

    /// <summary>
    /// Returns all maps that can be voted for.
    /// </summary>
    /// <returns>enumerator of map prototypes</returns>
    IEnumerable<GameMapPrototype> AllVotableMaps();

    /// <summary>
    /// Returns all maps.
    /// </summary>
    /// <returns>enumerator of map prototypes</returns>
    IEnumerable<GameMapPrototype> AllMaps();

    /// <summary>
    /// Gets the currently selected map
    /// </summary>
    /// <returns>selected map</returns>
    GameMapPrototype? GetSelectedMap();

    /// <summary>
    /// Clears the selected map, if any
    /// </summary>
    void ClearSelectedMap();

    /// <summary>
    /// Attempts to select the given map, checking eligibility criteria
    /// </summary>
    /// <param name="gameMap">map prototype</param>
    /// <returns>success or failure</returns>
    bool TrySelectMapIfEligible(string gameMap);

    /// <summary>
    /// Select the given map regardless of eligibility
    /// </summary>
    /// <param name="gameMap">map prototype</param>
    /// <returns>success or failure</returns>
    void SelectMap(string gameMap);

    /// <summary>
    /// Selects a random map eligible map
    /// </summary>
    void SelectMapRandom();

    /// <summary>
    /// Selects the map at the front of the rotation queue
    /// </summary>
    /// <returns>selected map</returns>
    void SelectMapFromRotationQueue(bool markAsPlayed = false);

    /// <summary>
    /// Selects the map by following rules set in the config
    /// </summary>
    public void SelectMapByConfigRules();

    /// <summary>
    /// Checks if the given map exists
    /// </summary>
    /// <param name="gameMap">name of the map</param>
    /// <returns>existence</returns>
    bool CheckMapExists(string gameMap);
}
