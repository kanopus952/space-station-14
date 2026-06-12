using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client._Sunrise.Sheetlets.SciFiStyle;

/// <summary>
///     Button with an optional one-shot sci-fi shine sweep.
/// </summary>
/// <remarks>
///     The normal <see cref="Button"/> style box is the only source of the button shape and background.
///     This control only draws the shine sweep overlay inside the button bounds. Intensity properties multiply
///     each other, while alpha properties are capped by <see cref="MaxAlpha"/>.
/// </remarks>
[Virtual]
public class SciFiGlowButton : Button
{
    private const float DefaultSurfaceInset = 2f;
    private const float DefaultShineSpeed = 1.45f;
    private const float DefaultShineSkew = 0.34f;
    private const float DefaultShineWidth = 0.16f;
    private const float DefaultMaxAlpha = 0.68f;

    private readonly Vector2[] _surfaceVertices = new Vector2[4];
    private float _surfaceInset = DefaultSurfaceInset;
    private float _shineSpeed = DefaultShineSpeed;
    private float _shineSkew = DefaultShineSkew;
    private float _shineWidth = DefaultShineWidth;
    private float _glowIntensity = 1f;
    private float _normalGlowIntensity;
    private float _hoverGlowIntensity = 0.85f;
    private float _pressedGlowIntensity = 0.95f;
    private float _activeGlowIntensity = 1.15f;
    private float _shineAlpha = 1.2f;
    private float _maxAlpha = DefaultMaxAlpha;
    private float _shineProgress;
    private bool _wasShining;

    /// <summary>
    ///     If true, a button with <see cref="ActiveStyleClass"/> uses <see cref="ActiveGlowIntensity"/> for shine
    ///     intensity even in normal draw mode.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool ActiveGlowEnabled { get; set; }

    /// <summary>
    ///     Enables the animated shine sweep that runs once when the button enters a glowing state or changes draw mode.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool ShineSweepEnabled { get; set; } = true;

    /// <summary>
    ///     Style class that marks the button as active for <see cref="ActiveGlowEnabled"/>.
    ///     Set to null to disable class-based active glow detection.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public string? ActiveStyleClass { get; set; } = SunriseStyleClass.StyleClassSciFiTabActive;

    /// <summary>
    ///     Pixel inset used for the shine sweep inside the button bounds.
    ///     Values below zero are clamped to zero.
    /// </summary>
    /// <remarks>
    ///     If the inset is at least half of the button width or height, the shine sweep is not drawn.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    public float SurfaceInset
    {
        get => _surfaceInset;
        set => _surfaceInset = Math.Max(0f, value);
    }

    /// <summary>
    ///     Shine sweep progress per second. Higher values make the sweep cross the button faster.
    ///     Values below zero are clamped to zero.
    /// </summary>
    /// <remarks>
    ///     A value of 1 completes the sweep in about one second; the default 1.45 completes it faster.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    public float ShineSpeed
    {
        get => _shineSpeed;
        set => _shineSpeed = Math.Max(0f, value);
    }

    /// <summary>
    ///     Horizontal skew of the shine facets relative to the button height.
    ///     Positive values lean the facets left at the bottom edge.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float ShineSkew
    {
        get => _shineSkew;
        set => _shineSkew = value;
    }

    /// <summary>
    ///     Normalized width of the shine sweep bands. Higher values make the highlight broader.
    ///     Values below zero are clamped to zero.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float ShineWidth
    {
        get => _shineWidth;
        set => _shineWidth = Math.Max(0f, value);
    }

    /// <summary>
    ///     Global multiplier applied to the selected normal, hover, pressed, or active shine intensity.
    ///     Values below zero are clamped to zero.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float GlowIntensity
    {
        get => _glowIntensity;
        set => _glowIntensity = Math.Max(0f, value);
    }

    /// <summary>
    ///     Shine intensity used while the button is in normal draw mode.
    ///     Values below zero are clamped to zero.
    /// </summary>
    /// <remarks>
    ///     The default is zero, so a non-active idle button does not show the shine sweep unless this value is set.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    public float NormalGlowIntensity
    {
        get => _normalGlowIntensity;
        set => _normalGlowIntensity = Math.Max(0f, value);
    }

    /// <summary>
    ///     Shine intensity used while the button is hovered.
    ///     Values below zero are clamped to zero.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float HoverGlowIntensity
    {
        get => _hoverGlowIntensity;
        set => _hoverGlowIntensity = Math.Max(0f, value);
    }

    /// <summary>
    ///     Shine intensity used while the button is pressed.
    ///     Values below zero are clamped to zero.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float PressedGlowIntensity
    {
        get => _pressedGlowIntensity;
        set => _pressedGlowIntensity = Math.Max(0f, value);
    }

    /// <summary>
    ///     Minimum shine intensity used when <see cref="ActiveGlowEnabled"/> is true and the button has
    ///     <see cref="ActiveStyleClass"/>.
    ///     Values below zero are clamped to zero.
    /// </summary>
    /// <remarks>
    ///     Active shine does not replace hover or pressed intensity; the control uses the larger of the active and
    ///     current draw-mode intensities.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    public float ActiveGlowIntensity
    {
        get => _activeGlowIntensity;
        set => _activeGlowIntensity = Math.Max(0f, value);
    }

    /// <summary>
    ///     Multiplier applied to all shine sweep alpha values.
    ///     Values below zero are clamped to zero.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float ShineAlpha
    {
        get => _shineAlpha;
        set => _shineAlpha = Math.Max(0f, value);
    }

    /// <summary>
    ///     Maximum alpha allowed for every shine primitive.
    ///     Values are clamped to the 0..1 range.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxAlpha
    {
        get => _maxAlpha;
        set => _maxAlpha = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    ///     Color of the dim outer bands of the shine sweep.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Color ShineOuterColor { get; set; } = SciFiPalette.AccentDim;

    /// <summary>
    ///     Color of the main shine sweep bands around the core highlight.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Color ShineColor { get; set; } = SciFiPalette.Accent;

    /// <summary>
    ///     Color of the brightest central band of the shine sweep.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Color ShineCoreColor { get; set; } = SciFiPalette.Text;

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (DrawMode == DrawModeEnum.Disabled ||
            !ShineSweepEnabled ||
            ShineWidth <= 0f ||
            GetShineIntensity() <= 0f)
        {
            _wasShining = false;
            return;
        }

        if (!_wasShining)
        {
            _shineProgress = 0f;
            _wasShining = true;
            return;
        }

        if (_shineProgress >= 1f)
            return;

        _shineProgress = Math.Min(_shineProgress + args.DeltaSeconds * ShineSpeed, 1f);
    }

    protected override void DrawModeChanged()
    {
        base.DrawModeChanged();
        ResetShineSweep();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (DrawMode == DrawModeEnum.Disabled ||
            !ShineSweepEnabled ||
            _shineProgress >= 1f ||
            ShineWidth <= 0f)
            return;

        var intensity = GetShineIntensity();
        if (intensity <= 0f)
            return;

        if (!TryGetShineBox(out var shineBox))
            return;

        DrawShineSweep(handle, shineBox, intensity);
    }

    /// <summary>
    ///     Resets the one-shot shine animation so the next glowing state starts from the beginning.
    /// </summary>
    protected virtual void ResetShineSweep()
    {
        _shineProgress = 0f;
        _wasShining = false;
    }

    /// <summary>
    ///     Returns true when the current style classes should activate <see cref="ActiveGlowIntensity"/>.
    /// </summary>
    protected virtual bool IsActiveGlowVisible()
    {
        return ActiveGlowEnabled &&
               ActiveStyleClass != null &&
               HasStyleClass(ActiveStyleClass);
    }

    /// <summary>
    ///     Computes the final shine intensity for the current draw mode before alpha clamping.
    /// </summary>
    protected virtual float GetShineIntensity()
    {
        if (DrawMode == DrawModeEnum.Disabled)
            return 0f;

        var intensity = DrawMode switch
        {
            DrawModeEnum.Pressed => PressedGlowIntensity,
            DrawModeEnum.Hover => HoverGlowIntensity,
            DrawModeEnum.Normal => NormalGlowIntensity,
            _ => 0f
        };

        if (IsActiveGlowVisible())
            intensity = Math.Max(intensity, ActiveGlowIntensity);

        return Math.Max(0f, intensity) * GlowIntensity;
    }

    /// <summary>
    ///     Computes the inset box used by the shine sweep without defining or drawing the button shape.
    /// </summary>
    protected virtual bool TryGetShineBox(out UIBox2 box)
    {
        var pixelBox = PixelSizeBox;
        if (pixelBox.Width <= SurfaceInset * 2f || pixelBox.Height <= SurfaceInset * 2f)
        {
            box = default;
            return false;
        }

        box = new UIBox2(
            pixelBox.Left + SurfaceInset,
            pixelBox.Top + SurfaceInset,
            pixelBox.Right - SurfaceInset,
            pixelBox.Bottom - SurfaceInset);
        return true;
    }

    /// <summary>
    ///     Draws the animated shine sweep bands for the current sweep progress.
    /// </summary>
    protected virtual void DrawShineSweep(DrawingHandleScreen handle, UIBox2 box, float intensity)
    {
        var progress = EaseOutCubic(_shineProgress);
        var center = Lerp(-0.58f, 1.58f, progress);

        DrawSurfaceFacet(
            handle,
            box,
            center - ShineWidth * 2.6f,
            center - ShineWidth * 1.05f,
            ShineSkew,
            ShineOuterColor.WithAlpha(ClampAlpha(0.08f * intensity * ShineAlpha)));

        DrawSurfaceFacet(
            handle,
            box,
            center - ShineWidth * 1.05f,
            center - ShineWidth * 0.25f,
            ShineSkew,
            ShineColor.WithAlpha(ClampAlpha(0.18f * intensity * ShineAlpha)));

        DrawSurfaceFacet(
            handle,
            box,
            center - ShineWidth * 0.25f,
            center + ShineWidth * 0.32f,
            ShineSkew,
            ShineCoreColor.WithAlpha(ClampAlpha(0.34f * intensity * ShineAlpha)));

        DrawSurfaceFacet(
            handle,
            box,
            center + ShineWidth * 0.32f,
            center + ShineWidth * 1.1f,
            ShineSkew,
            ShineColor.WithAlpha(ClampAlpha(0.16f * intensity * ShineAlpha)));

        DrawSurfaceFacet(
            handle,
            box,
            center + ShineWidth * 1.1f,
            center + ShineWidth * 2.7f,
            ShineSkew,
            ShineOuterColor.WithAlpha(ClampAlpha(0.07f * intensity * ShineAlpha)));
    }

    /// <summary>
    ///     Draws a skewed quadrilateral facet in normalized horizontal surface coordinates.
    /// </summary>
    protected virtual void DrawSurfaceFacet(
        DrawingHandleScreen handle,
        UIBox2 box,
        float leftNorm,
        float rightNorm,
        float skewNorm,
        Color color)
    {
        var left = Lerp(box.Left, box.Right, Clamp01(leftNorm));
        var right = Lerp(box.Left, box.Right, Clamp01(rightNorm));
        var skew = box.Height * skewNorm;

        _surfaceVertices[0] = new Vector2(left, box.Top);
        _surfaceVertices[1] = new Vector2(right, box.Top);
        _surfaceVertices[2] = new Vector2(ClampToBox(right - skew, box.Left, box.Right), box.Bottom);
        _surfaceVertices[3] = new Vector2(ClampToBox(left - skew, box.Left, box.Right), box.Bottom);

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, _surfaceVertices, color);
    }

    /// <summary>
    ///     Clamps a primitive alpha value by <see cref="MaxAlpha"/>.
    /// </summary>
    protected virtual float ClampAlpha(float alpha)
    {
        return Math.Clamp(alpha, 0f, MaxAlpha);
    }

    protected static float ClampToBox(float value, float min, float max)
    {
        return Math.Clamp(value, min, max);
    }

    protected static float Clamp01(float value)
    {
        return Math.Clamp(value, 0f, 1f);
    }

    protected static float EaseOutCubic(float value)
    {
        var inverse = 1f - Clamp01(value);
        return 1f - inverse * inverse * inverse;
    }

    protected static float Lerp(float from, float to, float amount)
    {
        return from + (to - from) * amount;
    }
}
