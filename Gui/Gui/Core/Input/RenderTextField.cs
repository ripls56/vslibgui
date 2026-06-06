using System;
using Gui.Core.Layout;
using Gui.Rendering;
using Gui.Rendering.Text;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Core.Input;

/// <summary>
///     Render object that draws a text field: selection highlight, text content,
///     and a blinking cursor.
/// </summary>
internal class RenderTextField : RenderConstrainedBox
{
    public string Text
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    } = "";

    public int CursorPosition
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    }

    public int SelectionStart
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    }

    public int SelectionEnd
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    }

    public Vector4 SelectionColor
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    }

    public bool HasFocus
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    }

    public TextStyle TextStyle
    {
        get;
        set => SetProperty(ref field, value, true);
    } = new();

    public float ScrollOffset
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    }

    public bool CursorVisible
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    } = true;

    public override bool IsHitTestTarget => true;

    protected override void PaintInternal(
        PaintingContext context
    )
    {
        base.PaintInternal(context);
        if (context.Canvas == null)
        {
            return;
        }

        var font = TextLayoutHelper.GetFont(
            TextStyle.FontFamily,
            TextStyle.FontSize,
            TextStyle.Weight
        );
        font.Embolden = TextStyle.Boldness > 0;
        var metrics = font.Metrics;

        float horizontalPadding = 10;
        var clipRect = new SKRect(
            horizontalPadding,
            0,
            Size.X - horizontalPadding,
            Size.Y
        );

        context.Canvas.Save();
        context.Canvas.ClipRect(clipRect);
        context.Canvas.Translate(
            horizontalPadding - ScrollOffset,
            0
        );

        var textY = TextLayoutHelper.GetVerticalOffset(
            metrics,
            Size.Y
        );
        var lineTop = textY + metrics.Ascent;
        var lineHeight = metrics.Descent - metrics.Ascent;

        if (HasFocus && SelectionStart != SelectionEnd)
        {
            var clampedStart = Math.Clamp(
                SelectionStart,
                0,
                Text.Length
            );
            var clampedEnd = Math.Clamp(
                SelectionEnd,
                0,
                Text.Length
            );
            var selX1 = clampedStart > 0
                ? font.MeasureText(
                    Text.AsSpan(
                        0,
                        clampedStart
                    )
                )
                : 0;
            var selX2 = font.MeasureText(
                Text.AsSpan(
                    0,
                    clampedEnd
                )
            );
            context.DrawBox(
                new Vector2(
                    selX1,
                    lineTop
                ),
                new Vector2(
                    selX2 - selX1,
                    lineHeight
                ),
                SelectionColor,
                Vector4.Zero,
                0,
                Vector4.Zero
            );
        }

        context.DrawText(
            Text,
            new Vector2(
                0,
                textY
            ),
            TextStyle.FontSize,
            TextStyle.Color,
            TextStyle.FontFamily
        );

        var hasSelection = SelectionStart != SelectionEnd;
        var showCursor = HasFocus && (hasSelection || CursorVisible);
        if (showCursor)
        {
            var cursorX = font.MeasureText(
                Text.AsSpan(
                    0,
                    Math.Clamp(
                        CursorPosition,
                        0,
                        Text.Length
                    )
                )
            );
            context.DrawBox(
                new Vector2(
                    cursorX,
                    lineTop
                ),
                new Vector2(
                    2,
                    lineHeight
                ),
                Vector4.One,
                Vector4.Zero,
                0,
                Vector4.Zero
            );
        }

        context.Canvas.Restore();
    }
}
