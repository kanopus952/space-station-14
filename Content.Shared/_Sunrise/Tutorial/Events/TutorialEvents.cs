using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Tutorial.Events;

[Serializable, NetSerializable]
public sealed class TutorialQuitRequestEvent : EntityEventArgs
{
}

[NetSerializable, Serializable]
public sealed class TutorialStepChangedEvent() : EntityEventArgs
{
}

[NetSerializable, Serializable]
public sealed class TutorialEndedEvent() : EntityEventArgs
{
}
