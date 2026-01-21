using Content.Client.UserInterface.ControlExtensions;
using Content.Client._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.EntitySystems;
using Content.Shared._Sunrise.Tutorial.Events;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Tutorial;

public sealed class TutorialSystem : SharedTutorialSystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    public event Action? WindowDataReceived;
    public bool CompletedTutorialsReceived { get; private set; }
    public IReadOnlyCollection<string> CompletedTutorials => _completedTutorials;
    private EntityQuery<TutorialBubbleUiComponent> _bubbleUiQuery;
    private LayoutContainer _speechBubbleRoot = default!;
    private readonly HashSet<string> _completedTutorials = new();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TutorialBubbleComponent, AfterAutoHandleStateEvent>(AfterAutoHandleState);
        SubscribeLocalEvent<TutorialBubbleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TutorialBubbleComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeNetworkEvent<TutorialWindowDataResponseEvent>(OnWindowDataResponse);

        _speechBubbleRoot = new LayoutContainer();
        _bubbleUiQuery = GetEntityQuery<TutorialBubbleUiComponent>();
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
        _completedTutorials.Clear();
        _completedTutorials.UnionWith(msg.CompletedTutorials);
        CompletedTutorialsReceived = true;
        WindowDataReceived?.Invoke();
    }
}
