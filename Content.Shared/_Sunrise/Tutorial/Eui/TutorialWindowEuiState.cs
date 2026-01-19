using System.Collections.Generic;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Content.Shared.Eui;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Tutorial.Eui;

[Serializable, NetSerializable]
public sealed class TutorialWindowEuiState(List<string> tutorials) : EuiStateBase
{
    public List<string> CompletedTutorials = tutorials;
}
/// <summary>
///     EUI message sent from the client when a tutorial button is pressed.
///     Contains data required to start the selected tutorial sequence.
/// </summary>
[Serializable, NetSerializable]
public sealed class TutorialButtonPressedEuiMessage(EntProtoId? playerEntity, ResPath? grid) : EuiMessageBase
{
    public EntProtoId? PlayerEntity = playerEntity;
    public ResPath? Grid = grid;
}

[Serializable, NetSerializable]
public sealed class TutorialQuitButtonPressedMessage() : EuiMessageBase
{
}
