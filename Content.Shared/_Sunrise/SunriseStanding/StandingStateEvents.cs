using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Standing;

[Serializable, NetSerializable]
public sealed partial class StandUpDoAfterEvent : SimpleDoAfterEvent;
