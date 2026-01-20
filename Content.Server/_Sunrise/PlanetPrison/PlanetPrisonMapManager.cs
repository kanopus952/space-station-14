using System.Collections.Generic;
using System.Linq;
using Content.Server.Maps;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.CCVar;
using Content.Shared.Maps;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.PlanetPrison;

public sealed class PlanetPrisonMapManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private readonly HashSet<GameMapPrototype> _prisonMaps = new();
    private readonly Queue<string> _previousPrisonMaps = new();
    private readonly HashSet<string> _excludedPrisonMaps = new();
    private string? _nextPrisonSelection;
    private int _mapQueueDepth = 1;
    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("mapsel");
        _cfg.OnValueChanged(CCVars.GameMapMemoryDepth, value =>
        {
            _mapQueueDepth = value;
            while (_previousPrisonMaps.Count > _mapQueueDepth)
            {
                _previousPrisonMaps.Dequeue();
            }
        }, true);
    }

    public IEnumerable<string> CurrentlyExcludedPrisonMaps()
    {
        return _excludedPrisonMaps;
    }

    public void AddExcludedPrisonMap(string mapId)
    {
        if (!_cfg.GetCVar(SunriseCCVars.ExcludePrisonMaps))
            return;

        _excludedPrisonMaps.Add(mapId);
    }

    public void ClearExcludedPrisonMaps()
    {
        _excludedPrisonMaps.Clear();
    }

    public IEnumerable<GameMapPrototype> PrisonMapsOrderedByRotation()
    {
        var eligible = _prisonMaps
            .Select(x => (proto: x, weight: GetPrisonRotationQueuePriority(x.ID)))
            .OrderByDescending(x => x.weight)
            .ThenBy(x => x.proto.ID)
            .ToArray();

        return eligible.Select(x => x.proto);
    }

    public void EnqueuePrisonMap(string mapProtoName)
    {
        if (string.IsNullOrEmpty(mapProtoName))
            return;

        _previousPrisonMaps.Enqueue(mapProtoName);
        while (_previousPrisonMaps.Count > _mapQueueDepth)
        {
            _previousPrisonMaps.Dequeue();
        }
    }

    public void SetNextPrisonMap(string mapProtoName)
    {
        _nextPrisonSelection = mapProtoName;
    }

    public bool TryConsumeNextPrisonMap(out ProtoId<GameMapPrototype>? mapProtoName)
    {
        if (_nextPrisonSelection == null)
        {
            mapProtoName = null;
            return false;
        }

        mapProtoName = _nextPrisonSelection;
        _nextPrisonSelection = null;
        return true;
    }

    public void AddPrisonMap()
    {
        var prisonPool = _cfg.GetCVar(SunriseCCVars.PlanetPrisonMapPool);

        if (_prototypeManager.TryIndex<GameMapPoolPrototype>(prisonPool, out var pool))
        {
            foreach (var map in pool.Maps)
            {
                if (!_prototypeManager.TryIndex<GameMapPrototype>(map, out var mapProto))
                {
                    _sawmill.Error($"Couldn't index prison map {map} in pool {prisonPool}");
                    continue;
                }

                _prisonMaps.Add(mapProto);
            }

            return;
        }
    }

    public bool HasPrisonMaps()
    {
        return _prisonMaps.Count > 0;
    }

    private int GetPrisonRotationQueuePriority(string gameMapProtoName)
    {
        var i = 0;
        foreach (var map in _previousPrisonMaps.Reverse())
        {
            if (map == gameMapProtoName)
                return i;
            i++;
        }
        return _mapQueueDepth;
    }
}
