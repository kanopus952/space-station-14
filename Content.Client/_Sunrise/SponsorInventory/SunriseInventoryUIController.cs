using Content.Client.Lobby;
using Content.Shared._Sunrise.SponsorInventory;
using Content.Shared.Preferences;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Sunrise.SponsorInventory;

public sealed class SunriseInventoryUIController : UIController, IOnStateExited<LobbyState>
{
    [Dependency] private readonly IClientPreferencesManager _preferences = default!;

    [UISystemDependency] private readonly SunriseInventorySystem _inventory = default!;

    private SunriseInventoryWindow? _window;

    public void Open()
    {
        EnsureWindow();
        RefreshWindow();

        if (_window is { IsOpen: true })
            return;

        _window?.OpenCentered();
    }

    public void OnStateExited(LobbyState state)
    {
        _window?.Close();
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<SunriseInventoryWindow>();
        _window.OnSaveRequested += SaveProfile;
        _window.OnPetSelected += SelectPet;
    }

    private void RefreshWindow()
    {
        if (_window == null || _preferences.Preferences == null)
            return;

        var selectedSlot = _preferences.Preferences.SelectedCharacterIndex;
        _window.SetProfile(
            _preferences.Preferences.SelectedCharacter as HumanoidCharacterProfile,
            selectedSlot,
            _inventory.GetInventoryProfile(selectedSlot));
    }

    private void SaveProfile(HumanoidCharacterProfile profile, int slot, SunriseInventoryProfile sponsorInventory)
    {
        _inventory.SetInventoryProfile(slot, sponsorInventory);
        _preferences.UpdateCharacter(profile, slot);
        UIManager.GetUIController<LobbyUIController>().ReloadCharacterSetup();
        RefreshWindow();
    }

    private void SelectPet(string? petSelection)
    {
        _inventory.SelectPet(petSelection);
        _window?.RefreshPets();
    }
}
