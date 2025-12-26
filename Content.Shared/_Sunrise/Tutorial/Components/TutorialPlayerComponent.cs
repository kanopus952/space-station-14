using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Tutorial.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TutorialPlayerComponent : Component
{
    [DataField, AutoNetworkedField]
    public string SequenceId = "BasicTutorial";

    [DataField, AutoNetworkedField]
    public int StepIndex;

    [DataField, AutoNetworkedField]
    public bool Completed;

    [DataField, AutoNetworkedField]
    public EntityUid? CurrentBubbleTarget;
}
