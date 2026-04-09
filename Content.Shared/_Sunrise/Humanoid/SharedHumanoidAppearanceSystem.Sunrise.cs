// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using System.Linq;
using Content.Shared._Sunrise;
using Content.Shared._Sunrise.Humanoid;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Humanoid;

public abstract partial class SharedHumanoidAppearanceSystem
{
    // Sunrise-TTS-Start
    public const string DefaultVoice = "Voljin";

    public static readonly Dictionary<Sex, string> DefaultSexVoice = new()
    {
        { Sex.Male, "Voljin" },
        { Sex.Female, "Amina" },
        { Sex.Unsexed, "Charlotte" },
    };
    public static ProtoId<BodyTypePrototype> DefaultBodyType = "HumanNormal";

    private ISharedSponsorsManager? _sponsors;

    public void InitializeSunrise()
    {
        IoCManager.Instance!.TryResolveType(out _sponsors); // Sunrise-Sponsors
        SubscribeLocalEvent<HumanoidAppearanceComponent, ComponentAdd>(OnHumanoidComponentAdd);
    }

    /// <summary>
    ///     Auto-attaches companion Sunrise components whenever a humanoid entity is created.
    /// </summary>
    private void OnHumanoidComponentAdd(EntityUid uid, HumanoidAppearanceComponent _, ComponentAdd args)
    {
        EnsureComp<HumanoidVoiceComponent>(uid);
        EnsureComp<HumanoidScaleComponent>(uid);
        EnsureComp<HumanoidBodyTypeComponent>(uid);
    }

    public void CloneAppearanceSunrise(
        EntityUid source,
        EntityUid target,
        HumanoidAppearanceComponent targetHumanoid,
        HumanoidAppearanceComponent sourceHumanoid)
    {
        if (TryComp<HumanoidVoiceComponent>(source, out var sourceVoice))
            SetTTSVoice(target, sourceVoice.Voice); // Sunrise-TTS

        if (TryComp<HumanoidBodyTypeComponent>(source, out var srcBodyType) &&
            TryComp<HumanoidBodyTypeComponent>(target, out var dstBodyType))
        {
            dstBodyType.BodyType = srcBodyType.BodyType;
            Dirty(target, dstBodyType);
        }

        if (TryComp<HumanoidScaleComponent>(source, out var srcScale) &&
            TryComp<HumanoidScaleComponent>(target, out var dstScale))
        {
            dstScale.Width = srcScale.Width;
            dstScale.Height = srcScale.Height;
            Dirty(target, dstScale);
        }
    }

    public void LoadProfileSunrise(EntityUid uid, HumanoidCharacterProfile profile, HumanoidAppearanceComponent humanoid)
    {
        SetTTSVoice(uid, profile.Voice); // Sunrise-TTS
        SetBodyType(uid, profile.BodyType, false);

        if (TryComp<HumanoidScaleComponent>(uid, out var scale))
        {
            scale.Width = profile.Appearance.Width;
            scale.Height = profile.Appearance.Height;
            Dirty(uid, scale);
        }
    }

    public void GetSponsorPrototypes(ref string[] sponsorPrototypes)
    {
        sponsorPrototypes = _sponsors?.GetClientPrototypes().ToArray() ?? [];
    }

    /// <summary>
    ///     Sets the TTS voice on both <see cref="HumanoidVoiceComponent"/> and <see cref="TTSComponent"/>.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public void SetTTSVoice(EntityUid uid, ProtoId<TTSVoicePrototype> voiceId)
    {
        if (TryComp<HumanoidVoiceComponent>(uid, out var voice))
        {
            voice.Voice = voiceId;
            Dirty(uid, voice);
        }

        if (TryComp<TTSComponent>(uid, out var tts))
        {
            tts.VoicePrototypeId = voiceId;
            Dirty(uid, tts);
        }
    }

    /// <summary>
    ///     Sets the body type on <see cref="HumanoidBodyTypeComponent"/>, falling back to the first
    ///     available body type if the requested one is not valid for the current species.
    /// </summary>
    public void SetBodyType(
        EntityUid uid,
        ProtoId<BodyTypePrototype> bodyType,
        bool sync = true,
        HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
            return;

        if (!TryComp<HumanoidBodyTypeComponent>(uid, out var bodyTypeComp))
            return;

        var speciesPrototype = _proto.Index<SpeciesPrototype>(humanoid.Species);
        bodyTypeComp.BodyType = speciesPrototype.BodyTypes.Contains(bodyType)
            ? bodyType
            : speciesPrototype.BodyTypes.First();

        if (sync)
            Dirty(uid, bodyTypeComp);
    }
}
