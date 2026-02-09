using System.Linq;
using Content.Client.UserInterface.Systems.Guidebook;
using Content.Shared._Sunrise.MarkingEffects;
using Content.Shared.Guidebook;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Enums;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    public event Action<List<ProtoId<GuideEntryPrototype>>>? OnOpenGuidebook;

    private ColorSelectorSliders _rgbSkinColorSelector;
    private List<SpeciesPrototype> _species = new();
    private static readonly ProtoId<GuideEntryPrototype> DefaultSpeciesGuidebook = "Species";

    public void UpdateSpeciesGuidebookIcon()
    {
        SpeciesInfoButton.StyleClasses.Clear();

        var species = Profile?.Species;
        if (species is null)
            return;

        if (!_prototypeManager.Resolve<SpeciesPrototype>(species, out var speciesProto))
            return;

        // Don't display the info button if no guide entry is found
        if (!_prototypeManager.HasIndex<GuideEntryPrototype>(species))
            return;

        const string style = "SpeciesInfoDefault";
        SpeciesInfoButton.StyleIdentifier = style;
    }

    private void UpdateGenderControls()
    {
        if (Profile == null)
        {
            return;
        }

        PronounsButton.SelectId((int)Profile.Gender);
    }

    private void UpdateAgeEdit()
    {
        AgeEdit.Text = Profile?.Age.ToString() ?? "";
    }

    private void UpdateSexControls()
    {
        if (Profile == null)
            return;

        SexButton.Clear();

        var sexes = new List<Sex>();

        // add species sex options, default to just none if we are in bizzaro world and have no species
        if (_prototypeManager.Resolve(Profile.Species, out var speciesProto))
        {
            foreach (var sex in speciesProto.Sexes)
            {
                sexes.Add(sex);
            }
        }
        else
        {
            sexes.Add(Sex.Unsexed);
        }

        // add button for each sex
        foreach (var sex in sexes)
        {
            SexButton.AddItem(Loc.GetString($"humanoid-profile-editor-sex-{sex.ToString().ToLower()}-text"), (int)sex);
        }

        if (sexes.Contains(Profile.Sex))
            SexButton.SelectId((int)Profile.Sex);
        else
            SexButton.SelectId((int)sexes[0]);
    }

    private void UpdateEyePickers()
    {
        if (Profile == null)
        {
            return;
        }

        _markingsModel.SetOrganEyeColor(Profile.Appearance.EyeColor);
        EyeColorPicker.SetData(Profile.Appearance.EyeColor);
    }

    private void UpdateSkinColor()
    {
        if (Profile == null)
            return;

        var skin = _prototypeManager.Index<SpeciesPrototype>(Profile.Species).SkinColoration;
        var strategy = _prototypeManager.Index(skin).Strategy;

        switch (strategy.InputType)
        {
            case SkinColorationStrategyInput.Unary:
                {
                    if (!Skin.Visible)
                    {
                        Skin.Visible = true;
                        RgbSkinColorContainer.Visible = false;
                    }

                    Skin.Value = strategy.ToUnary(Profile.Appearance.SkinColor);

                    break;
                }
            case SkinColorationStrategyInput.Color:
                {
                    if (!RgbSkinColorContainer.Visible)
                    {
                        Skin.Visible = false;
                        RgbSkinColorContainer.Visible = true;
                    }

                    _rgbSkinColorSelector.Color = strategy.ClosestSkinColor(Profile.Appearance.SkinColor);

                    break;
                }
        }
    }

    private void UpdateSpawnPriorityControls()
    {
        if (Profile == null)
        {
            return;
        }

        SpawnPriorityButton.SelectId((int)Profile.SpawnPriority);
    }

    /// <summary>
    /// Refreshes the species selector.
    /// </summary>
    public void RefreshSpecies()
    {
        SpeciesButton.Clear();
        _species.Clear();

        _species.AddRange(_prototypeManager.EnumeratePrototypes<SpeciesPrototype>().Where(o => o.RoundStart));
        _species.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        var speciesIds = _species.Select(o => o.ID).ToList();

        for (var i = 0; i < _species.Count; i++)
        {
            var name = Loc.GetString(_species[i].Name);
            SpeciesButton.AddItem(name, i);

            if (Profile?.Species.Equals(_species[i].ID) == true)
            {
                SpeciesButton.SelectId(i);
            }

            if (_sponsorsMgr is null)
                continue;

            if (_species[i].SponsorOnly &&
                !_sponsorsMgr.GetClientPrototypes().Contains(_species[i].ID))
            {
                SpeciesButton.SetItemDisabled(SpeciesButton.GetIdx(i), true);
                SpeciesButton.SetItemText(SpeciesButton.GetIdx(i), Loc.GetString("sponsor-marking", ("name", name)));
            }
        }

        // If our species isn't available then reset it to default.
        if (Profile != null)
        {
            if (!speciesIds.Contains(Profile.Species))
            {
                SetSpecies(HumanoidCharacterProfile.DefaultSpecies);
            }
        }
    }

    private void SetSpecies(string newSpecies)
    {
        Profile = Profile?.WithSpecies(newSpecies);
        OnSkinColorOnValueChanged(); // Species may have special color prefs, make sure to update it.
        _markingsModel.OrganData = _markingManager.GetMarkingData(newSpecies);
        _markingsModel.ValidateMarkings();
        // In case there's job restrictions for the species
        RefreshJobs();
        // In case there's species restrictions for loadouts
        RefreshLoadouts();
        UpdateSexControls(); // update sex for new species
        UpdateBodyTypes();
        UpdateSpeciesGuidebookIcon();
        ResetSize();
        ReloadPreview();
    }

    private void SetAge(int newAge)
    {
        Profile = Profile?.WithAge(newAge);
        ReloadPreview();
    }

    private void SetSex(Sex newSex)
    {
        Profile = Profile?.WithSex(newSex);
        // for convenience, default to most common gender when new sex is selected
        switch (newSex)
        {
            case Sex.Male:
                Profile = Profile?.WithGender(Gender.Male);
                break;
            case Sex.Female:
                Profile = Profile?.WithGender(Gender.Female);
                break;
            default:
                Profile = Profile?.WithGender(Gender.Epicene);
                break;
        }

        UpdateGenderControls();
        UpdateTtsVoicesControls();
        UpdateBodyTypes();
        RefreshLoadouts();
        _markingsModel.SetOrganSexes(newSex);
        ReloadPreview();
    }

    private void SetGender(Gender newGender)
    {
        Profile = Profile?.WithGender(newGender);
        ReloadPreview();
    }

    private void SetSpawnPriority(SpawnPriorityPreference newSpawnPriority)
    {
        Profile = Profile?.WithSpawnPriorityPreference(newSpawnPriority);
        SetDirty();
    }

    private void SetVoice(string newVoice)
    {
        Profile = Profile?.WithVoice(newVoice);
        IsDirty = true;
    }

    private void UpdateSizeText()
    {
        if (Profile is null || !_prototypeManager.TryIndex<SpeciesPrototype>(Profile.Species, out var speciesPrototype))
            return;

        var heightRaw = HeightSlider.Value;
        var weightRaw = WidthSlider.Value;

        var height = ConvertSliderToHeight(
            heightRaw,
            speciesPrototype.MinHeight,
            speciesPrototype.MaxHeight,
            speciesPrototype.MinHeightCm,
            speciesPrototype.MaxHeightCm);

        var weight = speciesPrototype.StandardWeight + speciesPrototype.StandardDensity * (weightRaw * heightRaw - 1);
        HeightDescribeLabel.Text = Loc.GetString("humanoid-profile-editor-height-label", ("height", Math.Round(height)));
        WidthDescribeLabel.Text = Loc.GetString("humanoid-profile-editor-width-label", ("weight", Math.Round(weight)));
    }

    private float ConvertSliderToHeight(
        float sliderValue,
        float minSlider,
        float maxSlider,
        float minHeightCm,
        float maxHeightCm)
    {
        var denominator = maxSlider - minSlider;
        if (MathF.Abs(denominator) < 0.0001f)
            return minHeightCm;

        var normalized = (sliderValue - minSlider) / denominator;
        normalized = MathF.Min(1f, MathF.Max(0f, normalized));
        return minHeightCm + normalized * (maxHeightCm - minHeightCm);
    }

    private void SetWidth(float newWidth)
    {
        if (Profile is null)
            return;

        Profile.Appearance = Profile.Appearance.WithWidth(newWidth);

        UpdateSizeText();
        ReloadPreview();
    }

    private void SetHeight(float newHeight)
    {
        if (Profile is null)
            return;

        Profile.Appearance = Profile.Appearance.WithHeight(newHeight);

        UpdateSizeText();
        ReloadPreview();
    }

    private void ResetWidth()
    {
        if (Profile is null || !_prototypeManager.TryIndex<SpeciesPrototype>(Profile.Species, out var speciesPrototype))
            return;

        var defaultWidth = speciesPrototype.DefaultWidth;

        WidthSlider.MinValue = speciesPrototype.MinWidth;
        WidthSlider.MaxValue = speciesPrototype.MaxWidth;
        WidthSlider.Value = defaultWidth;

        Profile.Appearance = Profile.Appearance.WithWidth(defaultWidth);

        UpdateSizeText();
    }

    private void ResetHeight()
    {
        if (Profile is null || !_prototypeManager.TryIndex<SpeciesPrototype>(Profile.Species, out var speciesPrototype))
            return;

        var defaultHeight = speciesPrototype.DefaultHeight;

        HeightSlider.MinValue = speciesPrototype.MinHeight;
        HeightSlider.MaxValue = speciesPrototype.MaxHeight;
        HeightSlider.Value = defaultHeight;

        Profile.Appearance = Profile.Appearance.WithHeight(defaultHeight);

        UpdateSizeText();
    }

    private void ResetSize()
    {
        if (Profile is null || !_prototypeManager.HasIndex<SpeciesPrototype>(Profile.Species))
            return;

        ResetWidth();
        ResetHeight();
    }

    private void UpdateSizeControls()
    {
        if (Profile is null || !_prototypeManager.TryIndex<SpeciesPrototype>(Profile.Species, out var speciesPrototype))
            return;

        WidthSlider.MinValue = speciesPrototype.MinWidth;
        WidthSlider.MaxValue = speciesPrototype.MaxWidth;
        WidthSlider.Value = Profile.Appearance.Width;

        HeightSlider.MinValue = speciesPrototype.MinHeight;
        HeightSlider.MaxValue = speciesPrototype.MaxHeight;
        HeightSlider.Value = Profile.Appearance.Height;

        UpdateSizeText();
    }

    private void OnSpeciesInfoButtonPressed(BaseButton.ButtonEventArgs args)
    {
        // TODO GUIDEBOOK
        // make the species guide book a field on the species prototype.
        // I.e., do what jobs/antags do.

        var guidebookController = UserInterfaceManager.GetUIController<GuidebookUIController>();
        var species = Profile?.Species ?? HumanoidCharacterProfile.DefaultSpecies;
        var page = DefaultSpeciesGuidebook;
        if (_prototypeManager.HasIndex<GuideEntryPrototype>(species))
            page = new ProtoId<GuideEntryPrototype>(species.Id); // Gross. See above todo comment.

        if (_prototypeManager.Resolve(DefaultSpeciesGuidebook, out var guideRoot))
        {
            var dict = new Dictionary<ProtoId<GuideEntryPrototype>, GuideEntry>();
            dict.Add(DefaultSpeciesGuidebook, guideRoot);
            //TODO: Don't close the guidebook if its already open, just go to the correct page
            guidebookController.OpenGuidebook(dict, includeChildren: true, selected: page);
        }
    }

    private void OnSkinColorOnValueChanged()
    {
        if (Profile is null) return;

        var skin = _prototypeManager.Index<SpeciesPrototype>(Profile.Species).SkinColoration;
        var strategy = _prototypeManager.Index(skin).Strategy;

        switch (strategy.InputType)
        {
            case SkinColorationStrategyInput.Unary:
                {
                    if (!Skin.Visible)
                    {
                        Skin.Visible = true;
                        RgbSkinColorContainer.Visible = false;
                    }

                    var color = strategy.FromUnary(Skin.Value);

                    _markingsModel.SetOrganSkinColor(color);
                    Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithSkinColor(color));

                    break;
                }
            case SkinColorationStrategyInput.Color:
                {
                    if (!RgbSkinColorContainer.Visible)
                    {
                        Skin.Visible = false;
                        RgbSkinColorContainer.Visible = true;
                    }

                    var color = strategy.ClosestSkinColor(_rgbSkinColorSelector.Color);

                    _markingsModel.SetOrganSkinColor(color);
                    Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithSkinColor(color));

                    break;
                }
        }

        ReloadProfilePreview();
    }

    private void UpdateHairPickers()
    {
        if (Profile == null)
            return;

        var hairMarking = CreateHairMarkings(
            Profile.Appearance.HairStyleId,
            HairStyles.DefaultHairStyle,
            Profile.Appearance.HairColor,
            Profile.Appearance.HairMarkingEffect);

        var facialHairMarking = CreateHairMarkings(
            Profile.Appearance.FacialHairStyleId,
            HairStyles.DefaultFacialHairStyle,
            Profile.Appearance.FacialHairColor,
            Profile.Appearance.FacialHairMarkingEffect);

        HairStylePicker.UpdateData(
            hairMarking,
            Profile.Species,
            1);
        FacialHairPicker.UpdateData(
            facialHairMarking,
            Profile.Species,
            1);
    }

    private static List<Marking> CreateHairMarkings(
        string styleId,
        string defaultStyleId,
        Color color,
        MarkingEffect? effect)
    {
        if (styleId == defaultStyleId)
            return [];

        var effects = effect is { } ext
            ? new List<MarkingEffect> { ext.Clone() }
            : null;

        return
        [
            new(
                styleId,
                [color],
                effects)
        ];
    }

    private void UpdateCMarkingsHair()
    {
        RebuildAppearanceMarkings();
    }

    private void UpdateCMarkingsFacialHair()
    {
        RebuildAppearanceMarkings();
    }

    private void RebuildAppearanceMarkings()
    {
        if (Profile == null)
            return;

        var sponsorPrototypes = _sponsorsMgr?.GetClientPrototypes().ToArray() ?? [];
        var appearance = HumanoidCharacterAppearance.EnsureValid(
            Profile.Appearance,
            Profile.Species,
            Profile.Sex,
            sponsorPrototypes);

        Profile = Profile.WithCharacterAppearance(appearance);
        UpdateMarkings();
    }
}
