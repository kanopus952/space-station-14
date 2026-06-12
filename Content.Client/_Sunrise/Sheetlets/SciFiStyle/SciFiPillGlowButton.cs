using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client._Sunrise.Sheetlets.SciFiStyle;

/// <summary>
///     Sci-fi shine sweep button variant clipped to a horizontal capsule shape.
/// </summary>
public sealed class SciFiPillGlowButton : SciFiGlowButton
{
    private const int CapsuleArcSegments = 8;
    private const int MaxClipVertices = 64;

    private readonly Vector2[] _surfaceVertices = new Vector2[4];
    private readonly Vector2[] _clipInputVertices = new Vector2[MaxClipVertices];
    private readonly Vector2[] _clipOutputVertices = new Vector2[MaxClipVertices];
    private readonly Vector2[] _capsuleVertices = new Vector2[MaxClipVertices];

    protected override void DrawSurfaceFacet(
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

        DrawCapsuleClippedFacet(handle, box, color);
    }

    private void DrawCapsuleClippedFacet(DrawingHandleScreen handle, UIBox2 box, Color color)
    {
        var clipCount = BuildCapsuleClipPolygon(box);
        if (clipCount < 3)
            return;

        for (var i = 0; i < _surfaceVertices.Length; i++)
            _clipInputVertices[i] = _surfaceVertices[i];

        var input = _clipInputVertices;
        var output = _clipOutputVertices;
        var inputCount = _surfaceVertices.Length;

        for (var i = 0; i < clipCount; i++)
        {
            if (inputCount == 0)
                return;

            var clipStart = _capsuleVertices[i];
            var clipEnd = _capsuleVertices[(i + 1) % clipCount];
            var outputCount = 0;

            var previous = input[inputCount - 1];
            var previousInside = IsInsideClipEdge(previous, clipStart, clipEnd);

            for (var j = 0; j < inputCount; j++)
            {
                var current = input[j];
                var currentInside = IsInsideClipEdge(current, clipStart, clipEnd);

                if (currentInside)
                {
                    if (!previousInside)
                    {
                        AddClipVertex(
                            output,
                            ref outputCount,
                            GetLineIntersection(previous, current, clipStart, clipEnd));
                    }

                    AddClipVertex(output, ref outputCount, current);
                }
                else if (previousInside)
                {
                    AddClipVertex(
                        output,
                        ref outputCount,
                        GetLineIntersection(previous, current, clipStart, clipEnd));
                }

                previous = current;
                previousInside = currentInside;
            }

            (input, output) = (output, input);
            inputCount = outputCount;
        }

        if (inputCount < 3)
            return;

        handle.DrawPrimitives(
            DrawPrimitiveTopology.TriangleFan,
            new ReadOnlySpan<Vector2>(input, 0, inputCount),
            color);
    }

    private int BuildCapsuleClipPolygon(UIBox2 box)
    {
        var radius = Math.Min(box.Width, box.Height) * 0.5f;
        if (radius <= 0f)
            return 0;

        var centerY = (box.Top + box.Bottom) * 0.5f;
        var leftCenterX = box.Left + radius;
        var rightCenterX = box.Right - radius;
        var count = 0;

        AddClipVertex(_capsuleVertices, ref count, new Vector2(leftCenterX, box.Top));
        AddClipVertex(_capsuleVertices, ref count, new Vector2(rightCenterX, box.Top));

        for (var i = 1; i <= CapsuleArcSegments; i++)
        {
            var angle = -MathF.PI * 0.5f + MathF.PI * i / CapsuleArcSegments;
            AddClipVertex(
                _capsuleVertices,
                ref count,
                new Vector2(
                    rightCenterX + MathF.Cos(angle) * radius,
                    centerY + MathF.Sin(angle) * radius));
        }

        AddClipVertex(_capsuleVertices, ref count, new Vector2(leftCenterX, box.Bottom));

        for (var i = 1; i <= CapsuleArcSegments; i++)
        {
            var angle = MathF.PI * 0.5f + MathF.PI * i / CapsuleArcSegments;
            AddClipVertex(
                _capsuleVertices,
                ref count,
                new Vector2(
                    leftCenterX + MathF.Cos(angle) * radius,
                    centerY + MathF.Sin(angle) * radius));
        }

        return count;
    }

    private static void AddClipVertex(Vector2[] vertices, ref int count, Vector2 vertex)
    {
        if (count >= vertices.Length)
            return;

        vertices[count++] = vertex;
    }

    private static bool IsInsideClipEdge(Vector2 point, Vector2 edgeStart, Vector2 edgeEnd)
    {
        return Cross(edgeEnd - edgeStart, point - edgeStart) >= -0.01f;
    }

    private static Vector2 GetLineIntersection(Vector2 lineStart, Vector2 lineEnd, Vector2 clipStart, Vector2 clipEnd)
    {
        var line = lineEnd - lineStart;
        var clip = clipEnd - clipStart;
        var denominator = Cross(line, clip);

        if (MathF.Abs(denominator) <= 0.0001f)
            return lineEnd;

        var amount = Cross(clipStart - lineStart, clip) / denominator;
        return lineStart + line * amount;
    }

    private static float Cross(Vector2 left, Vector2 right)
    {
        return left.X * right.Y - left.Y * right.X;
    }
}
