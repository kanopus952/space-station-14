using System.Numerics;
using Content.Client.Viewport;
using Content.Client._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Content.Client._Sunrise.TimeCounterContainer;

namespace Content.Client._Sunrise.Tutorial;

public sealed class TimeCounterSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    private EntityQuery<TimeCounterUiComponent> _timeCounterUiQuery;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TimeCounterComponent, AfterAutoHandleStateEvent>(OnTimeCounterState);
        SubscribeLocalEvent<TimeCounterComponent, ComponentInit>(OnTimeCounterInit);
        SubscribeLocalEvent<TimeCounterComponent, ComponentShutdown>(OnTimeCounterShutdown);

        _timeCounterUiQuery = GetEntityQuery<TimeCounterUiComponent>();
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
}
