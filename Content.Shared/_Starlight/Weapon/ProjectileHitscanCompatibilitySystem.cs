using Content.Shared.Projectiles;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Damage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._Starlight.Weapon;

public sealed partial class ProjectileHitscanCompatibilitySystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();
        // Подписываемся на MapInitEvent, чтобы не ломать проверки сохранения неинициализированных сущностей.
        SubscribeLocalEvent<ProjectileComponent, MapInitEvent>(OnProjectileMapInit);
    }

    private void OnProjectileMapInit(EntityUid uid, ProjectileComponent component, ref MapInitEvent args)
    {
        // Фиксируем вращение при MapInitEvent, чтобы не менять физику во время спавна в проверках сохранения.
        if (TryComp<PhysicsComponent>(uid, out var physics))
        {
            _physics.SetFixedRotation(uid, true, body: physics);
        }

        // Copy damage and armor penetration from HitscanBasicDamageComponent if present
        if (TryComp<HitscanBasicDamageComponent>(uid, out var hitscanDamage))
        {
            component.Damage = hitscanDamage.Damage;
            component.ArmorPenetration = hitscanDamage.ArmorPenetration;
            component.IgnoreResistances = hitscanDamage.IgnoreResistances;
        }

        // Setup stamina damage from HitscanStaminaDamageComponent if present
        if (TryComp<HitscanStaminaDamageComponent>(uid, out var hitscanStamina))
        {
            var stamina = EnsureComp<StaminaDamageOnCollideComponent>(uid);
            stamina.Damage = hitscanStamina.StaminaDamage;
        }
    }
}
