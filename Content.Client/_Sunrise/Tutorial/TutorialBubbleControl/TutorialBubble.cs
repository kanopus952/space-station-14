using System.Numerics;
using Content.Client._Sunrise.UserInterface.RichText;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Tutorial.TutorialBubbleControl;

public abstract class TutorialBubble : Control
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private readonly SharedTransformSystem _transformSystem;

    /// <summary>
    ///     World-space vertical offset above the entity's centre.
    ///     Increase to push the bubble higher above the mob's head.
    /// </summary>
    private const float EntityVerticalOffset = 0.6f;

    private static readonly TimeSpan FadeInDuration = TimeSpan.FromSeconds(0.3);
    private static readonly TimeSpan FadeOutDuration = TimeSpan.FromSeconds(0.3);

    /// <summary>Allowed rich-text tags for bubble message content.</summary>
    public static readonly Type[] BubbleTags =
    [
        typeof(BoldItalicTag),
        typeof(BoldTag),
        typeof(BulletTag),
        typeof(ColorTag),
        typeof(HeadingTag),
        typeof(ItalicTag),
        typeof(TutorialKeybindTag),
    ];

    // man down
    public event Action<EntityUid, TutorialBubble>? OnDied;

    private readonly EntityUid _senderEntity;

    private enum FadeState { FadingIn, Visible, FadingOut }

    private FadeState _fade = FadeState.FadingIn;
    private TimeSpan _transitionStart;
    // Guards against Timer.Spawn(0, Die) being queued on every frame once alpha reaches 0.
    private bool _dying;

    public static TutorialBubble Create(string message, EntityUid senderEntity)
    {
        return new TutorialMainBubble(message, senderEntity, string.Empty);
    }

    protected TutorialBubble(string message, EntityUid senderEntity, string styleClass, Color? fontColor = null)
    {
        IoCManager.InjectDependencies(this);
        _senderEntity = senderEntity;
        _transformSystem = _entityManager.System<SharedTransformSystem>();
        _transitionStart = _timing.RealTime;

        // Clip text so content never bleeds outside during layout.
        RectClipContent = true;

        var control = BuildBubble(message, styleClass, fontColor);
        AddChild(control);
        ForceRunStyleUpdate();
        control.Measure(Vector2Helpers.Infinity);
    }

    /// <summary>
    ///     Begin fading out immediately. No-op if already fading out.
    /// </summary>
    public void FadeNow()
    {
        if (_fade == FadeState.FadingOut)
            return;

        _fade = FadeState.FadingOut;
        _transitionStart = _timing.RealTime;
    }

    /// <summary>
    ///     Reset to a fresh fade-in (used when text is updated on an existing bubble).
    ///     Cancels any in-progress fade-out.
    /// </summary>
    public void ResetFade()
    {
        _fade = FadeState.FadingIn;
        _transitionStart = _timing.RealTime;
    }

    /// <summary>Updates the displayed text without recreating the bubble control.</summary>
    public abstract void SetMessage(string locMessage);

    protected abstract Control BuildBubble(string message, string styleClass, Color? fontColor = null);

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        // Hide (without dying) when the entity is not renderable or is on a different map.
        if (!_entityManager.TryGetComponent<TransformComponent>(_senderEntity, out var xform)
            || xform.MapID != _eyeManager.CurrentEye.Position.MapId)
        {
            Modulate = Color.White.WithAlpha(0);
            return;
        }

        var elapsed = (float)(_timing.RealTime - _transitionStart).TotalSeconds;
        float alpha;

        switch (_fade)
        {
            case FadeState.FadingIn:
                alpha = Math.Clamp(elapsed / (float)FadeInDuration.TotalSeconds, 0f, 1f);
                if (alpha >= 1f)
                    _fade = FadeState.Visible;
                break;

            case FadeState.Visible:
                alpha = 1f;
                break;

            case FadeState.FadingOut:
                alpha = Math.Clamp(1f - elapsed / (float)FadeOutDuration.TotalSeconds, 0f, 1f);
                if (alpha <= 0f)
                {
                    Modulate = Color.White.WithAlpha(0);
                    if (!_dying)
                    {
                        _dying = true;
                        // Defer via Timer.Spawn so OnDied does not fire inside an active
                        // FrameUpdate / collection-iteration call. (SpeechBubble pattern)
                        Timer.Spawn(0, Die);
                    }
                    return;
                }
                break;

            default:
                alpha = 1f;
                break;
        }

        Modulate = Color.White.WithAlpha(alpha);

        var eyeOffset = (-_eyeManager.CurrentEye.Rotation).ToWorldVec() * -EntityVerticalOffset;
        var worldPos = _transformSystem.GetWorldPosition(xform) + eyeOffset;
        var center = _eyeManager.WorldToScreen(worldPos) / UIScale;

        // Anchor bubble so its bottom-centre sits at the projected world position.
        var screenPos = center - new Vector2(Size.X / 2f, Size.Y);
        // Round to nearest 0.5 px for crisp sub-pixel rendering.
        screenPos = (screenPos * 2).Rounded() / 2;
        LayoutContainer.SetPosition(this, screenPos);
    }

    private void Die()
    {
        if (Disposed)
            return;

        OnDied?.Invoke(_senderEntity, this);
    }

    public static FormattedMessage FormatSpeech(string message, Color? fontColor = null)
    {
        var msg = new FormattedMessage();
        if (fontColor != null)
            msg.PushColor(fontColor.Value);
        msg.AddMarkupOrThrow(message);
        return msg;
    }
}

// ─────────────────────────────────────────────────────────────────────────────

public sealed class TutorialMainBubble : TutorialBubble
{
    private TutorialBubbleControl _control = default!;

    public TutorialMainBubble(string message, EntityUid senderEntity, string styleClass, Color? fontColor = null)
        : base(message, senderEntity, styleClass, fontColor) { }

    protected override Control BuildBubble(string message, string styleClass, Color? fontColor = null)
    {
        _control = new TutorialBubbleControl();
        _control.BubbleFrame.StyleClasses.Add(styleClass);
        _control.BubbleText.SetMessage(FormatSpeech(message, fontColor), BubbleTags);
        return _control;
    }

    public override void SetMessage(string locMessage)
    {
        _control.BubbleText.SetMessage(FormatSpeech(locMessage), BubbleTags);
    }
}
