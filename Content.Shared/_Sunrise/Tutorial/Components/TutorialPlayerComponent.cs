using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Tutorial.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TutorialPlayerComponent : Component
{
    public string SequenceId;
    public int StepIndex;
    public bool Completed;
}
