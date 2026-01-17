using Robust.Shared.Serialization;

namespace Content.Shared.Body.Part
{
    /// <summary>
    ///     Defines the type of a body part.
    /// </summary>
    [Serializable, NetSerializable]
    public enum BodyPartType
    {
        Other = 0,
        Torso,
        Head,
        Arm,
        Hand,
        Leg,
        Foot,
        Tail,
        Back,
        // Sunrise - Start
        Dreadlocks,
        Rings
        // Sunrise - End
    }
}
