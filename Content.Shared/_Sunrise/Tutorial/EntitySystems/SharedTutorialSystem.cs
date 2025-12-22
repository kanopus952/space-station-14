using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Conditions;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.EntitySystems;

/// <summary>
/// Adds details about fuel level when examining antimatter engine fuel containers.
/// </summary>
public sealed class SharedTutorialSystem : EntitySystem
{
    [Dependency] private readonly SharedTutorialConditionsSystem _tutorial = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TutorialPlayerComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, TutorialPlayerComponent comp, ComponentInit args)
    {
    }
    public TutorialStepPrototype GetCurrentStep(EntityUid uid)
    {
        var comp = Comp<TutorialPlayerComponent>(uid);
        var sequence = _proto.Index<TutorialSequencePrototype>(comp.SequenceId);

        var stepId = sequence.Steps[comp.StepIndex];
        return _proto.Index<TutorialStepPrototype>(stepId);
    }
    public void TryCheck(EntityUid uid)
    {
        var step = GetCurrentStep(uid);

        foreach (var condition in step.Conditions)
        {
            if (!_tutorial.TryCondition(uid, condition))
                return;
        }

        Advance(uid);
    }

    public void Advance(EntityUid uid)
    {
        var comp = Comp<TutorialPlayerComponent>(uid);
        var sequence = _proto.Index<TutorialSequencePrototype>(comp.SequenceId);

        comp.StepIndex++;

        if (comp.StepIndex >= sequence.Steps.Count)
        {
            comp.Completed = true;
            EndTutorial(uid);
            return;
        }

        OnStepChanged(uid);
    }
}
