using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Tutorial.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TutorialProgressBarComponent : Component
{
    [DataField, AutoNetworkedField]
    public int CurrentStepIndex;
}
