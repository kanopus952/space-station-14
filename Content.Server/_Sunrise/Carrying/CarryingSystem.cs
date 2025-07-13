using Content.Server.Resist;
using Content.Shared._Sunrise.Carrying;
using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Physics.Components;

namespace Content.Server._Sunrise.Carrying
{
    public sealed class CarryingSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly EscapeInventorySystem _escapeInventorySystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BeingCarriedComponent, MoveInputEvent>(OnMoveInput);
        }

        /// <summary>
        /// Try to escape via the escape inventory system.
        /// </summary>
        private void OnMoveInput(EntityUid uid, BeingCarriedComponent component, ref MoveInputEvent args)
        {
            if (!TryComp<CanEscapeInventoryComponent>(uid, out var escape))
                return;

            if (args.OldMovement == MoveButtons.None || args.OldMovement == MoveButtons.Walk)
                return;

            if (_actionBlockerSystem.CanInteract(uid, component.Carrier))
            {
                _escapeInventorySystem.AttemptEscape(uid, component.Carrier, escape, MassContest(component.Carrier, uid));
            }
        }
        public float MassContest(EntityUid roller, EntityUid target, PhysicsComponent? rollerPhysics = null, PhysicsComponent? targetPhysics = null)
        {
            if (!Resolve(roller, ref rollerPhysics, false) || !Resolve(target, ref targetPhysics, false))
                return 1f;

            if (targetPhysics.FixturesMass == 0)
                return 1f;

            return rollerPhysics.FixturesMass / targetPhysics.FixturesMass;
        }
    }
}
