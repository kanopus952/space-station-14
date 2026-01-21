using Content.Client._Sunrise.TimeCounterContainer;

namespace Content.Client._Sunrise.Tutorial.Components;

[RegisterComponent]
public sealed partial class TimeCounterUiComponent : Component
{
    public TimeCounter? Counter;
}
