using System.Collections.Generic;
using Content.Shared._Sunrise.Tutorial.Conditions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TutorialTrackerComponent : Component
{
    [AutoNetworkedField]
    public Dictionary<(TutorialEventType Type, EntProtoId? Target), int> Counters = new();

    [AutoNetworkedField]
    public HashSet<EntProtoId> TargetPrototypes = new();

    [AutoNetworkedField]
    public HashSet<EntityUid> ObservedEntities = new();
    public bool ObserveAnyUseInHand;
    public bool ObserveAnyDrop;
    public bool ObserveAnyAttack;
    public bool ObserveAnyExamine;
}
