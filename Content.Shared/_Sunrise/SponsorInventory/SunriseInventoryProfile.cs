using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.SponsorInventory;

/// <summary>
/// Stores sponsor inventory choices for a humanoid profile.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class SunriseInventoryProfile : IEquatable<SunriseInventoryProfile>
{
    [DataField]
    public SunriseInventorySelection Global { get; set; } = new();

    [DataField]
    public Dictionary<string, SunriseInventorySelection> Jobs { get; set; } = new();

    public SunriseInventoryProfile Clone()
    {
        var clone = new SunriseInventoryProfile
        {
            Global = Global.Clone(),
        };

        foreach (var (job, selection) in Jobs)
        {
            clone.Jobs[job] = selection.Clone();
        }

        return clone;
    }

    public bool IsEmpty()
    {
        return Global.IsEmpty() && Jobs.Count == 0;
    }

    public bool Equals(SunriseInventoryProfile? other)
    {
        if (ReferenceEquals(null, other))
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (!Global.Equals(other.Global) || Jobs.Count != other.Jobs.Count)
            return false;

        foreach (var (job, selection) in Jobs)
        {
            if (!other.Jobs.TryGetValue(job, out var otherSelection) || !selection.Equals(otherSelection))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is SunriseInventoryProfile other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Global);

        foreach (var (job, selection) in Jobs.OrderBy(p => p.Key))
        {
            hash.Add(job);
            hash.Add(selection);
        }

        return hash.ToHashCode();
    }
}

/// <summary>
/// Selected sponsor inventory items for one global or job-specific layer.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class SunriseInventorySelection : IEquatable<SunriseInventorySelection>
{
    [DataField]
    public Dictionary<string, string> SlotItems { get; set; } = new();

    [DataField]
    public List<string> BagItems { get; set; } = new();

    public SunriseInventorySelection Clone()
    {
        return new SunriseInventorySelection
        {
            SlotItems = new Dictionary<string, string>(SlotItems),
            BagItems = new List<string>(BagItems),
        };
    }

    public bool IsEmpty()
    {
        return SlotItems.Count == 0 && BagItems.Count == 0;
    }

    public bool Equals(SunriseInventorySelection? other)
    {
        if (ReferenceEquals(null, other))
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (SlotItems.Count != other.SlotItems.Count || !BagItems.SequenceEqual(other.BagItems))
            return false;

        foreach (var (slot, item) in SlotItems)
        {
            if (!other.SlotItems.TryGetValue(slot, out var otherItem) || otherItem != item)
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is SunriseInventorySelection other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var (slot, item) in SlotItems.OrderBy(p => p.Key))
        {
            hash.Add(slot);
            hash.Add(item);
        }

        foreach (var item in BagItems)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }
}
