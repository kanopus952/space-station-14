using System;
using System.Collections.Generic;
using Content.Client.UserInterface.Screens;
using Content.Client._Sunrise.Tutorial.UiHighlight;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Conditions;
using Content.Shared._Sunrise.Tutorial.EntitySystems;
using Content.Shared._Sunrise.Tutorial.Events;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Tutorial.Conditions;

public sealed class UiButtonPressedConditionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly SharedTutorialSystem _tutorial = default!;

    private readonly List<UiButtonPressedCondition> _activeConditions = [];
    private readonly Dictionary<UiButtonPressedCondition, ButtonSubscription> _subscriptions = [];

    private TutorialUiControlResolver _resolver = default!;
    private EntityUid? _activePlayer;
    private ProtoId<TutorialSequencePrototype>? _activeSequence;
    private int _activeStep = -1;
    private UIScreen? _activeScreen;

    public override void Initialize()
    {
        base.Initialize();

        _resolver = new TutorialUiControlResolver(EntityManager);

        _ui.OnScreenChanged += OnScreenChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _ui.OnScreenChanged -= OnScreenChanged;
        ClearState();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        RefreshButtonSubscriptions();
    }

    private void OnScreenChanged((UIScreen? Old, UIScreen? New) args)
    {
        ClearState();
        RefreshButtonSubscriptions();
    }

    private void RefreshButtonSubscriptions()
    {
        if (!TryGetTutorialContext(out var context))
        {
            ClearState();
            return;
        }

        if (IsContextChanged(context))
        {
            SetActiveContext(context);
        }

        UpdateConditionSubscriptions(context.Screen);
    }

    private bool TryGetTutorialContext(out TutorialButtonContext context)
    {
        context = default;

        if (_ui.ActiveScreen is not InGameScreen screen)
            return false;

        if (_player.LocalEntity is not { } player)
            return false;

        if (!TryComp(player, out TutorialPlayerComponent? tutorialPlayer))
            return false;

        if (!tutorialPlayer.TutorialInitialized)
            return false;

        if (!_tutorial.TryGetCurrentStep((player, tutorialPlayer), out var step))
            return false;

        context = new TutorialButtonContext(screen, player, tutorialPlayer, step);
        return true;
    }

    private bool IsContextChanged(TutorialButtonContext context)
    {
        if (_activeScreen != context.Screen)
            return true;

        if (_activePlayer != context.Player)
            return true;

        if (_activeStep != context.TutorialPlayer.StepIndex)
            return true;

        if (_activeSequence is not { } activeSequence)
            return true;

        return !activeSequence.Equals(context.TutorialPlayer.SequenceId);
    }

    private void SetActiveContext(TutorialButtonContext context)
    {
        ClearState();
        _activeScreen = context.Screen;
        _activePlayer = context.Player;
        _activeStep = context.TutorialPlayer.StepIndex;
        _activeSequence = context.TutorialPlayer.SequenceId;
        CollectButtonConditions(context.Step);
    }

    private void CollectButtonConditions(TutorialStepPrototype step)
    {
        CollectButtonConditions(step.Preconditions);
        CollectButtonConditions(step.Conditions);
        CollectButtonConditions(step.AnyConditions);
    }

    private void CollectButtonConditions(List<TutorialCondition> conditions)
    {
        for (var i = 0; i < conditions.Count; i++)
        {
            if (conditions[i] is UiButtonPressedCondition condition &&
                !string.IsNullOrWhiteSpace(condition.Button) &&
                condition.Selectors.Count > 0)
            {
                _activeConditions.Add(condition);
            }
        }
    }

    private void UpdateConditionSubscriptions(Control root)
    {
        for (var i = 0; i < _activeConditions.Count; i++)
        {
            var condition = _activeConditions[i];

            if (!_resolver.TryFind(root, condition.Selectors, out var control))
            {
                RemoveSubscription(condition);
                continue;
            }

            if (_subscriptions.TryGetValue(condition, out var subscription) &&
                subscription.Control == control)
            {
                continue;
            }

            RemoveSubscription(condition);
            AddSubscription(condition, control);
        }
    }

    private void AddSubscription(UiButtonPressedCondition condition, Control control)
    {
        void Handler(GUIBoundKeyEventArgs args)
        {
            OnControlKeyBindDown(condition.Button, args);
        }

        control.OnKeyBindDown += Handler;
        _subscriptions[condition] = new ButtonSubscription(control, Handler);
    }

    private void RemoveSubscription(UiButtonPressedCondition condition)
    {
        if (!_subscriptions.Remove(condition, out var subscription))
            return;

        subscription.Control.OnKeyBindDown -= subscription.Handler;
    }

    private void ClearState()
    {
        foreach (var subscription in _subscriptions.Values)
        {
            subscription.Control.OnKeyBindDown -= subscription.Handler;
        }

        _subscriptions.Clear();
        _activeConditions.Clear();
        _activePlayer = null;
        _activeSequence = null;
        _activeStep = -1;
        _activeScreen = null;
    }

    private void OnControlKeyBindDown(string button, GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        RaiseNetworkEvent(new TutorialUiButtonPressedEvent(button));
    }

    private sealed record ButtonSubscription(Control Control, Action<GUIBoundKeyEventArgs> Handler);

    private readonly record struct TutorialButtonContext(
        InGameScreen Screen,
        EntityUid Player,
        TutorialPlayerComponent TutorialPlayer,
        TutorialStepPrototype Step);
}
