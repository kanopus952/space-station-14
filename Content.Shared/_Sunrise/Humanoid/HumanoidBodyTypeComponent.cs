// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.DisplacementMap;
using Content.Shared.Humanoid;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Humanoid;

/// <summary>
///     Stores the body type and associated displacement maps for a humanoid character.
///     Companion component to <see cref="HumanoidAppearanceComponent"/>.
///     Auto-attached to any entity that receives <see cref="HumanoidAppearanceComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HumanoidBodyTypeComponent : Component
{
    /// <summary>
    ///     Current body type prototype. Determines which body-type sprite overlays are applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<BodyTypePrototype> BodyType = SharedHumanoidAppearanceSystem.DefaultBodyType;

    /// <summary>
    ///     Body-type-specific displacement maps for markings.
    ///     Format: bodytype name → layer → DisplacementData.
    ///     Server-only (not networked); client reads it from YAML via component state.
    /// </summary>
    [DataField]
    public Dictionary<string, Dictionary<HumanoidVisualLayers, DisplacementData>> BodyTypeMarkingsDisplacement = new();

    /// <summary>
    ///     Sex-specific displacement maps for markings.
    ///     Format: sex → layer → DisplacementData.
    /// </summary>
    [DataField]
    public Dictionary<Sex, Dictionary<HumanoidVisualLayers, DisplacementData>> SexMarkingsDisplacement = new();

    /// <summary>
    ///     Body-type and sex-specific displacement maps for markings.
    ///     Format: bodytype name → sex → layer → DisplacementData.
    /// </summary>
    [DataField]
    public Dictionary<string, Dictionary<Sex, Dictionary<HumanoidVisualLayers, DisplacementData>>> BodyTypeSexMarkingsDisplacement = new();
}
