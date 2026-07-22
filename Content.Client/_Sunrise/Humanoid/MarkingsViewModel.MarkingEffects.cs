using Content.Shared._Sunrise.MarkingEffects;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Client.Humanoid;

public sealed partial class MarkingsViewModel
{
    public void TrySetMarkingEffect(
        ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId,
        int colorIndex,
        MarkingEffect effect)
    {
        if (TryGetMarking(organ, layer, markingId) is not { } marking)
            return;

        if (colorIndex < 0 || colorIndex >= marking.MarkingColors.Count)
            return;

        var changed = false;

        if (effect.Colors.TryGetValue("base", out var baseColor) &&
            marking.MarkingColors[colorIndex] != baseColor)
        {
            marking.SetColor(colorIndex, baseColor);
            changed = true;
        }

        if (!marking.GetMarkingEffectOrDefault(colorIndex).Equals(effect))
        {
            marking.SetMarkingEffect(colorIndex, effect.Clone());
            changed = true;
        }

        if (!changed)
            return;

        MarkingsChanged?.Invoke(organ, layer);
    }
}
