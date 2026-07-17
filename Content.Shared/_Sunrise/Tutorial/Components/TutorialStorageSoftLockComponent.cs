using System.Collections.Generic;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Tutorial.Components;

/// <summary>
/// Временно отмечает сущность, участвующую в storage softlock туториала.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TutorialStorageSoftLockComponent : Component
{
    /// <summary>
    /// Игроки, для которых сущность участвует в текущем storage softlock.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> Players = [];
}
