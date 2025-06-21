using Robust.Shared.Serialization;

namespace Content.Shared.Mech.Events;

[Serializable, NetSerializable]
public sealed partial class UpdateAppearanceEvent : EntityEventArgs
{
    public UpdateAppearanceEvent(NetEntity uid)
    {
        Uid = uid;
    }
    public NetEntity Uid { get; }
}
