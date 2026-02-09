using System.Linq;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void SetName(string newName)
    {
        Profile = Profile?.WithName(newName);
        SetDirty();

        if (!IsDirty)
            return;

        SpriteView.SetName(newName);
    }

    private void UpdateNameEdit()
    {
        NameEdit.Text = Profile?.Name ?? "";
    }

    private void RandomizeEverything()
    {
        var ignoredSpecies = new HashSet<string>();
        foreach (var speciesPrototype in _prototypeManager.EnumeratePrototypes<SpeciesPrototype>())
        {
            if (speciesPrototype.SponsorOnly &&
                _sponsorsMgr != null &&
                !_sponsorsMgr.GetClientPrototypes().Contains(speciesPrototype.ID))
            {
                ignoredSpecies.Add(speciesPrototype.ID);
            }
        }

        Profile = HumanoidCharacterProfile.Random(ignoredSpecies);
        SetProfile(Profile, CharacterSlot);
        SetDirty();
    }

    private void RandomizeName()
    {
        if (Profile == null) return;
        var name = HumanoidCharacterProfile.GetName(Profile.Species, Profile.Gender);
        SetName(name);
        UpdateNameEdit();
    }
}
