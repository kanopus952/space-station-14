using System.Threading;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Carrying
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class CarriableComponent : Component
    {
        /// <summary>
        ///     необходимое количество свободных рук
        ///     что-бы взять сущность
        /// </summary>
        [DataField("freeHandsRequired")]
        public int FreeHandsRequired = 2;

        public float WalkSpeedModifier = 0.6f;

        public float SprintSpeedModifier = 0.6f;
    }
}
