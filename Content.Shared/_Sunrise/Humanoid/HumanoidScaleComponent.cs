// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;

namespace Content.Shared._Sunrise.Humanoid;

/// <summary>
///     Stores the sprite scale (width/height) for a humanoid character.
///     Companion component to <see cref="HumanoidAppearanceComponent"/>.
///     Auto-attached to any entity that receives <see cref="HumanoidAppearanceComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HumanoidScaleComponent : Component
{
    /// <summary>
    ///     Horizontal scale factor for this humanoid's sprite (1.0 = default width).
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public float Width = 1f;

    /// <summary>
    ///     Vertical scale factor for this humanoid's sprite (1.0 = default height).
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public float Height = 1f;
}
