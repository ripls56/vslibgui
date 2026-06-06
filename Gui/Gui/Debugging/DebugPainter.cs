using System;
using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Rendering;
using OpenTK.Mathematics;

namespace Gui.Debugging;

public static class DebugPainter
{
    private static readonly Vector4 BoundsColor = new(
        0,
        1,
        1,
        0.4f
    ); // Cyan

    private static readonly Vector4 BoundaryColor = new(
        1,
        0,
        1,
        0.6f
    ); // Magenta

    private static readonly Vector4 DirtyLayoutColor = new(
        1,
        0.5f,
        0,
        0.8f
    ); // Orange

    private static readonly Vector4 DirtyPaintColor = new(
        1,
        0,
        0,
        0.8f
    ); // Red

    private static readonly Vector4 ViolationFill = new(
        1,
        0,
        0,
        0.25f
    ); // Red fill

    private static readonly Vector4 ViolationBorder = new(
        1,
        0,
        0,
        0.9f
    ); // Red border

    // Repaint overlay colors
    private static readonly Vector3 HitColor = new(
        0,
        1,
        0
    ); // Green for cache hit

    private static readonly Vector3 MissColor = new(
        1,
        0,
        0
    ); // Red for cache miss

    private static readonly Vector3 DirtyColor = new(
        1,
        0.55f,
        0
    ); // Orange for dirty regular node

    public static Vector2? LastPointerPos { get; set; }
    public static bool IsPointerOverInteractive { get; set; }

    public static void PaintDebugInfo(
        RenderObject ro,
        PaintingContext context,
        DebugSettings settings,
        double currentTime,
        bool isActiveWindow = false
    )
    {
        if (context.Canvas == null)
        {
            return;
        }

        var frameId = RenderObject.CurrentFrameId;

        // Draw pointer at root level, only for the active window to avoid duplicate cursors
        if (ro.Parent == null && isActiveWindow && LastPointerPos.HasValue)
        {
            var pointerColor = IsPointerOverInteractive
                ? new Vector4(
                    0,
                    1,
                    0,
                    1
                )
                : new Vector4(
                    1,
                    1,
                    0,
                    1
                );
            context.DrawBox(
                LastPointerPos.Value - new Vector2(
                    3,
                    3
                ),
                new Vector2(
                    6,
                    6
                ),
                Vector4.Zero,
                Vector4.Zero,
                2.0f,
                pointerColor
            );
        }

        if (settings.ShowBounds)
        {
            PaintBoundsOverlay(
                ro,
                context
            );
        }

        if (settings.ShowPaint || settings.ShowHeatMap)
        {
            PaintRepaintOverlay(
                ro,
                context,
                settings,
                currentTime,
                frameId
            );
        }

        if (settings.ShowViolations && ro.HasLayoutViolation)
        {
            context.DrawBox(
                Vector2.Zero,
                ro.Size,
                ViolationFill,
                Vector4.Zero,
                2.5f,
                ViolationBorder
            );
        }

        foreach (var child in ro.Children)
        {
            context.Canvas.Save();
            context.Canvas.Translate(
                child.X,
                child.Y
            );
            PaintDebugInfo(
                child,
                context,
                settings,
                currentTime,
                isActiveWindow
            );
            context.Canvas.Restore();
        }
    }

    private static void PaintBoundsOverlay(
        RenderObject ro,
        PaintingContext context
    )
    {
        var thickness = ro.IsRepaintBoundary
            ? 2.0f
            : 1.0f;
        var color = ro.IsRepaintBoundary
            ? BoundaryColor
            : BoundsColor;

        if (ro.NeedsLayout)
        {
            color = DirtyLayoutColor;
        }
        else if (ro.NeedsPaint)
        {
            color = DirtyPaintColor;
        }

        context.DrawBox(
            Vector2.Zero,
            ro.Size,
            Vector4.Zero,
            Vector4.Zero,
            thickness,
            color
        );
    }

    private static void PaintRepaintOverlay(
        RenderObject ro,
        PaintingContext context,
        DebugSettings settings,
        double currentTime,
        int frameId
    )
    {
        if (ro.IsRepaintBoundary)
        {
            PaintBoundaryOverlay(
                ro,
                context,
                settings,
                currentTime,
                frameId
            );
        }
        else
        {
            PaintRegularNodeOverlay(
                ro,
                context,
                settings,
                currentTime,
                frameId
            );
        }
    }

    private static void PaintBoundaryOverlay(
        RenderObject ro,
        PaintingContext context,
        DebugSettings settings,
        double currentTime,
        int frameId
    )
    {
        var record = ro.RepaintRecord;
        var state = GetBoundaryState(
            record,
            frameId
        );

        if (state == BoundaryRepaintState.Idle)
        {
            return;
        }

        var elapsed = currentTime - record.LastEventTimestampMs;
        var alpha = (float)Math.Max(
            0.0,
            1.0 - elapsed / settings.FlashDurationMs
        );
        if (alpha <= 0f)
        {
            return;
        }

        if (state == BoundaryRepaintState.CacheHit)
        {
            // GREEN: thin border + faint fill
            var fill = new Vector4(
                HitColor,
                0.15f * alpha
            );
            var border = new Vector4(
                HitColor,
                0.9f * alpha
            );
            context.DrawBox(
                Vector2.Zero,
                ro.Size,
                fill,
                Vector4.Zero,
                2.0f,
                border
            );
            context.DrawText(
                "HIT",
                new Vector2(
                    3,
                    10
                ),
                9f,
                new Vector4(
                    HitColor,
                    alpha
                ),
                "monospace"
            );
        }
        else // CacheMiss
        {
            // RED: thicker border + stronger fill
            var fill = new Vector4(
                MissColor,
                0.25f * alpha
            );
            var border = new Vector4(
                MissColor,
                0.9f * alpha
            );
            context.DrawBox(
                Vector2.Zero,
                ro.Size,
                fill,
                Vector4.Zero,
                3.0f,
                border
            );
            context.DrawText(
                $"MISS #{record.DirtyPaintCount}",
                new Vector2(
                    3,
                    10
                ),
                9f,
                new Vector4(
                    1f,
                    0.4f,
                    0.4f,
                    alpha
                ),
                "monospace"
            );
        }
    }

    private static void PaintRegularNodeOverlay(
        RenderObject ro,
        PaintingContext context,
        DebugSettings settings,
        double currentTime,
        int frameId
    )
    {
        var record = ro.RepaintRecord;

        // Heat map: persistent blue→red tint proportional to repaint frequency
        if (settings.ShowHeatMap && record.DirtyPaintCount > 0)
        {
            var heat = Math.Clamp(
                record.HotFrameCount / 30f,
                0f,
                1f
            );
            var heatFill = new Vector4(
                heat,
                0f,
                1f - heat,
                0.12f * heat
            );
            context.DrawBox(
                Vector2.Zero,
                ro.Size,
                heatFill,
                Vector4.Zero,
                0f,
                Vector4.Zero
            );
        }

        if (!settings.ShowPaint)
        {
            return;
        }

        if (!record.WasDirtyPaintedThisFrame(frameId))
        {
            return;
        }

        // ORANGE flash: this node was dirty-painted this frame
        var elapsed = currentTime - record.LastEventTimestampMs;
        var alpha = (float)Math.Max(
            0.0,
            1.0 - elapsed / settings.FlashDurationMs
        );
        if (alpha <= 0f)
        {
            return;
        }

        var fill = new Vector4(
            DirtyColor,
            0.2f * alpha
        );
        var border = new Vector4(
            DirtyColor,
            0.7f * alpha
        );
        context.DrawBox(
            Vector2.Zero,
            ro.Size,
            fill,
            Vector4.Zero,
            1.5f,
            border
        );
    }

    private static BoundaryRepaintState GetBoundaryState(
        in RepaintRecord record,
        int frameId
    )
    {
        if (record.WasCacheHitThisFrame(frameId))
        {
            return BoundaryRepaintState.CacheHit;
        }

        if (record.WasDirtyPaintedThisFrame(frameId))
        {
            return BoundaryRepaintState.CacheMiss;
        }

        return BoundaryRepaintState.Idle;
    }
}
