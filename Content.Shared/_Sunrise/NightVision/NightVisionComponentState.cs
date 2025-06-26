using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.NightVision;

[NetSerializable, Serializable]
public sealed class NightVisionComponentState : ComponentState
{
    public EntProtoId Effect;

    public NightVisionComponentState(EntProtoId effect)
    {
        Effect = effect;
    }
}
