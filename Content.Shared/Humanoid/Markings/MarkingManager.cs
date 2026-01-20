using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Sunrise;
using Content.Shared.Body;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid.Markings;

public sealed class MarkingManager
{
    [Dependency] private readonly IComponentFactory _component = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private FrozenDictionary<HumanoidVisualLayers, FrozenDictionary<string, MarkingPrototype>> _categorizedMarkings = default!;

    public FrozenDictionary<MarkingCategories, FrozenDictionary<string, MarkingPrototype>> CategorizedMarkings = default!;
    public FrozenDictionary<string, MarkingPrototype> Markings = default!;

    public void Initialize()
    {
        _prototype.PrototypesReloaded += OnPrototypeReload;
        CachePrototypes();
    }

    private void CachePrototypes()
    {
        var layerDict = new Dictionary<HumanoidVisualLayers, Dictionary<string, MarkingPrototype>>();
        var categoryDict = new Dictionary<MarkingCategories, Dictionary<string, MarkingPrototype>>();

        foreach (var layer in Enum.GetValues<HumanoidVisualLayers>())
        {
            layerDict.Add(layer, new());
        }

        foreach (var category in Enum.GetValues<MarkingCategories>())
        {
            categoryDict.Add(category, new());
        }

        foreach (var prototype in _prototype.EnumeratePrototypes<MarkingPrototype>())
        {
            try
            {
                layerDict[prototype.BodyPart].Add(prototype.ID, prototype);
                categoryDict[prototype.MarkingCategory].Add(prototype.ID, prototype);
            }
            catch (Exception e)
            {
                throw new Exception($"failed to process {prototype.ID}", e);
            }
        }

        Markings = _prototype.EnumeratePrototypes<MarkingPrototype>().ToFrozenDictionary(x => x.ID);
        _categorizedMarkings = layerDict.ToFrozenDictionary(
            x => x.Key,
            x => x.Value.ToFrozenDictionary());
        CategorizedMarkings = categoryDict.ToFrozenDictionary(
            x => x.Key,
            x => x.Value.ToFrozenDictionary());
    }

    public FrozenDictionary<string, MarkingPrototype> MarkingsByLayer(HumanoidVisualLayers category)
    {
        return _categorizedMarkings[category];
    }

    public FrozenDictionary<string, MarkingPrototype> MarkingsByCategory(MarkingCategories category)
    {
        return CategorizedMarkings[category];
    }

    public IReadOnlyDictionary<string, MarkingPrototype> MarkingsByLayerAndGroupAndSex(
        HumanoidVisualLayers layer,
        ProtoId<MarkingsGroupPrototype> group,
        Sex sex)
    {
        var groupProto = _prototype.Index(group);
        var whitelisted = groupProto.Limits.GetValueOrDefault(layer)?.OnlyGroupWhitelisted ?? groupProto.OnlyGroupWhitelisted;
        var res = new Dictionary<string, MarkingPrototype>();

        foreach (var (key, marking) in MarkingsByLayer(layer))
        {
            if (!CanBeApplied(groupProto, sex, marking, whitelisted))
                continue;

            res.Add(key, marking);
        }

        return res;
    }

    public IReadOnlyDictionary<string, MarkingPrototype> MarkingsByCategoryAndSpecies(
        MarkingCategories category,
        string species)
    {
        var speciesProto = _prototype.Index<SpeciesPrototype>(species);
        var markingPoints = _prototype.Index(speciesProto.MarkingPoints);
        var res = new Dictionary<string, MarkingPrototype>();

        foreach (var (key, marking) in MarkingsByCategory(category))
        {
            if (!markingPoints.Points.TryGetValue(category, out var point))
            {
                Logger.Warning($"Marking {marking.ID} does not have a point for category {category}.");
                continue;
            }

            if ((markingPoints.OnlyWhitelisted || point.OnlyWhitelisted) && marking.SpeciesRestrictions == null)
                continue;

            if (marking.SpeciesRestrictions != null && !marking.SpeciesRestrictions.Contains(species))
                continue;

            res.Add(key, marking);
        }

        return res;
    }

    public IReadOnlyDictionary<string, MarkingPrototype> MarkingsByCategoryAndSex(MarkingCategories category, Sex sex)
    {
        var res = new Dictionary<string, MarkingPrototype>();

        foreach (var (key, marking) in MarkingsByCategory(category))
        {
            if (marking.SexRestriction != null && marking.SexRestriction != sex)
                continue;

            res.Add(key, marking);
        }

        return res;
    }

    public IReadOnlyDictionary<string, MarkingPrototype> MarkingsByCategoryAndSpeciesAndSex(
        MarkingCategories category,
        string species,
        Sex sex)
    {
        var speciesProto = _prototype.Index<SpeciesPrototype>(species);
        var onlyWhitelisted = _prototype.Index(speciesProto.MarkingPoints).OnlyWhitelisted;
        var res = new Dictionary<string, MarkingPrototype>();

        foreach (var (key, marking) in MarkingsByCategory(category))
        {
            if (onlyWhitelisted && marking.SpeciesRestrictions == null)
                continue;

            if (marking.SpeciesRestrictions != null && !marking.SpeciesRestrictions.Contains(species))
                continue;

            if (marking.SexRestriction != null && marking.SexRestriction != sex)
                continue;

            res.Add(key, marking);
        }

        return res;
    }

    public bool TryGetMarking(Marking marking, [NotNullWhen(true)] out MarkingPrototype? markingResult)
    {
        return Markings.TryGetValue(marking.MarkingId, out markingResult);
    }

    public bool IsValidMarking(Marking marking, MarkingCategories category, string species, Sex sex)
    {
        if (!TryGetMarking(marking, out var proto))
            return false;

        if (proto.MarkingCategory != category ||
            proto.SpeciesRestrictions != null && !proto.SpeciesRestrictions.Contains(species) ||
            proto.SexRestriction != null && proto.SexRestriction != sex)
            return false;

        return marking.MarkingColors.Count == proto.Sprites.Count;
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<MarkingPrototype>())
            CachePrototypes();
    }

    public bool CanBeApplied(ProtoId<MarkingsGroupPrototype> group, Sex sex, MarkingPrototype prototype)
    {
        var groupProto = _prototype.Index(group);
        var whitelisted = groupProto.Limits.GetValueOrDefault(prototype.BodyPart)?.OnlyGroupWhitelisted ?? groupProto.OnlyGroupWhitelisted;

        return CanBeApplied(groupProto, sex, prototype, whitelisted);
    }

    private bool CanBeApplied(MarkingsGroupPrototype group, Sex sex, MarkingPrototype prototype, bool whitelisted)
    {
        if (prototype.GroupWhitelist == null)
        {
            if (prototype.SpeciesRestrictions != null && !prototype.SpeciesRestrictions.Contains(group.ID))
                return false;

            if (prototype.SpeciesRestrictions == null && whitelisted)
                return false;
        }
        else if (!prototype.GroupWhitelist.Contains(group))
        {
            return false;
        }

        return prototype.SexRestriction == null || prototype.SexRestriction == sex;
    }

    public bool CanBeApplied(string species, Sex sex, Marking marking, IPrototypeManager? prototypeManager = null)
    {
        IoCManager.Resolve(ref prototypeManager);

        if (!TryGetMarking(marking, out var prototype))
            return false;

        return CanBeApplied(species, sex, prototype, prototypeManager);
    }

    public bool CanBeApplied(string species, Sex sex, MarkingPrototype prototype, IPrototypeManager? prototypeManager = null)
    {
        IoCManager.Resolve(ref prototypeManager);

        var speciesProto = prototypeManager.Index<SpeciesPrototype>(species);
        var onlyWhitelisted = prototypeManager.Index(speciesProto.MarkingPoints).OnlyWhitelisted;

        if (onlyWhitelisted && prototype.SpeciesRestrictions == null)
            return false;

        if (prototype.SpeciesRestrictions != null && !prototype.SpeciesRestrictions.Contains(species))
            return false;

        return prototype.SexRestriction == null || prototype.SexRestriction == sex;
    }

    public bool MustMatchSkin(string species, HumanoidVisualLayers layer, out float alpha, IPrototypeManager? prototypeManager = null)
    {
        IoCManager.Resolve(ref prototypeManager);
        var speciesProto = prototypeManager.Index<SpeciesPrototype>(species);
        if (!prototypeManager.Resolve(speciesProto.BodyTypes.First(), out BodyTypePrototype? baseBodyType) ||
            !baseBodyType.Sprites.TryGetValue(layer, out var spriteName) ||
            !prototypeManager.Resolve(spriteName, out HumanoidSpeciesSpriteLayer? sprite) ||
            sprite is not { MarkingsMatchSkin: true })
        {
            alpha = 1f;
            return false;
        }

        alpha = sprite.LayerAlpha;
        return true;
    }

    /// <summary>
    /// Ensures that the <see cref="markingSets"/> have a valid amount of colors.
    /// </summary>
    public void EnsureValidColors(Dictionary<HumanoidVisualLayers, List<Marking>> markingSets)
    {
        foreach (var markings in markingSets.Values)
        {
            for (var i = markings.Count - 1; i >= 0; i--)
            {
                if (!TryGetMarking(markings[i], out var marking))
                {
                    markings.RemoveAt(i);
                    continue;
                }

                if (marking.Sprites.Count != markings[i].MarkingColors.Count)
                    markings[i] = new Marking(marking.ID, marking.Sprites.Count);
            }
        }
    }

    /// <summary>
    /// Ensures that the <see cref="markingSets"/> are valid per the constraints on <see cref="group"/> and <see cref="sex"/>.
    /// </summary>
    public void EnsureValidGroupAndSex(Dictionary<HumanoidVisualLayers, List<Marking>> markingSets, ProtoId<MarkingsGroupPrototype> group, Sex sex)
    {
        foreach (var markings in markingSets.Values)
        {
            for (var i = markings.Count - 1; i >= 0; i--)
            {
                if (!TryGetMarking(markings[i], out var marking) || !CanBeApplied(group, sex, marking))
                    markings.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Ensures that the <see cref="markingSets"/> only belong to the <see cref="layers"/>.
    /// </summary>
    public void EnsureValidLayers(Dictionary<HumanoidVisualLayers, List<Marking>> markingSets, HashSet<HumanoidVisualLayers> layers)
    {
        foreach (var markings in markingSets.Values)
        {
            for (var i = markings.Count - 1; i >= 0; i--)
            {
                if (!TryGetMarking(markings[i], out var marking) || !layers.Contains(marking.BodyPart))
                    markings.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Ensures the list of <see cref="markingSets"/> is valid per the limits of the <see cref="group"/>.
    /// </summary>
    public void EnsureValidLimits(
        Dictionary<HumanoidVisualLayers, List<Marking>> markingSets,
        ProtoId<MarkingsGroupPrototype> group,
        HashSet<HumanoidVisualLayers> layers,
        Color? skinColor,
        Color? eyeColor)
    {
        var groupProto = _prototype.Index(group);
        var counts = new Dictionary<HumanoidVisualLayers, int>();

        foreach (var (_, markings) in markingSets)
        {
            for (var i = markings.Count - 1; i >= 0; i--)
            {
                if (!TryGetMarking(markings[i], out var marking))
                {
                    markings.RemoveAt(i);
                    continue;
                }

                if (!groupProto.Limits.TryGetValue(marking.BodyPart, out var limit))
                    continue;

                var count = counts.GetValueOrDefault(marking.BodyPart);
                if (count >= limit.Limit)
                {
                    markings.RemoveAt(i);
                    continue;
                }

                counts[marking.BodyPart] = count + 1;
            }
        }

        foreach (var layer in layers)
        {
            if (!groupProto.Limits.TryGetValue(layer, out var layerLimit))
                continue;

            var layerCounts = counts.GetValueOrDefault(layer);
            if (layerCounts > 0 || !layerLimit.Required)
                continue;

            foreach (var marking in layerLimit.Default)
            {
                if (!Markings.TryGetValue(marking, out var markingProto))
                    continue;

                markingSets[layer] = markingSets.GetValueOrDefault(layer) ?? [];
                var colors = MarkingColoring.GetMarkingLayerColors(markingProto, skinColor, eyeColor, markingSets[layer]);
                markingSets[layer].Add(new(marking, colors));
            }
        }
    }

    public Dictionary<ProtoId<OrganCategoryPrototype>, EntProtoId<OrganComponent>> GetOrgans(ProtoId<SpeciesPrototype> species)
    {
        var speciesPrototype = _prototype.Index(species);
        var appearancePrototype = _prototype.Index(speciesPrototype.DollPrototype);

        if (!appearancePrototype.TryGetComponent<InitialBodyComponent>(out var initialBody, _component))
            return new();

        return initialBody.Organs;
    }

    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData> GetMarkingData(ProtoId<SpeciesPrototype> species)
    {
        var ret = new Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData>();

        foreach (var (organ, proto) in GetOrgans(species))
        {
            if (!TryGetMarkingData(proto, out var organData))
                continue;

            ret[organ] = organData.Value;
        }

        return ret;
    }

    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> GetProfileData(
        ProtoId<SpeciesPrototype> species,
        Sex sex,
        Color skinColor,
        Color eyeColor)
    {
        var ret = new Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData>();

        foreach (var organ in GetOrgans(species).Keys)
        {
            ret[organ] = new()
            {
                Sex = sex,
                EyeColor = eyeColor,
                SkinColor = skinColor,
            };
        }

        return ret;
    }

    public bool TryGetMarkingData(EntProtoId organ, [NotNullWhen(true)] out OrganMarkingData? organData)
    {
        organData = null;

        if (!_prototype.TryIndex(organ, out var organProto))
            return false;

        if (!organProto.TryGetComponent<VisualOrganMarkingsComponent>(out var comp, _component))
            return false;

        organData = comp.MarkingData;
        return true;
    }

    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> ConvertMarkings(
        List<Marking> markings,
        ProtoId<SpeciesPrototype> species)
    {
        var ret = new Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>();

        var data = GetMarkingData(species);
        var layersToOrgans = data
            .SelectMany(kvp => kvp.Value.Layers.Select(layer => (layer, kvp.Key)))
            .ToDictionary(pair => pair.layer, pair => pair.Key);

        foreach (var marking in markings)
        {
            if (!_prototype.TryIndex<MarkingPrototype>(marking.MarkingId, out var markingProto))
                continue;

            if (!layersToOrgans.TryGetValue(markingProto.BodyPart, out var organ))
                continue;

            var organDict = ret.GetValueOrDefault(organ) ?? [];
            ret[organ] = organDict;
            var markingList = organDict.GetValueOrDefault(markingProto.BodyPart) ?? [];
            organDict[markingProto.BodyPart] = markingList;
            markingList.Add(marking);
        }

        return ret;
    }

    /// <summary>
    /// Recursively compares two markings dictionaries for equality.
    /// </summary>
    public static bool MarkingsAreEqual(
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> a,
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> b)
    {
        if (a.Count != b.Count)
            return false;

        foreach (var (organ, aDictionary) in a)
        {
            if (!b.TryGetValue(organ, out var bDictionary))
                return false;

            if (aDictionary.Count != bDictionary.Count)
                return false;

            foreach (var (layer, aMarkings) in aDictionary)
            {
                if (!bDictionary.TryGetValue(layer, out var bMarkings))
                    return false;

                if (!aMarkings.SequenceEqual(bMarkings))
                    return false;
            }
        }

        return true;
    }
}
