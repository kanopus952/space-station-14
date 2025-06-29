using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.NightVision;

[NetSerializable, Serializable]
public sealed class NightVisionComponentState(EntProtoId effect) : ComponentState
{
    public EntProtoId Effect = effect;

}
