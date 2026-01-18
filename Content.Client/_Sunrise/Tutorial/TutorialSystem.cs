using System.Linq;
using System.Numerics;
using Content.Client.Chat.UI;
using Content.Client.UserInterface.ControlExtensions;
using Content.Client.Viewport;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.EntitySystems;
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
    private readonly Dictionary<EntityUid, TutorialBubble> _activeTutorialBubbles = new();
    private readonly Dictionary<EntityUid, TimeCounter> _activeTimeCounters = new();
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

        if (_activeTutorialBubbles.TryGetValue(ent.Owner, out var existing))
        {
            var labels = existing.GetControlOfType<RichTextLabel>();

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

        _activeTutorialBubbles[ent.Owner] = bubble;
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
        if (!_activeTutorialBubbles.Remove(uid, out var bubble))
            return;

        bubble.DisposeAllChildren();
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

        if (_activeTimeCounters.Remove(ent.Owner, out var existing))
        {
            existing.Orphan();
            existing.Dispose();
        }

        var counter = new TimeCounter(ent.Comp.EndTime, style, position);
        _ui.PopupRoot.AddChild(counter);
        _activeTimeCounters[ent.Owner] = counter;
    }

    private void RemoveTimeCounter(EntityUid uid)
    {
        if (!_activeTimeCounters.Remove(uid, out var counter))
            return;

        counter.Orphan();
        counter.Dispose();
    }
}
