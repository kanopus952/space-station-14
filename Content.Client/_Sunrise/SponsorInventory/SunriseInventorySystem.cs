using System.Collections.Generic;
using Content.Shared._Sunrise.SponsorInventory;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Sunrise.Interfaces.Shared;
using Robust.Client;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.SponsorInventory;

public sealed class SunriseInventorySystem : EntitySystem
{
    [Dependency] private readonly IBaseClient _baseClient = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly Dictionary<int, SunriseInventoryProfile> _profiles = new();
    private ISharedSponsorsManager? _sponsors;

    public override void Initialize()
    {
        base.Initialize();

        IoCManager.Instance!.TryResolveType(out _sponsors);
        _baseClient.RunLevelChanged += OnRunLevelChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _baseClient.RunLevelChanged -= OnRunLevelChanged;
    }

    private void OnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (e.NewLevel == ClientRunLevel.Initialize)
            _profiles.Clear();
    }

    public SunriseInventoryProfile GetInventoryProfile(int slot)
    {
        return _profiles.TryGetValue(slot, out var profile)
            ? profile.Clone()
            : new SunriseInventoryProfile();
    }

    public void SetInventoryProfile(int slot, SunriseInventoryProfile profile)
    {
        if (_player.LocalSession == null)
            return;

        var validProfile = SunriseInventoryValidation.EnsureValid(
            profile,
            _player.LocalSession,
            _prototype,
            _sponsors);

        if (validProfile.IsEmpty())
            _profiles.Remove(slot);
        else
            _profiles[slot] = validProfile.Clone();

        RaiseNetworkEvent(new SunriseInventoryProfileChangedEvent(slot, validProfile));
    }

    public void SelectPet(string? petSelection)
    {
        _cfg.SetCVar(SunriseCCVars.SponsorPet, petSelection ?? string.Empty);
        RaiseNetworkEvent(new SunriseInventoryPetSelectedEvent(petSelection));
    }
}
