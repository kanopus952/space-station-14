using Content.Client._Sunrise.Tutorial;
using Robust.Shared.GameObjects;

namespace Content.Client._Sunrise.Tutorial.Components;

[RegisterComponent]
public sealed partial class TutorialTimeCounterUiComponent : Component
{
    public TimeCounter? Counter;
}
