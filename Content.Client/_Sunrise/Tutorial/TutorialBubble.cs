using System.Numerics;
using Content.Shared.Speech;
using Content.Client._Sunrise.UserInterface.RichText;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Tutorial;

public abstract class TutorialBubble : Control
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] protected readonly IConfigurationManager ConfigManager = default!;
    private readonly SharedTransformSystem _transformSystem;

    /// <summary>
    ///     The distance in world space to offset the speech bubble from the center of the entity.
    ///     i.e. greater -> higher above the mob's head.
    /// </summary>
    private const float EntityVerticalOffset = 0.3f;

    /// <summary>
    ///     Allowed rich text tags for tutorial bubble content.
    /// </summary>
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

    private readonly EntityUid _senderEntity;

    public Vector2 ContentSize { get; private set; }


    public static TutorialBubble CreateTutorialBubble(string message, EntityUid senderEntity)
    {
        return new TutorialMainBubble(message, senderEntity, "sayBox");
    }

    public TutorialBubble(string message, EntityUid senderEntity, string speechStyleClass, Color? fontColor = null)
    {
        IoCManager.InjectDependencies(this);
        _senderEntity = senderEntity;
        _transformSystem = _entityManager.System<SharedTransformSystem>();

        var bubble = BuildBubble(message, speechStyleClass, fontColor);

        AddChild(bubble);

        ForceRunStyleUpdate();

        bubble.Measure(Vector2Helpers.Infinity);
        ContentSize = bubble.DesiredSize;
    }

    protected abstract Control BuildBubble(string message, string speechStyleClass, Color? fontColor = null);

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_entityManager.TryGetComponent<TransformComponent>(_senderEntity, out var xform)
            || ResolveMapId(xform) != _eyeManager.CurrentEye.Position.MapId)
        {
            Modulate = Color.White.WithAlpha(0);
            return;
        }

        Modulate = Color.White;

        var baseOffset = 0f;

        if (_entityManager.TryGetComponent<SpeechComponent>(_senderEntity, out var speech))
            baseOffset = speech.SpeechBubbleOffset;

        var offset = (-_eyeManager.CurrentEye.Rotation).ToWorldVec() * -(EntityVerticalOffset + baseOffset);
        var worldPos = _transformSystem.GetWorldPosition(xform) + offset;

        var lowerCenter = _eyeManager.WorldToScreen(worldPos) / UIScale;
        var screenPos = lowerCenter - new Vector2(ContentSize.X / 2, ContentSize.Y);
        // Round to nearest 0.5
        screenPos = (screenPos * 2).Rounded() / 2;
        LayoutContainer.SetPosition(this, screenPos);
    }

    private MapId? ResolveMapId(TransformComponent? xform)
    {
        var mapId = xform?.MapID;
        if (mapId != MapId.Nullspace)
            return mapId;

        var parent = xform?.ParentUid;
        while (parent != EntityUid.Invalid && _entityManager.TryGetComponent(parent, out xform))
        {
            mapId = xform.MapID;
            if (mapId != MapId.Nullspace)
                return mapId;

            parent = xform.ParentUid;
        }

        return MapId.Nullspace;
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
        var panel = new TutorialBubbleControl();
        panel.BubbleFrame.StyleClasses.Add(speechStyleClass);
        panel.BubbleText.SetMessage(FormatSpeech(message, fontColor), BubbleTags);

        return panel;
    }
}
