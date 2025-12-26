using Content.Shared._Sunrise.Tutorial.Prototypes;
using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Tutorial.Eui;

[Serializable, NetSerializable]
public sealed class TutorialWindowEuiState : EuiStateBase
{
}

[Serializable, NetSerializable]
public sealed class TutorialButtonPressedEuiMessage(EntProtoId? playerEntity, ResPath? grid) : EuiMessageBase
{
    public EntProtoId? PlayerEntity = playerEntity;
    public ResPath? Grid = grid;
}
