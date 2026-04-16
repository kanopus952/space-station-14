using Content.Shared._Sunrise.Roadmap;
using Robust.Shared.Maths;

namespace Content.Client._Sunrise.Roadmap;

public static class RoadmapColors
{
    public static readonly Color WindowBackground = Color.FromHex("#1d1c1c");
    public static readonly Color ItemBackground = Color.FromHex("#121111");
    public static readonly Color ItemHintText = Color.FromHex("#666666");
    public static readonly Color VersionCardBackground = Color.FromHex("#121111");
    public static readonly Color VersionHeaderBackground = Color.FromHex("#2a2929");
    public static readonly Color VersionHeaderText = Color.FromHex("#ffffff");

    public static readonly Color StatePlanned = Color.FromHex("#e74c3c");
    public static readonly Color StateInProgress = Color.FromHex("#3498db");
    public static readonly Color StatePartial = Color.FromHex("#f1c40f");
    public static readonly Color StateComplete = Color.FromHex("#2ecc71");

    public static Color GetStateColor(RoadmapItemState state)
    {
        return state switch
        {
            RoadmapItemState.Planned => StatePlanned,
            RoadmapItemState.InProgress => StateInProgress,
            RoadmapItemState.Partial => StatePartial,
            RoadmapItemState.Complete => StateComplete,
            _ => Color.Transparent,
        };
    }
}
