using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Effects;
using Content.Shared._Sunrise.Tutorial.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.EntitySystems;

public abstract partial class SharedTutorialSystem
{
    private void ApplyStepEffects(Entity<TutorialPlayerComponent> ent, TutorialStepPrototype step)
    {
        for (var i = 0; i < step.Effects.Count; i++)
        {
            var effect = step.Effects[i];
            switch (effect)
            {
                case TutorialStatusEffect statusEffect:
                    if (_net.IsServer)
                        _statusEffects.TrySetStatusEffectDuration(ent, statusEffect.Status, statusEffect.Duration);
                    break;
                case TutorialComponentEffect componentEffect:
                    if (componentEffect.Components.Count > 0)
                        EntityManager.AddComponents(ent, componentEffect.Components, componentEffect.RemoveExisting);
                    break;
                case TutorialEntityEffect entityEffect:
                    if (_net.IsServer)
                        _entityEffects.ApplyEffects(ent, entityEffect.Effects, entityEffect.Scale, ent);
                    break;
            }
        }
    }

    private void ClearStepEffects(Entity<TutorialPlayerComponent> ent)
    {
        if (!TryGetCurrentStep(ent, out var step))
            return;

        for (var i = 0; i < step.Effects.Count; i++)
        {
            var effect = step.Effects[i];
            switch (effect)
            {
                case TutorialStatusEffect { RemoveOnExit: true } statusEffect:
                    if (_net.IsServer)
                        _statusEffects.TryRemoveStatusEffect(ent, statusEffect.Status);
                    break;
                case TutorialComponentEffect { RemoveOnExit: true } componentEffect:
                    if (componentEffect.Components.Count > 0)
                        EntityManager.RemoveComponents(ent, componentEffect.Components);
                    break;
            }
        }
    }
}
