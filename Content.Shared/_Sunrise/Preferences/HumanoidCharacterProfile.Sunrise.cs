// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using System.Linq;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{

    /// <summary>
    ///     TTS voice selected for this character profile.
    /// </summary>
    [DataField]
    public ProtoId<TTSVoicePrototype> Voice { get; set; } = SharedHumanoidAppearanceSystem.DefaultVoice;


    /// <summary>
    ///     Body type selected for this character profile.
    /// </summary>
    [DataField]
    public string BodyType { get; set; } = SharedHumanoidAppearanceSystem.DefaultBodyType;


    public HumanoidCharacterProfile WithVoice(string voice)
    {
        return new(this) { Voice = voice };
    }

    public HumanoidCharacterProfile WithBodyType(string bodyType)
    {
        return new HumanoidCharacterProfile(this) { BodyType = bodyType };
    }


    /// <summary>
    ///     Returns true if the given TTS voice is valid for the specified sex.
    /// </summary>
    public static bool CanHaveVoice(TTSVoicePrototype voice, Sex sex)
    {
        return voice.RoundStart && sex == Sex.Unsexed || voice.Sex == sex || voice.Sex == Sex.Unsexed;
    }


    #region EnsureValid Sunrise hooks

    /// <summary>
    ///     Resets species to the default if the current species is sponsor-only and the player is not a sponsor.
    /// </summary>
    public void EnsureValidSunriseSpecies(
        string[] sponsorPrototypes,
        IPrototypeManager prototypeManager,
        ref SpeciesPrototype speciesPrototype)
    {
        if (speciesPrototype.SponsorOnly && !sponsorPrototypes.Contains(Species.Id))
        {
            Species = SharedHumanoidAppearanceSystem.DefaultSpecies;
            speciesPrototype = prototypeManager.Index(Species);
        }
    }

    /// <summary>
    ///     Overrides the vanilla max flavor-text length with the sponsor-aware Sunrise CVar,
    ///     and clears flavor text if the player is not allowed to have it.
    /// </summary>
    public void EnsureValidSunriseFlavor(
        ICommonSession session,
        IConfigurationManager configManager,
        ref int maxDescLength)
    {
        IoCManager.Instance!.TryResolveType<ISharedSponsorsManager>(out var sponsors);
        maxDescLength = configManager.GetCVar(SunriseCCVars.FlavorTextBaseLength);
        if (sponsors != null)
        {
            if (sponsors.IsSponsor(session.UserId))
                maxDescLength = sponsors.GetSizeFlavor(session.UserId);
            if (!sponsors.IsAllowedFlavor(session.UserId) && configManager.GetCVar(SunriseCCVars.FlavorTextSponsorOnly))
                FlavorText = string.Empty;
        }
    }

    /// <summary>
    ///     Validates the stored BodyType against the species and falls back to the first valid type.
    /// </summary>
    public void EnsureValidSunriseBodyType(SpeciesPrototype speciesPrototype)
    {
        BodyType = speciesPrototype.BodyTypes.Contains(BodyType)
            ? BodyType
            : speciesPrototype.BodyTypes.First();
    }

    /// <summary>
    ///     Validates the stored TTS voice against the character's sex,
    ///     falling back to the sex-appropriate default voice.
    /// </summary>
    public void EnsureValidSunriseTTS(IPrototypeManager prototypeManager, Sex sex)
    {
        prototypeManager.TryIndex(Voice, out var voice);
        if (voice is null || !CanHaveVoice(voice, sex))
            Voice = SharedHumanoidAppearanceSystem.DefaultSexVoice[sex];
    }

    #endregion

    /// <summary>
    ///     Picks a random TTS voice valid for the given sex from round-start, non-sponsor voices.
    /// </summary>
    public static void RandomWithSpeciesSunriseTTS(
        IPrototypeManager prototypeManager,
        IRobustRandom random,
        Sex sex,
        ref string voiceId)
    {
        voiceId = random.Pick(prototypeManager
            .EnumeratePrototypes<TTSVoicePrototype>()
            .Where(o => CanHaveVoice(o, sex) && !o.SponsorOnly)
            .ToArray()
        ).ID;
    }
}
