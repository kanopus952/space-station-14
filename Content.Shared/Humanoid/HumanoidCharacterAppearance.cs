using System.Linq;
using System.Numerics;
using Content.Shared._Sunrise.MarkingEffects;
using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HumanoidCharacterAppearance : IEquatable<HumanoidCharacterAppearance>
{
    // Sunrise gradient edit start
    [DataField]
    public MarkingEffectType HairMarkingEffectType { get; set; } = MarkingEffectType.Color;

    [DataField]
    public MarkingEffect? HairMarkingEffect { get; set; }

    [DataField]
    public MarkingEffectType FacialHairMarkingEffectType { get; set; } = MarkingEffectType.Color;

    [DataField]
    public MarkingEffect? FacialHairMarkingEffect { get; set; }
    // Sunrise gradient edit end

    [DataField("hair")]
    public string HairStyleId { get; set; } = HairStyles.DefaultHairStyle;

    [DataField]
    public Color HairColor { get; set; } = Color.Black;

    [DataField("facialHair")]
    public string FacialHairStyleId { get; set; } = HairStyles.DefaultFacialHairStyle;

    [DataField]
    public Color FacialHairColor { get; set; } = Color.Black;

    [DataField]
    public Color EyeColor { get; set; } = Color.Black;

    [DataField]
    public Color SkinColor { get; set; } = Color.FromHsv(new Vector4(0.07f, 0.2f, 1f, 1f));

    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings { get; set; } = new();

    [DataField]
    public float Width { get; set; } = 1f; // Sunrise

    [DataField]
    public float Height { get; set; } = 1f; // Sunrise

    public HumanoidCharacterAppearance()
    {
    }

    public HumanoidCharacterAppearance(
        Color eyeColor,
        Color skinColor,
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings)
        : this(
            HairStyles.DefaultHairStyle,
            Color.Black,
            HairStyles.DefaultFacialHairStyle,
            Color.Black,
            eyeColor,
            skinColor,
            markings,
            MarkingEffectType.Color,
            null,
            MarkingEffectType.Color,
            null,
            1f,
            1f)
    {
    }

    public HumanoidCharacterAppearance(
        string hairStyleId,
        Color hairColor,
        string facialHairStyleId,
        Color facialHairColor,
        Color eyeColor,
        Color skinColor,
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings,
        MarkingEffectType hairMarkingEffectType,
        MarkingEffect? hairMarkingEffect,
        MarkingEffectType facialHairMarkingEffectType,
        MarkingEffect? facialHairMarkingEffect,
        float width,
        float height)
    {
        HairStyleId = hairStyleId;
        HairColor = ClampColor(hairColor);
        FacialHairStyleId = facialHairStyleId;
        FacialHairColor = ClampColor(facialHairColor);
        EyeColor = ClampColor(eyeColor);
        SkinColor = ClampColor(skinColor);
        Markings = CloneMarkings(markings);
        HairMarkingEffectType = hairMarkingEffectType;
        HairMarkingEffect = CloneAndClampEffect(hairMarkingEffect);
        FacialHairMarkingEffectType = facialHairMarkingEffectType;
        FacialHairMarkingEffect = CloneAndClampEffect(facialHairMarkingEffect);
        Width = width;
        Height = height;
    }

    public HumanoidCharacterAppearance(HumanoidCharacterAppearance other)
        : this(
            other.HairStyleId,
            other.HairColor,
            other.FacialHairStyleId,
            other.FacialHairColor,
            other.EyeColor,
            other.SkinColor,
            other.Markings,
            other.HairMarkingEffectType,
            other.HairMarkingEffect,
            other.FacialHairMarkingEffectType,
            other.FacialHairMarkingEffect,
            other.Width,
            other.Height)
    {
    }

    public HumanoidCharacterAppearance WithHairStyleName(string newName)
    {
        return With(hairStyleId: newName);
    }

    public HumanoidCharacterAppearance WithHairColor(Color newColor, MarkingEffect? newExtendedColor = null)
    {
        return With(
            hairColor: newColor,
            hairMarkingEffectType: newExtendedColor?.Type,
            hairMarkingEffect: newExtendedColor);
    }

    public HumanoidCharacterAppearance WithFacialHairStyleName(string newName)
    {
        return With(facialHairStyleId: newName);
    }

    public HumanoidCharacterAppearance WithFacialHairColor(Color newColor, MarkingEffect? newFacialExtendedColor = null)
    {
        return With(
            facialHairColor: newColor,
            facialHairMarkingEffectType: newFacialExtendedColor?.Type,
            facialHairMarkingEffect: newFacialExtendedColor);
    }

    public HumanoidCharacterAppearance WithEyeColor(Color newColor)
    {
        return With(eyeColor: newColor);
    }

    public HumanoidCharacterAppearance WithSkinColor(Color newColor)
    {
        return With(skinColor: newColor);
    }

    public HumanoidCharacterAppearance WithMarkings(
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> newMarkings)
    {
        return With(markings: newMarkings);
    }

    // Sunrise gradient edit start
    public HumanoidCharacterAppearance WithHairExtendedColor(MarkingEffect? newExtendedColor)
    {
        return With(
            hairMarkingEffectType: newExtendedColor?.Type ?? MarkingEffectType.Color,
            hairMarkingEffect: newExtendedColor);
    }

    public HumanoidCharacterAppearance WithFacialHairExtendedColor(MarkingEffect? newFacialExtendedColor)
    {
        return With(
            facialHairMarkingEffectType: newFacialExtendedColor?.Type ?? MarkingEffectType.Color,
            facialHairMarkingEffect: newFacialExtendedColor);
    }
    // Sunrise gradient edit end

    public HumanoidCharacterAppearance WithWidth(float newWidth)
    {
        return With(width: newWidth);
    }

    public HumanoidCharacterAppearance WithHeight(float newHeight)
    {
        return With(height: newHeight);
    }

    public List<Marking> GetFlatMarkings()
    {
        var result = new List<Marking>();

        foreach (var organMarkings in Markings.Values)
        {
            foreach (var layerMarkings in organMarkings.Values)
            {
                foreach (var marking in layerMarkings)
                {
                    result.Add(new(marking));
                }
            }
        }

        return result;
    }

    public static HumanoidCharacterAppearance DefaultWithSpecies(string species, Sex sex = Sex.Male)
    {
        return DefaultWithSpecies((ProtoId<SpeciesPrototype>) species, sex);
    }

    public static HumanoidCharacterAppearance DefaultWithSpecies(ProtoId<SpeciesPrototype> species, Sex sex)
    {
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var speciesPrototype = protoMan.Index<SpeciesPrototype>(species);
        var skinColoration = protoMan.Index(speciesPrototype.SkinColoration).Strategy;
        var skinColor = skinColoration.InputType switch
        {
            SkinColorationStrategyInput.Unary => skinColoration.FromUnary(speciesPrototype.DefaultHumanSkinTone),
            SkinColorationStrategyInput.Color => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
            _ => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
        };

        var appearance = new HumanoidCharacterAppearance(
            HairStyles.DefaultHairStyle,
            Color.Black,
            HairStyles.DefaultFacialHairStyle,
            Color.Black,
            Color.Black,
            skinColor,
            new(),
            MarkingEffectType.Color,
            null,
            MarkingEffectType.Color,
            null,
            speciesPrototype.DefaultWidth,
            speciesPrototype.DefaultHeight);

        return EnsureValid(appearance, species, sex);
    }

    private static readonly IReadOnlyList<Color> RealisticEyeColors = new List<Color>
    {
        Color.Brown,
        Color.Gray,
        Color.Azure,
        Color.SteelBlue,
        Color.Black,
    };

    public static HumanoidCharacterAppearance Random(string species, Sex sex)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var markingManager = IoCManager.Resolve<MarkingManager>();

        var newFacialHairStyle = HairStyles.DefaultFacialHairStyle;
        var newHairStyle = HairStyles.DefaultHairStyle;

        // Sunrise - Start
        var hairStyles = markingManager.MarkingsByCategoryAndSpeciesAndSex(MarkingCategories.Hair, species, sex);
        if (hairStyles.Count > 0)
            newHairStyle = random.Pick(hairStyles.Keys.ToArray());

        if (sex != Sex.Female)
        {
            var facialHairStyles = markingManager.MarkingsByCategoryAndSpeciesAndSex(MarkingCategories.FacialHair, species, sex);
            if (facialHairStyles.Count > 0)
                newFacialHairStyle = random.Pick(facialHairStyles.Keys.ToArray());
        }
        // Sunrise - End

        var newHairColor = random.Pick(HairStyles.RealisticHairColors);
        newHairColor = newHairColor
            .WithRed(RandomizeColor(newHairColor.R))
            .WithGreen(RandomizeColor(newHairColor.G))
            .WithBlue(RandomizeColor(newHairColor.B));

        var newEyeColor = random.Pick(RealisticEyeColors);

        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var speciesPrototype = protoMan.Index<SpeciesPrototype>(species);
        var skinType = speciesPrototype.SkinColoration;
        var strategy = protoMan.Index(skinType).Strategy;

        var newSkinColor = strategy.InputType switch
        {
            SkinColorationStrategyInput.Unary => strategy.FromUnary(random.NextFloat(0f, 100f)),
            SkinColorationStrategyInput.Color => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
            _ => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
        };

        var newWidth = random.NextFloat(speciesPrototype.MinWidth, speciesPrototype.MaxWidth);
        var newHeight = random.NextFloat(speciesPrototype.MinHeight, speciesPrototype.MaxHeight);

        var appearance = new HumanoidCharacterAppearance(
                newHairStyle,
                newHairColor,
                newFacialHairStyle,
                newHairColor,
                newEyeColor,
                newSkinColor,
                new(),
                MarkingEffectType.Color,
                null,
                MarkingEffectType.Color,
                null,
                newWidth,
                newHeight);

        // Safety step. Most systems which called Random() also called this, and not doing so caused issues with markings.
        return EnsureValid(appearance, species, sex);

        float RandomizeColor(float channel)
        {
            return MathHelper.Clamp01(channel + random.Next(-25, 25) / 100f);
        }
    }

    public static Color ClampColor(Color color)
    {
        return new(color.RByte, color.GByte, color.BByte);
    }

    public static HumanoidCharacterAppearance EnsureValid(HumanoidCharacterAppearance appearance, string species, Sex sex, string[]? sponsorPrototypes = null)
    {
        return EnsureValid(appearance, (ProtoId<SpeciesPrototype>) species, sex, sponsorPrototypes);
    }

    public static HumanoidCharacterAppearance EnsureValid(
        HumanoidCharacterAppearance appearance,
        ProtoId<SpeciesPrototype> species,
        Sex sex,
        string[]? sponsorPrototypes = null)
    {
        sponsorPrototypes ??= [];

        var hairStyleId = appearance.HairStyleId;
        var facialHairStyleId = appearance.FacialHairStyleId;

        var hairColor = ClampColor(appearance.HairColor);
        var facialHairColor = ClampColor(appearance.FacialHairColor);
        var eyeColor = ClampColor(appearance.EyeColor);

        var width = appearance.Width;
        var height = appearance.Height;

        var proto = IoCManager.Resolve<IPrototypeManager>();
        var markingManager = IoCManager.Resolve<MarkingManager>();

        if (!markingManager.MarkingsByCategory(MarkingCategories.Hair).ContainsKey(hairStyleId))
            hairStyleId = HairStyles.DefaultHairStyle;

        // Sunrise-Sponsors-Start
        if (proto.TryIndex(hairStyleId, out MarkingPrototype? hairProto) &&
            hairProto.SponsorOnly &&
            !sponsorPrototypes.Contains(hairStyleId))
        {
            hairStyleId = HairStyles.DefaultHairStyle;
        }
        // Sunrise-Sponsors-End

        if (!markingManager.MarkingsByCategory(MarkingCategories.FacialHair).ContainsKey(facialHairStyleId))
            facialHairStyleId = HairStyles.DefaultFacialHairStyle;

        // Sunrise-Sponsors-Start
        if (proto.TryIndex(facialHairStyleId, out MarkingPrototype? facialHairProto) &&
            facialHairProto.SponsorOnly &&
            !sponsorPrototypes.Contains(facialHairStyleId))
        {
            facialHairStyleId = HairStyles.DefaultFacialHairStyle;
        }
        // Sunrise-Sponsors-End

        var skinColor = appearance.SkinColor;
        var validatedMarkings = CloneMarkings(appearance.Markings);

        if (proto.TryIndex(species, out var speciesProto))
        {
            var strategy = proto.Index(speciesProto.SkinColoration).Strategy;
            var organs = markingManager.GetOrgans(species);
            skinColor = strategy.EnsureVerified(skinColor);
            width = Math.Clamp(width, speciesProto.MinWidth, speciesProto.MaxWidth);
            height = Math.Clamp(height, speciesProto.MinHeight, speciesProto.MaxHeight);

            if (!markingManager.CanBeApplied(species, sex, new Marking(hairStyleId, 1), proto))
                hairStyleId = HairStyles.DefaultHairStyle;

            if (!markingManager.CanBeApplied(species, sex, new Marking(facialHairStyleId, 1), proto))
                facialHairStyleId = HairStyles.DefaultFacialHairStyle;

            MergeLegacyHairMarkings(
                validatedMarkings,
                markingManager,
                species,
                hairStyleId,
                hairColor,
                appearance.HairMarkingEffect,
                facialHairStyleId,
                facialHairColor,
                appearance.FacialHairMarkingEffect);

            foreach (var organ in appearance.Markings.Keys.ToArray())
            {
                if (!organs.ContainsKey(organ))
                    validatedMarkings.Remove(organ);
            }

            foreach (var (organ, organProtoId) in organs)
            {
                if (!markingManager.TryGetMarkingData(organProtoId, out var organData))
                {
                    validatedMarkings.Remove(organ);
                    continue;
                }

                var actualMarkings = validatedMarkings.GetValueOrDefault(organ) is { } current
                    ? CloneLayerMarkings(current)
                    : [];

                markingManager.EnsureValidColors(actualMarkings);
                markingManager.EnsureValidGroupAndSex(actualMarkings, organData.Value.Group, sex);
                markingManager.EnsureValidLayers(actualMarkings, organData.Value.Layers);
                markingManager.EnsureValidLimits(actualMarkings, organData.Value.Group, organData.Value.Layers, skinColor, eyeColor);
                FilterSponsorMarkings(actualMarkings, sponsorPrototypes, proto);

                validatedMarkings[organ] = actualMarkings;
            }
        }

        return new HumanoidCharacterAppearance(
            hairStyleId,
            hairColor,
            facialHairStyleId,
            facialHairColor,
            eyeColor,
            skinColor,
            validatedMarkings,
            appearance.HairMarkingEffectType,
            appearance.HairMarkingEffect,
            appearance.FacialHairMarkingEffectType,
            appearance.FacialHairMarkingEffect,
            width,
            height);
    }

    public bool Equals(HumanoidCharacterAppearance? other)
    {
        if (ReferenceEquals(null, other))
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return HairStyleId == other.HairStyleId &&
               HairColor.Equals(other.HairColor) &&
               FacialHairStyleId == other.FacialHairStyleId &&
               FacialHairColor.Equals(other.FacialHairColor) &&
               EyeColor.Equals(other.EyeColor) &&
               SkinColor.Equals(other.SkinColor) &&
               MarkingManager.MarkingsAreEqual(Markings, other.Markings) &&
               HairMarkingEffectType.Equals(other.HairMarkingEffectType) &&
               Equals(HairMarkingEffect, other.HairMarkingEffect) &&
               FacialHairMarkingEffectType.Equals(other.FacialHairMarkingEffectType) &&
               Equals(FacialHairMarkingEffect, other.FacialHairMarkingEffect) &&
               Width == other.Width &&
               Height == other.Height;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is HumanoidCharacterAppearance other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(HairStyleId);
        hashCode.Add(HairColor);
        hashCode.Add(FacialHairStyleId);
        hashCode.Add(FacialHairColor);
        hashCode.Add(EyeColor);
        hashCode.Add(SkinColor);
        hashCode.Add(Markings);
        hashCode.Add(HairMarkingEffectType);
        hashCode.Add(HairMarkingEffect);
        hashCode.Add(FacialHairMarkingEffectType);
        hashCode.Add(FacialHairMarkingEffect);
        hashCode.Add(Width);
        hashCode.Add(Height);
        return hashCode.ToHashCode();
    }

    public HumanoidCharacterAppearance Clone()
    {
        return new(this);
    }

    private HumanoidCharacterAppearance With(
        string? hairStyleId = null,
        Color? hairColor = null,
        string? facialHairStyleId = null,
        Color? facialHairColor = null,
        Color? eyeColor = null,
        Color? skinColor = null,
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>? markings = null,
        MarkingEffectType? hairMarkingEffectType = null,
        MarkingEffect? hairMarkingEffect = null,
        MarkingEffectType? facialHairMarkingEffectType = null,
        MarkingEffect? facialHairMarkingEffect = null,
        float? width = null,
        float? height = null)
    {
        return new(
            hairStyleId ?? HairStyleId,
            hairColor ?? HairColor,
            facialHairStyleId ?? FacialHairStyleId,
            facialHairColor ?? FacialHairColor,
            eyeColor ?? EyeColor,
            skinColor ?? SkinColor,
            markings ?? Markings,
            hairMarkingEffectType ?? HairMarkingEffectType,
            hairMarkingEffectType.HasValue ? hairMarkingEffect : HairMarkingEffect,
            facialHairMarkingEffectType ?? FacialHairMarkingEffectType,
            facialHairMarkingEffectType.HasValue ? facialHairMarkingEffect : FacialHairMarkingEffect,
            width ?? Width,
            height ?? Height);
    }

    private static Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> CloneMarkings(
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings)
    {
        var clone = new Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>();

        foreach (var (organ, layerMarkings) in markings)
        {
            clone[organ] = CloneLayerMarkings(layerMarkings);
        }

        return clone;
    }

    private static Dictionary<HumanoidVisualLayers, List<Marking>> CloneLayerMarkings(
        Dictionary<HumanoidVisualLayers, List<Marking>> layerMarkings)
    {
        var clone = new Dictionary<HumanoidVisualLayers, List<Marking>>();

        foreach (var (layer, markings) in layerMarkings)
        {
            clone[layer] = markings.Select(marking => new Marking(marking)).ToList();
        }

        return clone;
    }

    private static MarkingEffect? CloneAndClampEffect(MarkingEffect? effect)
    {
        if (effect == null)
            return null;

        var clone = effect.Clone();
        foreach (var (key, color) in clone.Colors)
        {
            clone.Colors[key] = ClampColor(color);
        }

        return clone;
    }

    private static void FilterSponsorMarkings(
        Dictionary<HumanoidVisualLayers, List<Marking>> markings,
        string[] sponsorPrototypes,
        IPrototypeManager prototype)
    {
        foreach (var layerMarkings in markings.Values)
        {
            for (var i = layerMarkings.Count - 1; i >= 0; i--)
            {
                var marking = layerMarkings[i];
                if (!prototype.TryIndex<MarkingPrototype>(marking.MarkingId, out var markingProto))
                    continue;

                if (markingProto.SponsorOnly && !sponsorPrototypes.Contains(marking.MarkingId))
                    layerMarkings.RemoveAt(i);
            }
        }
    }

    private static void MergeLegacyHairMarkings(
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> target,
        MarkingManager markingManager,
        ProtoId<SpeciesPrototype> species,
        string hairStyleId,
        Color hairColor,
        MarkingEffect? hairEffect,
        string facialHairStyleId,
        Color facialHairColor,
        MarkingEffect? facialHairEffect)
    {
        var legacyMarkings = new List<Marking>();

        AddLegacyMarking(legacyMarkings, hairStyleId, HairStyles.DefaultHairStyle, hairColor, hairEffect, markingManager);
        AddLegacyMarking(legacyMarkings, facialHairStyleId, HairStyles.DefaultFacialHairStyle, facialHairColor, facialHairEffect, markingManager);

        if (legacyMarkings.Count == 0)
            return;

        var converted = markingManager.ConvertMarkings(legacyMarkings, species);
        foreach (var (organ, layerMarkings) in converted)
        {
            var organMarkings = target.GetValueOrDefault(organ) ?? [];
            target[organ] = organMarkings;

            foreach (var (layer, markings) in layerMarkings)
            {
                var targetMarkings = organMarkings.GetValueOrDefault(layer) ?? [];
                organMarkings[layer] = targetMarkings;

                foreach (var marking in markings)
                {
                    if (targetMarkings.Any(existing => existing.MarkingId == marking.MarkingId))
                        continue;

                    targetMarkings.Add(marking);
                }
            }
        }
    }

    private static void AddLegacyMarking(
        List<Marking> target,
        string markingId,
        string defaultMarkingId,
        Color color,
        MarkingEffect? effect,
        MarkingManager markingManager)
    {
        if (markingId == defaultMarkingId ||
            !markingManager.Markings.TryGetValue(markingId, out var markingProto))
        {
            return;
        }

        var colors = Enumerable.Repeat(color, markingProto.Sprites.Count).ToList();
        var effects = effect == null
            ? null
            : Enumerable.Range(0, markingProto.Sprites.Count)
                .Select(_ => effect.Clone())
                .ToList();

        target.Add(new Marking(markingId, colors, effects));
    }
}
