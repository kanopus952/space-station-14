using System.Numerics;

namespace Content.Server._Sunrise.Tutorial.Components;

[RegisterComponent]
public sealed partial class TutorialMapComponent : Component
{
    [ViewVariables]
    public List<EntityUid> LoadedGrids;

    [ViewVariables]
    public Dictionary<EntityUid, Vector2> GridOffsets;

    [ViewVariables]
    public string MapName = "Tutorial Map (DO NOT TOUCH)";

    [ViewVariables]
    public Vector2 CoordinateStep = new(0, 200);
}
