using Content.Shared._Sunrise.Tutorial.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class TutorialPlayerComponent : Component
{
    [DataField]
    public ProtoId<TutorialSequencePrototype> SequenceId = "BasicTutorial";

    [DataField, AutoNetworkedField]
    public int StepIndex;

    [DataField]
    public bool Completed;

    [DataField, AutoNetworkedField]
    public EntityUid? CurrentBubbleTarget;

    [DataField, AutoPausedField]
    public TimeSpan? EndTime;
}
