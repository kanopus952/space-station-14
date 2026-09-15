using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.NightVision.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ToggleableNightVisionComponent : Component
{
    [DataField]
    public EntProtoId Action = "ToggleableNightVision";

    [ViewVariables]
    public EntityUid? ActionEntity;

    [ViewVariables, AutoNetworkedField]
    public bool Active;
}
