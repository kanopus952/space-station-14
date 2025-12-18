using Robust.Shared.Prototypes;

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
    public List<ProtoId<TutorialStepPrototype>> Steps = new();
}
