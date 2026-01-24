using System.Numerics;
using Content.Client.Viewport;
using Content.Client._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Content.Client._Sunrise.TimeCounterContainer;
using Content.Client.UserInterface.Screens;
using Robust.Shared.Player;

namespace Content.Client._Sunrise.Tutorial;

public sealed class TimeCounterSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    private EntityQuery<TimeCounterUiComponent> _timeCounterUiQuery;
    /// <summary>
    /// Why? The scaling viewport is not initialized after OnScreenChanged.
    /// Soo we have to wait
    /// </summary>
    private bool _pendingRefresh;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TimeCounterComponent, AfterAutoHandleStateEvent>(OnTimeCounterState);
        SubscribeLocalEvent<TimeCounterComponent, ComponentInit>(OnTimeCounterInit);
        SubscribeLocalEvent<TimeCounterComponent, ComponentShutdown>(OnTimeCounterShutdown);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);

        _timeCounterUiQuery = GetEntityQuery<TimeCounterUiComponent>();
        _ui.OnScreenChanged += OnScreenChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _ui.OnScreenChanged -= OnScreenChanged;
    }

    private void OnScreenChanged((UIScreen? Old, UIScreen? New) ev)
    {
        if (ev.New is not InGameScreen)
        {
            RemoveAllCounters();
            _pendingRefresh = false;
            return;
        }

        _pendingRefresh = true;
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent ev)
    {
        _pendingRefresh = true;
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent ev)
    {
        RemoveAllCounters();
        _pendingRefresh = false;
    }

    private void OnTimeCounterState(Entity<TimeCounterComponent> ent, ref AfterAutoHandleStateEvent ev)
    {
        UpdateTimeCounter(ent);
    }

    private void OnTimeCounterInit(Entity<TimeCounterComponent> ent, ref ComponentInit ev)
    {
        UpdateTimeCounter(ent);
    }

    private void OnTimeCounterShutdown(Entity<TimeCounterComponent> ent, ref ComponentShutdown ev)
    {
        RemoveTimeCounter(ent.Owner);
    }

    private void UpdateTimeCounter(Entity<TimeCounterComponent> ent)
    {
        if (ent.Comp.EndTime == null || ent.Comp.EndTime == TimeSpan.Zero)
        {
            RemoveTimeCounter(ent.Owner);
            return;
        }

        if (_ui.ActiveScreen is not InGameScreen)
            return;

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
        var counterUi = EnsureComp<TimeCounterUiComponent>(ent.Owner);
        counterUi.Counter = counter;
    }

    private void RemoveTimeCounter(EntityUid uid)
    {
        if (!_timeCounterUiQuery.TryGetComponent(uid, out var counterUi) || counterUi.Counter == null)
            return;

        counterUi.Counter.Orphan();
        RemComp<TimeCounterUiComponent>(uid);
    }

    private void RefreshCounters()
    {
        var query = EntityQueryEnumerator<TimeCounterComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateTimeCounter((uid, comp));
        }
    }

    private void RemoveAllCounters()
    {
        var query = EntityQueryEnumerator<TimeCounterUiComponent>();
        while (query.MoveNext(out var uid, out var ui))
        {
            if (ui.Counter != null)
                ui.Counter.Orphan();
            RemCompDeferred<TimeCounterUiComponent>(uid);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_pendingRefresh)
            return;

        if (_ui.ActiveScreen is not InGameScreen)
            return;

        if (_eye.MainViewport is not ScalingViewport)
            return;

        _pendingRefresh = false;
        RefreshCounters();
    }
}
