using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._Sunrise.Tutorial.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TutorialDistanceTrackerComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Vector2? LastPosition;

    [ViewVariables, AutoNetworkedField]
    public float Distance;
}
