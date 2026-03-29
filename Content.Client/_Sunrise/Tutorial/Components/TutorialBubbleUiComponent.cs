using Content.Client._Sunrise.Tutorial.TutorialBubbleControl;

namespace Content.Client._Sunrise.Tutorial.Components;

[RegisterComponent]
public sealed partial class TutorialBubbleUiComponent : Component
{
    public TutorialBubble? Bubble;
    /// <summary>
    ///     The last localization key shown in <see cref="Bubble"/>.
    ///     Used to avoid restarting the fade-in animation when unrelated component fields change.
    /// </summary>
    public string? LastInstruction;
}
