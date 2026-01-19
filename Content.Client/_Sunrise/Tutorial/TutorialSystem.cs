using System.Linq;
using System.Numerics;
using Content.Client.Chat.UI;
using Content.Client.UserInterface.ControlExtensions;
using Content.Client.Viewport;
using Content.Client._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.EntitySystems;
using Content.Shared._Sunrise.Tutorial.Events;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Timing;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Tutorial;

public sealed class TutorialSystem : SharedTutorialSystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    private EntityQuery<TutorialBubbleUiComponent> _bubbleUiQuery;
    private EntityQuery<TutorialTimeCounterUiComponent> _timeCounterUiQuery;
    private LayoutContainer _speechBubbleRoot = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TutorialBubbleComponent, AfterAutoHandleStateEvent>(AfterAutoHandleState);
        SubscribeLocalEvent<TutorialBubbleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TutorialBubbleComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<TutorialTimeCounterComponent, AfterAutoHandleStateEvent>(OnTimeCounterState);
        SubscribeLocalEvent<TutorialTimeCounterComponent, ComponentInit>(OnTimeCounterInit);
        SubscribeLocalEvent<TutorialTimeCounterComponent, ComponentShutdown>(OnTimeCounterShutdown);

        _speechBubbleRoot = new LayoutContainer();
        _bubbleUiQuery = GetEntityQuery<TutorialBubbleUiComponent>();
        _timeCounterUiQuery = GetEntityQuery<TutorialTimeCounterUiComponent>();
    }

    private void AfterAutoHandleState(Entity<TutorialBubbleComponent> ent, ref AfterAutoHandleStateEvent ev)
    {
        UpdateBubble(ent);
    }
    private void OnComponentInit(Entity<TutorialBubbleComponent> ent, ref ComponentInit ev)
    {
        UpdateBubble(ent);
    }
    private void OnComponentShutdown(Entity<TutorialBubbleComponent> ent, ref ComponentShutdown ev)
    {
        RemoveBubble(ent);
    }
    private void UpdateBubble(Entity<TutorialBubbleComponent> ent)
    {
        if (string.IsNullOrEmpty(ent.Comp.Instruction))
        {
            RemoveBubble(ent.Owner);
            return;
        }

        if (_bubbleUiQuery.TryGetComponent(ent.Owner, out var uiComp) && uiComp.Bubble != null)
        {
            var labels = uiComp.Bubble.GetControlOfType<RichTextLabel>();

            foreach (var item in labels)
            {
                item.SetMessage(TutorialBubble.FormatSpeech(Loc.GetString(ent.Comp.Instruction)));
            }

            return;
        }
        var viewportContainer = _ui.ActiveScreen!.FindControl<LayoutContainer>("ViewportContainer");

        var bubble = TutorialBubble.CreateTutorialBubble(
            Loc.GetString(ent.Comp.Instruction),
            ent);

        SetSpeechBubbleRoot(viewportContainer, bubble);

        var bubbleUi = EnsureComp<TutorialBubbleUiComponent>(ent.Owner);
        bubbleUi.Bubble = bubble;
    }

    public void SetSpeechBubbleRoot(LayoutContainer root, TutorialBubble bubble)
    {
        _speechBubbleRoot.Orphan();
        _speechBubbleRoot.AddChild(bubble);
        root.AddChild(_speechBubbleRoot);
        LayoutContainer.SetAnchorPreset(_speechBubbleRoot, LayoutContainer.LayoutPreset.Wide);
        _speechBubbleRoot.SetPositionLast();
    }
    private void RemoveBubble(EntityUid uid)
    {
        if (!_bubbleUiQuery.TryGetComponent(uid, out var bubbleUi) || bubbleUi.Bubble == null)
            return;

        bubbleUi.Bubble.DisposeAllChildren();
        RemComp<TutorialBubbleUiComponent>(uid);
    }

    private void OnTimeCounterState(Entity<TutorialTimeCounterComponent> ent, ref AfterAutoHandleStateEvent ev)
    {
        UpdateTimeCounter(ent);
    }

    private void OnTimeCounterInit(Entity<TutorialTimeCounterComponent> ent, ref ComponentInit ev)
    {
        UpdateTimeCounter(ent);
    }

    private void OnTimeCounterShutdown(Entity<TutorialTimeCounterComponent> ent, ref ComponentShutdown ev)
    {
        RemoveTimeCounter(ent.Owner);
    }

    private void UpdateTimeCounter(Entity<TutorialTimeCounterComponent> ent)
    {
        if (ent.Comp.EndTime == null)
        {
            RemoveTimeCounter(ent.Owner);
            return;
        }

        if (_eye.MainViewport is not ScalingViewport vp)
            return;

        var screenSize = vp.SizeBox;

        var position = ent.Comp.ScreenPosition ?? new Vector2(screenSize.Center.X, 1);

        var style = new TimeCounterStyle
        {
            FontSize = ent.Comp.FontSize,
            BackgroundColor = ent.Comp.BackgroundColor,
            BorderColor = ent.Comp.BorderColor,
            Centered = ent.Comp.Centered,
            DefaultColor = ent.Comp.DefaultColor,
            WarningColor = ent.Comp.WarningColor,
            CriticalColor = ent.Comp.CriticalColor,
            WarningTime = ent.Comp.WarningTime,
            CriticalTime = ent.Comp.CriticalTime
        };

        if (_timeCounterUiQuery.TryGetComponent(ent.Owner, out var timeUi) && timeUi.Counter != null)
            timeUi.Counter.Orphan();

        var counter = new TimeCounter(ent.Comp.EndTime, style, position);
        _ui.PopupRoot.AddChild(counter);
        var counterUi = EnsureComp<TutorialTimeCounterUiComponent>(ent.Owner);
        counterUi.Counter = counter;
    }

    private void RemoveTimeCounter(EntityUid uid)
    {
        if (!_timeCounterUiQuery.TryGetComponent(uid, out var counterUi) || counterUi.Counter == null)
            return;

        counterUi.Counter.Orphan();
        RemComp<TutorialTimeCounterUiComponent>(uid);
    }

    public void RequestQuitTutorial()
    {
        RaiseNetworkEvent(new TutorialQuitRequestEvent());
    }
}
