using System;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Shared._Sunrise.Tutorial.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TimeCounterComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan? EndTime;

    [DataField, AutoNetworkedField]
    public Vector2? ScreenPosition;

    [DataField, AutoNetworkedField]
    public int FontSize = 30;

    [DataField, AutoNetworkedField]
    public bool Centered = true;

    [DataField, AutoNetworkedField]
    public Color DefaultColor = Color.White;

    [DataField, AutoNetworkedField]
    public Color WarningColor = Color.Yellow;

    [DataField, AutoNetworkedField]
    public Color CriticalColor = Color.Red;

    [DataField, AutoNetworkedField]
    public Color BackgroundColor = Color.Transparent;

    [DataField, AutoNetworkedField]
    public Color BorderColor = Color.Transparent;

    [DataField, AutoNetworkedField]
    public TimeSpan WarningTime = TimeSpan.FromSeconds(120);

    [DataField, AutoNetworkedField]
    public TimeSpan CriticalTime = TimeSpan.FromSeconds(30);
}
