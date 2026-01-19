using Content.Shared._Sunrise.Tutorial.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class TutorialPlayerComponent : Component
{
    [DataField]
    public ProtoId<TutorialSequencePrototype> SequenceId = "BasicTutorial";

    [ViewVariables, AutoNetworkedField]
    public int StepIndex;

    [ViewVariables]
    public bool Completed;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? CurrentBubbleTarget;

    [ViewVariables, AutoPausedField]
    public TimeSpan? EndTime;

    [ViewVariables]
    public EntityUid? Grid;
}
