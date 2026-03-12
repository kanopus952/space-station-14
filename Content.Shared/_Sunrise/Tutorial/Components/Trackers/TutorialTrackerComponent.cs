using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TutorialTrackerComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Dictionary<(string Key, EntProtoId Target), int> Counters = new();

    [ViewVariables, AutoNetworkedField]
    public HashSet<EntProtoId> TargetPrototypes = new();

    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> ObservedEntities = new();
}
