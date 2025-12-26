using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Tutorial.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TutorialBubbleComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Instruction;
}
