using Content.Shared._Sunrise.Tutorial.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Prototypes;

/// <summary>
///     Random weighting dataset for solutions, able to specify reagents quantity.
/// </summary>
[Prototype]
public sealed partial class TutorialStepPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Instruction = default!;

    [DataField]
    public bool Optional;

    [DataField]
    public List<TutorialCondition> Conditions = new();
}
