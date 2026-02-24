using Content.Client.UserInterface.ControlExtensions;
using Content.Client.UserInterface.Screens;
using Content.Client._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.EntitySystems;
using Content.Shared._Sunrise.Tutorial.Events;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.AnnouncementSpeaker.Events;

namespace Content.Client._Sunrise.Tutorial;

public sealed class TutorialSystem : SharedTutorialSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    public event Action? WindowDataReceived;
    public bool CompletedTutorialsReceived { get; private set; }
    public HashSet<string> CompletedTutorials = new();
    private EntityQuery<TutorialBubbleUiComponent> _bubbleUiQuery;
    private LayoutContainer _speechBubbleRoot = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TutorialBubbleComponent, AfterAutoHandleStateEvent>(AfterAutoHandleState);
        SubscribeLocalEvent<TutorialBubbleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TutorialBubbleComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<TutorialPlayerComponent, AfterAutoHandleStateEvent>(OnTutorialPlayerState);
        SubscribeLocalEvent<TutorialBubbleComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<TutorialBubbleComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeNetworkEvent<TutorialWindowDataResponseEvent>(OnWindowDataResponse);
        SubscribeNetworkEvent<TutorialStartDeniedEvent>(OnStartDenied);

        _speechBubbleRoot = new LayoutContainer();
        _bubbleUiQuery = GetEntityQuery<TutorialBubbleUiComponent>();
        _ui.OnScreenChanged += OnScreenChanged;
    }
    private void OnScreenChanged((UIScreen? Old, UIScreen? New) ev)
    {
        if (ev.New is not InGameScreen)
        {
            _speechBubbleRoot.Orphan();
            return;
        }

        RefreshBubbles();
    }
    private void OnStartDenied(TutorialStartDeniedEvent msg, EntitySessionEventArgs args)
    {
        if (string.IsNullOrEmpty(msg.Reason))
            return;

        _ui.Popup(Loc.GetString(msg.Reason), null, false);
    }

    private void OnPlayerAttached(Entity<TutorialBubbleComponent> ent, ref LocalPlayerAttachedEvent ev)
    {
        RefreshBubbles();
    }

    private void OnPlayerDetached(Entity<TutorialBubbleComponent> ent, ref LocalPlayerDetachedEvent ev)
    {
        _speechBubbleRoot.Orphan();
    }

    private void OnTutorialPlayerState(Entity<TutorialPlayerComponent> ent, ref AfterAutoHandleStateEvent ev)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        RefreshBubbles();
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
    private void RefreshBubbles()
    {
        var query = EntityQueryEnumerator<TutorialBubbleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateBubble((uid, comp));
        }
    }
    private void RemoveBubble(EntityUid uid)
    {
        if (!_bubbleUiQuery.TryGetComponent(uid, out var bubbleUi) || bubbleUi.Bubble == null)
            return;

        bubbleUi.Bubble.DisposeAllChildren();
        RemComp<TutorialBubbleUiComponent>(uid);
    }
    private void UpdateBubble(Entity<TutorialBubbleComponent> ent)
    {
        if (string.IsNullOrEmpty(ent.Comp.Instruction))
        {
            RemoveBubble(ent.Owner);
            return;
        }

        if (_ui.ActiveScreen is not InGameScreen)
            return;

        var viewportContainer = _ui.ActiveScreen.FindControl<LayoutContainer>("ViewportContainer");

        if (_bubbleUiQuery.TryGetComponent(ent.Owner, out var uiComp) && uiComp.Bubble != null)
        {
            var labels = uiComp.Bubble.GetControlOfType<RichTextLabel>();

            foreach (var item in labels)
            {
                item.SetMessage(TutorialBubble.FormatSpeech(Loc.GetString(ent.Comp.Instruction)), TutorialBubble.BubbleTags, Color.White);
            }

            SetSpeechBubbleRoot(viewportContainer, uiComp.Bubble);
            return;
        }

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
        if (bubble.Parent != _speechBubbleRoot)
            _speechBubbleRoot.AddChild(bubble);
        root.AddChild(_speechBubbleRoot);
        LayoutContainer.SetAnchorPreset(_speechBubbleRoot, LayoutContainer.LayoutPreset.Wide);
        _speechBubbleRoot.SetPositionLast();
    }

    public void RequestQuitTutorial()
    {
        RaiseNetworkEvent(new TutorialQuitRequestEvent());
    }

    public void RequestWindowData()
    {
        RaiseNetworkEvent(new TutorialWindowDataRequestEvent());
    }

    public void RequestStartTutorial(ProtoId<TutorialSequencePrototype> sequenceId)
    {
        RaiseNetworkEvent(new TutorialStartRequestEvent(sequenceId));
    }

    private void OnWindowDataResponse(TutorialWindowDataResponseEvent msg, EntitySessionEventArgs args)
    {
        CompletedTutorials.Clear();
        CompletedTutorials.UnionWith(msg.CompletedTutorials);
        CompletedTutorialsReceived = true;
        WindowDataReceived?.Invoke();
    }
    public override void Shutdown()
    {
        base.Shutdown();
        _ui.OnScreenChanged -= OnScreenChanged;
    }
}
