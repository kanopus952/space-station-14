using System.Linq;
using System.Numerics;
using Content.Shared._Sunrise.MarkingEffects;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid;

/// <summary>
/// Holds all of the data for importing / exporting character profiles.
/// </summary>
[DataDefinition]
public sealed partial class HumanoidProfileExportV1
{
    [DataField]
    public string ForkId;

    [DataField]
    public int Version = 1;

    [DataField(required: true)]
    public HumanoidCharacterProfileV1 Profile = default!;

    public HumanoidProfileExportV2 ToV2()
    {
        return new()
        {
            ForkId = ForkId,
            Version = 2,
            Profile = Profile.ToV2()
        };
    }
}

[DataDefinition, Serializable]
public sealed partial class HumanoidCharacterProfileV1
{
    [DataField("_jobPriorities")]
    public Dictionary<ProtoId<JobPrototype>, JobPriority> JobPriorities = new();

    [DataField("_antagPreferences")]
    public HashSet<ProtoId<AntagPrototype>> AntagPreferences = new();

    [DataField("_traitPreferences")]
    public HashSet<ProtoId<TraitPrototype>> TraitPreferences = new();

    [DataField("_loadouts")]
    public Dictionary<string, RoleLoadout> Loadouts = new();

    [DataField]
    public string Name;

    [DataField]
    public string FlavorText;

    [DataField]
    public ProtoId<SpeciesPrototype> Species;

    [DataField]
    public int Age;

    [DataField]
    public Sex Sex;

    [DataField]
    public Gender Gender;

    [DataField]
    public HumanoidCharacterAppearanceV1 Appearance;

    [DataField]
    public SpawnPriorityPreference SpawnPriority;

    [DataField]
    public PreferenceUnavailableMode PreferenceUnavailable;

    public HumanoidCharacterProfile ToV2()
    {
        return new(
            Name,
            FlavorText,
            Species,
            SharedHumanoidAppearanceSystem.DefaultVoice,
            SharedHumanoidAppearanceSystem.DefaultBodyType,
            Age,
            Sex,
            Gender,
            Appearance.ToV2(Species),
            SpawnPriority,
            JobPriorities,
            PreferenceUnavailable,
            AntagPreferences,
            TraitPreferences,
            Loadouts);
    }
}


[DataDefinition, Serializable]
public sealed partial class HumanoidCharacterAppearanceV1
{
    [DataField("hair")]
    public string HairStyleId;

    [DataField]
    public Color HairColor;

    [DataField("facialHair")]
    public string FacialHairStyleId;

    [DataField]
    public Color FacialHairColor;

    [DataField]
    public Color EyeColor;

    [DataField]
    public Color SkinColor;

    [DataField]
    public List<Marking> Markings = new();

    public HumanoidCharacterAppearance ToV2(ProtoId<SpeciesPrototype> species)
    {
        var markingManager = IoCManager.Resolve<MarkingManager>();
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var speciesPrototype = protoMan.Index<SpeciesPrototype>(species);

        var incomingMarkings = Markings.ShallowClone();
        AddLegacyMarking(incomingMarkings, HairStyleId, HairColor, markingManager);
        AddLegacyMarking(incomingMarkings, FacialHairStyleId, FacialHairColor, markingManager);

        return new HumanoidCharacterAppearance(
            HairStyleId,
            HairColor,
            FacialHairStyleId,
            FacialHairColor,
            EyeColor,
            SkinColor,
            markingManager.ConvertMarkings(incomingMarkings, species),
            MarkingEffectType.Color,
            null,
            MarkingEffectType.Color,
            null,
            speciesPrototype.DefaultWidth,
            speciesPrototype.DefaultHeight);
    }

    private static void AddLegacyMarking(List<Marking> markings, string markingId, Color color, MarkingManager markingManager)
    {
        if (markingId == string.Empty ||
            !markingManager.Markings.TryGetValue(markingId, out var markingProto))
        {
            return;
        }

        markings.Add(new Marking(markingId, Enumerable.Repeat(color, markingProto.Sprites.Count).ToList()));
    }
}
