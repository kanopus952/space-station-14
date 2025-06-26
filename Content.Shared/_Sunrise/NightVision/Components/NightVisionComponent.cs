using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.NightVision.Components;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class NightVisionComponent : Component
{
    [DataField]
    public EntProtoId Effect = "EffectNightVision";
}
