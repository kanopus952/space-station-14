using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Graphics;
using Robust.Shared.Maths;

namespace Content.Client._Sunrise.Tutorial;

public sealed class TutorialBubbleTail : Control
{
    public const string StylePropertyFillColor = "FillColor";
    public const string StylePropertyBorderColor = "BorderColor";
    public const string StylePropertyBorderThickness = "BorderThickness";

    private Color _fillColor = Color.White;
    private Color _borderColor = Color.White;
    private float _borderThickness = 2f;

    protected override void StylePropertiesChanged()
    {
        base.StylePropertiesChanged();

        if (TryGetStyleProperty(StylePropertyFillColor, out Color fill))
            _fillColor = fill;

        if (TryGetStyleProperty(StylePropertyBorderColor, out Color border))
            _borderColor = border;

        if (TryGetStyleProperty(StylePropertyBorderThickness, out float thickness))
            _borderThickness = thickness;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var width = PixelSize.X;
        var height = PixelSize.Y;
        if (width <= 0 || height <= 0)
            return;

        var border = MathHelper.Clamp(_borderThickness, 0f, MathF.Min(width, height) / 2f);

        var borderVerts = new Vector2[3];
        borderVerts[0] = new Vector2(0f, 0f);
        borderVerts[1] = new Vector2(width, 0f);
        borderVerts[2] = new Vector2(width / 2f, height);

        if (border <= 0f)
        {
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, borderVerts.AsSpan(), _fillColor);
            return;
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, borderVerts.AsSpan(), _borderColor);

        var innerTop = border;
        var innerLeft = border;
        var innerRight = MathF.Max(innerLeft, width - border);
        var innerBottom = MathF.Max(innerTop, height - border);

        var innerVerts = new Vector2[3];
        innerVerts[0] = new Vector2(innerLeft, innerTop);
        innerVerts[1] = new Vector2(innerRight, innerTop);
        innerVerts[2] = new Vector2(width / 2f, innerBottom);

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, innerVerts.AsSpan(), _fillColor);
    }
}
