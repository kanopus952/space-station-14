using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Tutorial.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TutorialObservableComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> Observers = new();
}
