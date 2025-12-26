using System.Numerics;
using Content.Client.Chat.Managers;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Speech;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Tutorial;

public abstract class TutorialBubble : Control
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] protected readonly IConfigurationManager ConfigManager = default!;
    private readonly SharedTransformSystem _transformSystem;

    /// <summary>
    ///     The distance in world space to offset the speech bubble from the center of the entity.
    ///     i.e. greater -> higher above the mob's head.
    /// </summary>
    private const float EntityVerticalOffset = 0.5f;

    /// <summary>
    ///     The default maximum width for speech bubbles.
    /// </summary>
    public const float SpeechMaxWidth = 256;

    private readonly EntityUid _senderEntity;

    public float VerticalOffset { get; set; }
    private float _verticalOffsetAchieved;

    public Vector2 ContentSize { get; private set; }


    public static TutorialBubble CreateTutorialBubble(string message, EntityUid senderEntity)
    {
        return new TutorialMainBubble(message, senderEntity, "tooltipBox");
    }

    public TutorialBubble(string message, EntityUid senderEntity, string speechStyleClass, Color? fontColor = null)
    {
        IoCManager.InjectDependencies(this);
        _senderEntity = senderEntity;
        _transformSystem = _entityManager.System<SharedTransformSystem>();

        // Use text clipping so new messages don't overlap old ones being pushed up.
        RectClipContent = true;

        var bubble = BuildBubble(message, speechStyleClass, fontColor);

        AddChild(bubble);

        ForceRunStyleUpdate();

        bubble.Measure(Vector2Helpers.Infinity);
        ContentSize = bubble.DesiredSize;
        _verticalOffsetAchieved = -ContentSize.Y;
    }

    protected abstract Control BuildBubble(string message, string speechStyleClass, Color? fontColor = null);

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        // Lerp to our new vertical offset if it's been modified.
        if (MathHelper.CloseToPercent(_verticalOffsetAchieved - VerticalOffset, 0, 0.1))
        {
            _verticalOffsetAchieved = VerticalOffset;
        }
        else
        {
            _verticalOffsetAchieved = MathHelper.Lerp(_verticalOffsetAchieved, VerticalOffset, 10 * args.DeltaSeconds);
        }

        if (!_entityManager.TryGetComponent<TransformComponent>(_senderEntity, out var xform) || xform.MapID != _eyeManager.CurrentEye.Position.MapId)
        {
            Modulate = Color.White.WithAlpha(0);
            return;
        }

        var baseOffset = 0f;

        if (_entityManager.TryGetComponent<SpeechComponent>(_senderEntity, out var speech))
            baseOffset = speech.SpeechBubbleOffset;

        var offset = (-_eyeManager.CurrentEye.Rotation).ToWorldVec() * -(EntityVerticalOffset + baseOffset);
        var worldPos = _transformSystem.GetWorldPosition(xform) + offset;

        var lowerCenter = _eyeManager.WorldToScreen(worldPos) / UIScale;
        var screenPos = lowerCenter - new Vector2(ContentSize.X / 2, ContentSize.Y + _verticalOffsetAchieved);
        // Round to nearest 0.5
        screenPos = (screenPos * 2).Rounded() / 2;
        LayoutContainer.SetPosition(this, screenPos);

        var height = MathF.Ceiling(MathHelper.Clamp(lowerCenter.Y - screenPos.Y, 0, ContentSize.Y));
        SetHeight = height;
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


public sealed class TutorialMainBubble : TutorialBubble
{

    public TutorialMainBubble(string message, EntityUid senderEntity, string speechStyleClass, Color? fontColor = null)
        : base(message, senderEntity, speechStyleClass, fontColor)
    {
    }

    protected override Control BuildBubble(string message, string speechStyleClass, Color? fontColor = null)
    {

        var bubbleContent = new RichTextLabel
        {
            ModulateSelfOverride = Color.White.WithAlpha(ConfigManager.GetCVar(CCVars.SpeechBubbleTextOpacity)),
            MaxWidth = SpeechMaxWidth,
            StyleClasses = { "bubbleContent" },
        };

        bubbleContent.SetMessage(FormatSpeech(message, fontColor));

        //As for below: Some day this could probably be converted to xaml. But that is not today. -Myr
        var mainPanel = new PanelContainer
        {
            StyleClasses = { "speechBox", speechStyleClass },
            Children = { bubbleContent },
            ModulateSelfOverride = Color.White.WithAlpha(ConfigManager.GetCVar(CCVars.SpeechBubbleBackgroundOpacity)),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Bottom,
            Margin = new Thickness(4, 14, 4, 2)
        };

        var panel = new PanelContainer
        {
            Children = { mainPanel }
        };

        return panel;
    }
}
