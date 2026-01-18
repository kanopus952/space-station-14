using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Tutorial;

public sealed class TimeCounter : PanelContainer
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    private readonly TimeSpan? _endTime;
    private readonly char[] _textBuffer = new char[16];
    private readonly Label _label;
    private readonly TimeCounterStyle _style;

    public TimeCounter(TimeSpan? endTime, TimeCounterStyle? style = null, Vector2? position = null)
    {
        IoCManager.InjectDependencies(this);
        _endTime = endTime;
        _style = style ?? new TimeCounterStyle();

        var cache = IoCManager.Resolve<IResourceCache>();
        var font = cache.GetResource<FontResource>(_style.FontPath);
        var fontOverride = new VectorFont(font, _style.FontSize);

        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = _style.BackgroundColor,
            BorderColor = _style.BorderColor,
            BorderThickness = _style.BorderThickness,
            Padding = _style.Padding
        };

        _label = new Label
        {
            Align = _style.Centered ? Label.AlignMode.Center : Label.AlignMode.Left,
            VAlign = _style.Centered ? Label.VAlignMode.Center : Label.VAlignMode.Top,
            FontOverride = fontOverride,
            FontColorOverride = _style.DefaultColor,
            FontColorShadowOverride = _style.ShadowColor,
            ShadowOffsetXOverride = _style.ShadowOffsetX,
            ShadowOffsetYOverride = _style.ShadowOffsetY
        };

        AddChild(_label);

        if (position != null)
            LayoutContainer.SetPosition(this, position.Value);
    }

    public void SetPosition(Vector2 position)
    {
        LayoutContainer.SetPosition(this, position);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        if (!VisibleInTree)
        {
            return;
        }

        var time = _endTime - _gameTiming.CurTime;

        if (time is null)
            return;

        var remaining = time.Value;
        if (remaining < TimeSpan.Zero)
            remaining = TimeSpan.Zero;

        if (remaining <= _style.CriticalTime)
            _label.FontColorOverride = _style.CriticalColor;
        else if (remaining <= _style.WarningTime)
            _label.FontColorOverride = _style.WarningColor;
        else
            _label.FontColorOverride = _style.DefaultColor;

        _label.TextMemory = FormatHelpers.FormatIntoMem(_textBuffer, $"{remaining:mm\\:ss}");
    }
}

public sealed class TimeCounterStyle
{
    public Color DefaultColor { get; init; } = Color.White;
    public Color WarningColor { get; init; } = Color.Yellow;
    public Color CriticalColor { get; init; } = Color.Red;
    public Color BackgroundColor { get; init; } = Color.FromHex("#1B1B1E").WithAlpha(0.8f);
    public Color BorderColor { get; init; } = Color.Black.WithAlpha(0.9f);
    public Thickness BorderThickness { get; init; } = new(1);
    public Thickness Padding { get; init; } = new(6, 2);
    public int FontSize { get; init; } = 14;
    public string FontPath { get; init; } = "/Fonts/NotoSansDisplay/NotoSansDisplay-Bold.ttf";
    public Color ShadowColor { get; init; } = Color.Black.WithAlpha(0.85f);
    public int ShadowOffsetX { get; init; } = 1;
    public int ShadowOffsetY { get; init; } = 1;
    public bool Centered { get; init; } = true;
    public TimeSpan WarningTime { get; init; } = TimeSpan.FromSeconds(120);
    public TimeSpan CriticalTime { get; init; } = TimeSpan.FromSeconds(30);
}
