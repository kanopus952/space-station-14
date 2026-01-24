using System.Numerics;
using Content.Shared.Construction.Conditions;

namespace Content.Server._Sunrise.Tutorial.Components;

[RegisterComponent]
public sealed partial class TutorialMapComponent : Component
{
    [ViewVariables]
    public List<EntityUid> LoadedGrids = new();

    [ViewVariables]
    public Dictionary<EntityUid, Vector2> GridOffsets = new();

    [ViewVariables]
    public string MapName = "Tutorial Map (DO NOT TOUCH)";

    [ViewVariables]
    public Vector2 CoordinateStep = new(0, 200);
}
