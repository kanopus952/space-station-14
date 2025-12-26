using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Tutorial.Prototypes;

/// <summary>
///     Random weighting dataset for solutions, able to specify reagents quantity.
/// </summary>
[Prototype]
public sealed partial class TutorialSequencePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public string Tooltip = string.Empty;

    [DataField]
    public ResPath Grid;

    [DataField]
    public EntProtoId PlayerEntity;

    [DataField]
    public ResPath Texture;

    [DataField]
    public List<ProtoId<TutorialStepPrototype>> Steps = new();
}
