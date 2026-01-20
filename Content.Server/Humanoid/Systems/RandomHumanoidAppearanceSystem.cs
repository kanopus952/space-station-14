using Content.Server.Humanoid;
using Content.Server.Humanoid.Components;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;

namespace Content.Server.Humanoid.Systems;

public sealed class RandomHumanoidAppearanceSystem : EntitySystem
{
    [Dependency] private readonly HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomHumanoidAppearanceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, RandomHumanoidAppearanceComponent component, MapInitEvent args)
    {
        // If we have an initial profile/base layer set, do not randomize this humanoid.
        if (!TryComp<HumanoidProfileComponent>(uid, out var humanoid))
            return;

        var profile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);
        var appearance = profile.Appearance;

        // Sunrise-Start
        if (component.Hair != null)
            appearance = appearance.WithHairStyleName(component.Hair);

        if (component.SkinColor != null)
            appearance = appearance.WithSkinColor(component.SkinColor.Value);

        if (component.HairColor != null)
            appearance = appearance.WithHairColor(component.HairColor.Value);

        if (component.FacialHairColor != null)
            appearance = appearance.WithFacialHairColor(component.FacialHairColor.Value);
        // Sunrise-End

        appearance = HumanoidCharacterAppearance.EnsureValid(appearance, profile.Species, profile.Sex);
        profile = profile.WithCharacterAppearance(appearance);

        _visualBody.ApplyProfileTo(uid, profile);
        _humanoidProfile.ApplyProfileTo(uid, profile);
        _humanoid.LoadProfile(uid, profile);

        if (component.RandomizeName)
            _metaData.SetEntityName(uid, profile.Name);
    }
}
