using System.Collections.Generic;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Tutorial.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TutorialObservableComponent : Component
{
    [ViewVariables]
    public readonly HashSet<EntityUid> Observers = new();
}
