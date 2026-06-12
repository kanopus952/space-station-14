using System;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.SponsorInventory;

[Serializable, NetSerializable]
public sealed class SunriseInventoryPetSelectedEvent(string? selectedPetSelection) : EntityEventArgs
{
    public string? SelectedPetSelection = selectedPetSelection;
}

[Serializable, NetSerializable]
public sealed class SunriseInventoryProfileChangedEvent(int slot, SunriseInventoryProfile profile) : EntityEventArgs
{
    public int Slot = slot;
    public SunriseInventoryProfile Profile = profile;
}
