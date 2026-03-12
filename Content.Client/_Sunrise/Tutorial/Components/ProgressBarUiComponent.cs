using Content.Client._Sunrise.Tutorial.ProgressBar;

namespace Content.Client._Sunrise.Tutorial.Components;

[RegisterComponent]
public sealed partial class ProgressBarUiComponent : Component
{
    public TutorialProgressBar? Bar;
}
