using Content.Shared._Sunrise.Tutorial.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class TutorialPlayerComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<TutorialSequencePrototype> SequenceId = "IntroductionTutorial";

    [ViewVariables, AutoNetworkedField]
    public int StepIndex;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? CurrentBubbleTarget;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Target;

    [ViewVariables, AutoPausedField]
    public TimeSpan? EndTime;

    [ViewVariables]
    public EntityUid? Grid;
}
