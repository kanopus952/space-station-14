using System.Linq;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Conditions;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Microsoft.VisualBasic.FileIO;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Toolshed.Commands.Generic;

namespace Content.Shared._Sunrise.Tutorial.EntitySystems;

/// <summary>
/// System for educating new players
/// </summary>
public abstract class SharedTutorialSystem : EntitySystem
{
    [Dependency] private readonly SharedTutorialConditionsSystem _tutorial = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TutorialPlayerComponent, ComponentInit>(OnComponentInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TutorialPlayerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            TryCheck(uid);
        }
    }
    private void OnComponentInit(Entity<TutorialPlayerComponent> ent, ref ComponentInit args)
    {
        var step = GetCurrentStep(ent);
        UpdateTutorialBubble(ent, step);
    }
    public TutorialStepPrototype GetCurrentStep(EntityUid uid)
    {
        var comp = Comp<TutorialPlayerComponent>(uid);
        var sequence = _proto.Index<TutorialSequencePrototype>(comp.SequenceId);

        var stepId = sequence.Steps[comp.StepIndex];
        return _proto.Index(stepId);
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

        if (!_proto.TryIndex<TutorialSequencePrototype>(comp.SequenceId, out var sequence))
            return;

        comp.StepIndex++;

        if (comp.StepIndex >= sequence.Steps.Count)
        {
            comp.Completed = true;
            // EndTutorial(uid);
            return;
        }

        var stepId = sequence.Steps.ElementAt(comp.StepIndex);

        if (!_proto.TryIndex(stepId, out var step))
            return;

        OnStepChanged((uid, comp), step);
    }

    private void OnStepChanged(Entity<TutorialPlayerComponent> ent, TutorialStepPrototype step)
    {
        var ev = new TutorialStepChangedEvent();
        RaiseLocalEvent(ent, ev);

        UpdateTutorialBubble(ent, step);
    }

    private void UpdateTutorialBubble(Entity<TutorialPlayerComponent> ent, TutorialStepPrototype step)
    {
        // 1. Удаляем старый bubble (если был)
        if (ent.Comp.CurrentBubbleTarget is { } oldTarget && Exists(oldTarget))
        {
            RemComp<TutorialBubbleComponent>(oldTarget);
        }

        ent.Comp.CurrentBubbleTarget = null;
        // 2. Если bubble не задан — ничего не показываем
        if (step.Bubble == null)
            return;

        // 3. Резолвим цель
        EntityUid? target = step.Bubble.Target.Type switch
        {
            TutorialBubbleTargetType.Self => ent,

            TutorialBubbleTargetType.Entity =>
                TryFindBubbleEntity(ent, step),

            _ => null
        };

        if (target == null || !Exists(target.Value))
        {
            ent.Comp.CurrentBubbleTarget = null;
            return;
        }

        var bubble = EnsureComp<TutorialBubbleComponent>(target.Value);
        bubble.Instruction = step.Bubble.Text;

        ent.Comp.CurrentBubbleTarget = target;

        Dirty(ent);
    }

    private EntityUid? TryFindBubbleEntity(EntityUid uid, TutorialStepPrototype proto)
    {
        const float range = 10f;

        if (proto.Bubble == null)
            return null;

        var targetProtoId = proto.Bubble.Target.Prototype;

        var origin = _transform.GetMapCoordinates(uid);
        EntityUid? best = null;
        var bestDistSq = float.MaxValue;

        foreach (var ent in _lookupSystem.GetEntitiesInRange(uid, range))
        {
            var meta = MetaData(ent);

            if (meta.EntityPrototype == null)
                continue;

            if (meta.EntityPrototype.ID != targetProtoId)
                continue;

            var pos = _transform.GetMapCoordinates(ent);
            var d = (pos.Position - origin.Position).LengthSquared();

            if (d < bestDistSq)
            {
                bestDistSq = d;
                best = ent;
            }
        }

        return best;
    }
}

/// <summary>
/// Event that is raised whenever an implant is removed from someone.
/// Raised on the the implant entity.
/// </summary>

[NetSerializable, Serializable]
public sealed class TutorialStepChangedEvent() : EntityEventArgs
{
}

[NetSerializable, Serializable]
public sealed class UpdateBubbleEvent() : EntityEventArgs
{
}
