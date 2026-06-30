using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Tutorial.UiHighlight;

public sealed class TutorialUiHighlightOverlay : Control
{
    private const float HighlightPadding = 6f;
    private const float BorderThickness = 2f;

    private static readonly Color DimColor = Color.Black.WithAlpha(0.45f);
    private static readonly Color HighlightColor = Color.FromHex("#D8A63A33");
    private static readonly Color BorderColor = Color.FromHex("#D8A63AFF");

    private Control _root = default!;
    private readonly List<TutorialUiHighlightSelector> _selectors = [];
    private readonly TutorialUiControlResolver _resolver = new(IoCManager.Resolve<IEntityManager>());

    public TutorialUiHighlightOverlay(Control root, IReadOnlyList<TutorialUiHighlightSelector> selectors)
    {
        MouseFilter = MouseFilterMode.Ignore;
        HorizontalAlignment = HAlignment.Stretch;
        VerticalAlignment = VAlignment.Stretch;

        SetTarget(root, selectors);
    }

    public void SetTarget(Control root, IReadOnlyList<TutorialUiHighlightSelector> selectors)
    {
        _root = root;

        _selectors.Clear();
        _selectors.AddRange(selectors);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (PixelSize.X <= 0 || PixelSize.Y <= 0)
            return;

        if (!TryGetTarget(out var target))
            return;

        if (!target.VisibleInTree || target.PixelSize.X <= 0 || target.PixelSize.Y <= 0)
            return;

        var origin = new Vector2(GlobalPixelPosition.X, GlobalPixelPosition.Y);
        var targetRect = ((UIBox2)target.GlobalPixelRect).Translated(-origin);
        var padding = HighlightPadding * UIScale;
        targetRect = new UIBox2(
            targetRect.Left - padding,
            targetRect.Top - padding,
            targetRect.Right + padding,
            targetRect.Bottom + padding);

        var fullRect = UIBox2.FromDimensions(0f, 0f, PixelSize.X, PixelSize.Y);
        var clampedRect = new UIBox2(
            Math.Clamp(targetRect.Left, fullRect.Left, fullRect.Right),
            Math.Clamp(targetRect.Top, fullRect.Top, fullRect.Bottom),
            Math.Clamp(targetRect.Right, fullRect.Left, fullRect.Right),
            Math.Clamp(targetRect.Bottom, fullRect.Top, fullRect.Bottom));

        if (clampedRect.Right <= clampedRect.Left || clampedRect.Bottom <= clampedRect.Top)
            return;

        DrawFilledRect(handle, new UIBox2(fullRect.Left, fullRect.Top, fullRect.Right, clampedRect.Top), DimColor);
        DrawFilledRect(handle, new UIBox2(fullRect.Left, clampedRect.Bottom, fullRect.Right, fullRect.Bottom), DimColor);
        DrawFilledRect(handle, new UIBox2(fullRect.Left, clampedRect.Top, clampedRect.Left, clampedRect.Bottom), DimColor);
        DrawFilledRect(handle, new UIBox2(clampedRect.Right, clampedRect.Top, fullRect.Right, clampedRect.Bottom), DimColor);

        DrawFilledRect(handle, clampedRect, HighlightColor);
        DrawBorder(handle, clampedRect);
    }

    private bool TryGetTarget([NotNullWhen(true)] out Control? target)
    {
        if (_selectors.Count == 0)
        {
            target = null;
            return false;
        }

        return _resolver.TryFind(_root, _selectors, out target);
    }

    private static void DrawBorder(DrawingHandleScreen handle, UIBox2 rect)
    {
        DrawFilledRect(handle, new UIBox2(rect.Left, rect.Top, rect.Right, rect.Top + BorderThickness), BorderColor);
        DrawFilledRect(handle, new UIBox2(rect.Left, rect.Bottom - BorderThickness, rect.Right, rect.Bottom), BorderColor);
        DrawFilledRect(handle, new UIBox2(rect.Left, rect.Top, rect.Left + BorderThickness, rect.Bottom), BorderColor);
        DrawFilledRect(handle, new UIBox2(rect.Right - BorderThickness, rect.Top, rect.Right, rect.Bottom), BorderColor);
    }

    private static void DrawFilledRect(DrawingHandleScreen handle, UIBox2 rect, Color color)
    {
        if (rect.Right <= rect.Left || rect.Bottom <= rect.Top)
            return;

        handle.DrawRect(rect, color);
    }

}
