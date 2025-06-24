using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mech.Components;

/// <summary>
/// Делает мех уязвимым к электромагнитным импульсам
/// </summary>
[RegisterComponent]
public sealed partial class MechAffectedByEMPComponent : Component
{
    [ViewVariables]
    public TimeSpan NextPulseTime;

    [DataField]
    public TimeSpan CooldownTime = TimeSpan.FromSeconds(6);

    [DataField]
    public DamageSpecifier EmpDamage = new()
    {
        DamageDict = new()
        {
            { "Shock", 30f },
        }
    };

    [DataField]
    public EntProtoId EffectEMP = "EffectSparks";
}
