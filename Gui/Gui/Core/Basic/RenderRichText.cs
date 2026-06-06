using System;
using System.Collections.Generic;
using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Vtml;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Spans;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Core.Basic;

/// <summary>
///     Render object for <see cref="RichText" />. Flattens an
///     <see cref="InlineSpan" /> tree into styled runs, performs
///     word-wrap layout with float support, paints text with
///     optional underline decoration, and supports hit-testing
///     for gesture recognizers on individual spans.
/// </summary>
public class RenderRichText : RenderBox
{
    private readonly List<FloatRect> _floatsLeft = [];
    private readonly List<FloatRect> _floatsRight = [];
    private readonly List<PlacedRun> _runs = [];
    private TextStyle _defaultStyle = new();
    private int _maxLines;
    private TextOverflow _overflow;
    private InlineSpan? _root;
    private bool _runsDirty = true;
    private List<VisualLine> _visualLines = [];

    /// <summary>
    ///     Maximum number of visual lines to display. 0 means unlimited.
    /// </summary>
    public int MaxLines
    {
        get => _maxLines;
        set => SetProperty(ref _maxLines, value, relayout: true);
    }

    /// <summary>
    ///     Overflow behavior when MaxLines is exceeded.
    /// </summary>
    public TextOverflow Overflow
    {
        get => _overflow;
        set => SetProperty(ref _overflow, value, relayout: true);
    }

    /// <summary>Root inline span tree.</summary>
    public InlineSpan? Root
    {
        get => _root;
        set
        {
            _root = value;
            _runsDirty = true;
            MarkNeedsLayout();
        }
    }

    /// <summary>Default text style applied to spans without explicit style.</summary>
    public TextStyle DefaultStyle
    {
        get => _defaultStyle;
        set
        {
            _defaultStyle = value;
            _runsDirty = true;
            MarkNeedsLayout();
        }
    }


    private void FlattenRuns()
    {
        _runs.Clear();
        _root?.CollectRuns(
            _defaultStyle,
            _runs
        );

        var widgetIdx = 0;
        for (var i = 0; i < _runs.Count; i++)
        {
            if (_runs[i].Type == PlacedRunType.Widget)
            {
                var r = _runs[i];
                r.WidgetChildIndex = widgetIdx++;
                _runs[i] = r;
            }
        }

        _runsDirty = false;
    }


    /// <inheritdoc />
    protected override void PerformLayout()
    {
        if (_runsDirty)
        {
            FlattenRuns();
        }

        if (_runs.Count == 0)
        {
            Size = Vector2.Zero;
            _visualLines = [];
            return;
        }

        var maxWidth = Constraints.MaxWidth;
        var shouldWrap = !float.IsPositiveInfinity(maxWidth);

        _visualLines = shouldWrap
            ? WrapRuns(maxWidth)
            : BuildSingleLine();

        // MaxLines truncation
        if (_maxLines > 0 && _visualLines.Count > _maxLines)
        {
            _visualLines.RemoveRange(
                _maxLines,
                _visualLines.Count - _maxLines
            );
            if (_overflow == TextOverflow.Ellipsis && _visualLines.Count > 0)
            {
                AppendEllipsis(_visualLines[_visualLines.Count - 1]);
            }
        }

        float totalHeight = 0;
        float maxLineWidth = 0;
        foreach (var line in _visualLines)
        {
            totalHeight += line.Height;
            maxLineWidth = Math.Max(
                maxLineWidth,
                line.Width
            );
        }

        foreach (var f in _floatsLeft)
        {
            totalHeight = Math.Max(
                totalHeight,
                f.Bottom
            );
        }

        foreach (var f in _floatsRight)
        {
            totalHeight = Math.Max(
                totalHeight,
                f.Bottom
            );
        }

        Size = Constraints.Constrain(
            new Vector2(
                maxLineWidth,
                totalHeight
            )
        );
    }

    private void AppendEllipsis(
        VisualLine lastLine
    )
    {
        for (var fi = lastLine.Fragments.Count - 1; fi >= 0; fi--)
        {
            var frag = lastLine.Fragments[fi];
            if (frag.RunIndex < _runs.Count
                && _runs[frag.RunIndex].Type == PlacedRunType.Text
                && frag.Text != null)
            {
                var style = _runs[frag.RunIndex].Style;
                var font = TextLayoutHelper.GetFont(
                    style.FontFamily,
                    style.FontSize,
                    style.Weight
                );
                var ellipsisW = font.MeasureText("...");
                var targetW = frag.Width - ellipsisW;
                if (targetW > 0)
                {
                    long fits = font.BreakText(
                        frag.Text.AsSpan(),
                        targetW,
                        out _
                    );
                    var count = (int)Math.Min(
                        fits,
                        frag.Text.Length
                    );
                    frag.Text = frag.Text.Substring(
                            0,
                            count
                        )
                        .TrimEnd() + "...";
                    frag.Width = font.MeasureText(frag.Text);
                }
                else
                {
                    frag.Text = "...";
                    frag.Width = ellipsisW;
                }

                lastLine.Fragments[fi] = frag;
                break;
            }
        }
    }

    private List<VisualLine> BuildSingleLine()
    {
        LayoutWidgetChildren();

        var fragments = new List<VisualFragment>();
        float totalWidth = 0;
        float maxHeight = 0;

        for (var i = 0; i < _runs.Count; i++)
        {
            var run = _runs[i];

            if (run.Type == PlacedRunType.Clear)
            {
                continue;
            }

            if (run.Type == PlacedRunType.Widget)
            {
                var childSize = GetWidgetChildSize(run.WidgetChildIndex);
                fragments.Add(
                    new VisualFragment
                    {
                        RunIndex = i,
                        Text = "",
                        Width = childSize.X,
                        X = totalWidth,
                        Y = 0,
                        Height = childSize.Y
                    }
                );
                totalWidth += childSize.X;
                maxHeight = Math.Max(
                    maxHeight,
                    childSize.Y
                );
                continue;
            }

            var font = TextLayoutHelper.GetFont(
                run.Style.FontFamily,
                run.Style.FontSize,
                run.Style.Weight
            );
            var w = font.MeasureText(run.Text);
            var h = GetLineHeight(
                font,
                run.Style.FontSize
            );
            fragments.Add(
                new VisualFragment
                {
                    RunIndex = i,
                    Text = run.Text,
                    Width = w,
                    X = totalWidth,
                    Y = 0,
                    Height = h
                }
            );
            totalWidth += w;
            maxHeight = Math.Max(
                maxHeight,
                h
            );
        }

        // Store bounds + position widget children
        foreach (var frag in fragments)
        {
            var r = _runs[frag.RunIndex];
            r.X = frag.X;
            r.Y = 0;
            r.Width = frag.Width;
            r.Height = maxHeight;
            _runs[frag.RunIndex] = r;

            if (r.Type == PlacedRunType.Widget)
            {
                PositionWidgetChild(
                    r.WidgetChildIndex,
                    frag.X,
                    0,
                    maxHeight,
                    frag.Height
                );
            }
        }

        return
        [
            new VisualLine { Fragments = fragments, Width = totalWidth, Height = maxHeight, Y = 0 }
        ];
    }

    private List<VisualLine> WrapRuns(
        float maxWidth
    )
    {
        var lines = new List<VisualLine>();
        var currentFragments = new List<VisualFragment>();
        float currentWidth = 0;
        float currentHeight = 0;
        float currentY = 0;

        void FlushLine(
            float height = -1f
        )
        {
            var h = height >= 0f
                ? height
                : currentHeight;
            lines.Add(
                EmitLine(
                    currentFragments,
                    currentWidth,
                    h,
                    currentY
                )
            );
            currentFragments.Clear();
            currentY += h;
            currentWidth = 0;
            currentHeight = 0;
        }

        _floatsLeft.Clear();
        _floatsRight.Clear();
        LayoutWidgetChildren();

        for (var runIdx = 0; runIdx < _runs.Count; runIdx++)
        {
            var run = _runs[runIdx];

            if (run.Type == PlacedRunType.Clear)
            {
                if (currentFragments.Count > 0)
                {
                    FlushLine();
                }

                float clearY = 0;
                foreach (var f in _floatsLeft)
                {
                    clearY = Math.Max(
                        clearY,
                        f.Bottom
                    );
                }

                foreach (var f in _floatsRight)
                {
                    clearY = Math.Max(
                        clearY,
                        f.Bottom
                    );
                }

                if (clearY > currentY)
                {
                    currentY = clearY;
                }

                continue;
            }

            if (run.Type == PlacedRunType.Widget
                && (run.Float == VtmlFloat.Left || run.Float == VtmlFloat.Right))
            {
                if (currentFragments.Count > 0)
                {
                    FlushLine();
                }

                var childSize = GetWidgetChildSize(run.WidgetChildIndex);
                var floatRect = new FloatRect
                {
                    Width = childSize.X,
                    Height = childSize.Y,
                    Y = currentY,
                    Side = run.Float,
                    WidgetChildIndex = run.WidgetChildIndex
                };

                if (run.Float == VtmlFloat.Left)
                {
                    floatRect.X = GetLineStartX(
                        currentY,
                        0,
                        maxWidth
                    );
                    _floatsLeft.Add(floatRect);
                }
                else
                {
                    floatRect.X = maxWidth - childSize.X - GetRightFloatWidth(currentY);
                    _floatsRight.Add(floatRect);
                }

                if (run.WidgetChildIndex >= 0 && run.WidgetChildIndex < Children.Count)
                {
                    Children[run.WidgetChildIndex].X = floatRect.X;
                    Children[run.WidgetChildIndex].Y = floatRect.Y;
                }

                continue;
            }

            if (run.Type == PlacedRunType.Widget)
            {
                var childSize = GetWidgetChildSize(run.WidgetChildIndex);
                var widgetW = childSize.X;
                var widgetH = childSize.Y;

                var startX = GetLineStartX(
                    currentY,
                    Math.Max(
                        currentHeight,
                        widgetH
                    ),
                    maxWidth
                );
                var endX = GetLineEndX(
                    currentY,
                    Math.Max(
                        currentHeight,
                        widgetH
                    ),
                    maxWidth
                );
                if (currentFragments.Count == 0)
                {
                    currentWidth = startX;
                }

                var available = endX - currentWidth;

                if (currentWidth > startX && widgetW > available)
                {
                    FlushLine();
                    startX = GetLineStartX(
                        currentY,
                        widgetH,
                        maxWidth
                    );
                    currentWidth = startX;
                }

                currentFragments.Add(
                    new VisualFragment
                    {
                        RunIndex = runIdx,
                        Text = "",
                        Width = widgetW,
                        X = currentWidth,
                        Y = currentY,
                        Height = widgetH
                    }
                );
                currentWidth += widgetW;
                currentHeight = Math.Max(
                    currentHeight,
                    widgetH
                );
                continue;
            }

            var text = run.Text;
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            var font = TextLayoutHelper.GetFont(
                run.Style.FontFamily,
                run.Style.FontSize,
                run.Style.Weight
            );
            var lineH = GetLineHeight(
                font,
                run.Style.FontSize
            );

            var pos = 0;
            var nlIdx = text.IndexOf('\n');
            while (pos < text.Length)
            {
                if (nlIdx >= 0 && nlIdx < pos)
                {
                    nlIdx = text.IndexOf(
                        '\n',
                        pos
                    );
                }

                if (nlIdx == pos)
                {
                    FlushLine(
                        currentHeight > 0
                            ? currentHeight
                            : lineH
                    );
                    pos++;
                    continue;
                }

                // Limit segment to text before next newline
                var segEnd = nlIdx >= 0
                    ? nlIdx
                    : text.Length;

                var startX = GetLineStartX(
                    currentY,
                    Math.Max(
                        currentHeight,
                        lineH
                    ),
                    maxWidth
                );
                var endX = GetLineEndX(
                    currentY,
                    Math.Max(
                        currentHeight,
                        lineH
                    ),
                    maxWidth
                );
                if (currentFragments.Count == 0)
                {
                    currentWidth = startX;
                }

                var available = endX - currentWidth;

                var remainingSpan = text.AsSpan(
                    pos,
                    segEnd - pos
                );
                var remainingWidth = font.MeasureText(remainingSpan);

                if (remainingWidth <= available
                    || (available >= endX - startX - 0.1f
                        && remainingWidth <= endX - startX))
                {
                    currentFragments.Add(
                        new VisualFragment
                        {
                            RunIndex = runIdx,
                            Text = remainingSpan.ToString(),
                            Width = remainingWidth,
                            X = currentWidth,
                            Y = currentY,
                            Height = lineH
                        }
                    );
                    currentWidth += remainingWidth;
                    currentHeight = Math.Max(
                        currentHeight,
                        lineH
                    );
                    pos = segEnd;
                    if (pos >= text.Length)
                    {
                        break;
                    }

                    continue;
                }

                if (available < 1f)
                {
                    if (currentFragments.Count > 0)
                    {
                        FlushLine();
                        continue;
                    }

                    var ch = text.Substring(
                        pos,
                        1
                    );
                    var chW = font.MeasureText(ch);
                    currentFragments.Add(
                        new VisualFragment
                        {
                            RunIndex = runIdx,
                            Text = ch,
                            Width = chW,
                            X = startX,
                            Y = currentY,
                            Height = lineH
                        }
                    );
                    lines.Add(
                        EmitLine(
                            currentFragments,
                            startX + chW,
                            lineH,
                            currentY
                        )
                    );
                    currentFragments.Clear();
                    currentY += lineH;
                    currentWidth = 0;
                    currentHeight = 0;
                    pos++;
                    while (pos < text.Length && text[pos] == ' ')
                    {
                        pos++;
                    }

                    continue;
                }

                long charsUsed = font.BreakText(
                    remainingSpan,
                    available,
                    out _
                );
                if (charsUsed <= 0)
                {
                    charsUsed = 1;
                }

                var count = (int)Math.Min(
                    charsUsed,
                    remainingSpan.Length
                );

                if (count < remainingSpan.Length && count > 1)
                {
                    var lastSpace = remainingSpan.Slice(
                            0,
                            count
                        )
                        .LastIndexOf(' ');
                    if (lastSpace > 0)
                    {
                        count = lastSpace + 1;
                    }
                }

                var fragment = remainingSpan.Slice(
                        0,
                        count
                    )
                    .TrimEnd()
                    .ToString();
                var fragWidth = font.MeasureText(fragment);
                currentFragments.Add(
                    new VisualFragment
                    {
                        RunIndex = runIdx,
                        Text = fragment,
                        Width = fragWidth,
                        X = currentWidth,
                        Y = currentY,
                        Height = lineH
                    }
                );
                currentWidth += fragWidth;
                currentHeight = Math.Max(
                    currentHeight,
                    lineH
                );

                FlushLine();

                pos += count;
                while (pos < text.Length && text[pos] == ' ')
                {
                    pos++;
                }
            }

            if (pos >= text.Length && text.Length == 0)
            {
                currentHeight = Math.Max(
                    currentHeight,
                    lineH
                );
            }
        }

        if (currentFragments.Count > 0 || lines.Count == 0)
        {
            lines.Add(
                EmitLine(
                    currentFragments,
                    currentWidth,
                    currentHeight > 0
                        ? currentHeight
                        : GetDefaultLineHeight(),
                    currentY
                )
            );
        }

        StoreBounds(lines);
        PositionInlineWidgetChildren(lines);

        return lines;
    }


    private void LayoutWidgetChildren()
    {
        foreach (var child in Children)
        {
            child.Layout(
                LayoutConstraints.Loose(
                    Constraints.MaxWidth,
                    Constraints.MaxHeight
                )
            );
        }
    }

    private Vector2 GetWidgetChildSize(
        int widgetChildIndex
    )
    {
        if (widgetChildIndex >= 0 && widgetChildIndex < Children.Count)
        {
            return Children[widgetChildIndex].Size;
        }

        return Vector2.Zero;
    }

    private void PositionWidgetChild(
        int childIndex,
        float x,
        float lineY,
        float lineHeight,
        float childHeight
    )
    {
        if (childIndex < 0 || childIndex >= Children.Count)
        {
            return;
        }

        var child = Children[childIndex];

        var ws = FindWidgetSpanForChild(childIndex);
        if (ws?.Alignment == PlaceholderAlignment.Middle)
        {
            child.Y = lineY + (lineHeight - childHeight) / 2f;
        }
        else
        {
            child.Y = lineY + lineHeight - childHeight;
        }

        child.X = x;
    }

    private void PositionInlineWidgetChildren(
        List<VisualLine> lines
    )
    {
        foreach (var line in lines)
        foreach (var frag in line.Fragments)
        {
            if (frag.RunIndex >= _runs.Count)
            {
                continue;
            }

            var run = _runs[frag.RunIndex];
            if (run.Type != PlacedRunType.Widget)
            {
                continue;
            }

            if (run.Float == VtmlFloat.Left || run.Float == VtmlFloat.Right)
            {
                continue;
            }

            PositionWidgetChild(
                run.WidgetChildIndex,
                frag.X,
                line.Y,
                line.Height,
                frag.Height
            );
        }
    }

    private WidgetSpan? FindWidgetSpanForChild(
        int childIndex
    )
    {
        foreach (var run in _runs)
        {
            if (run.Type == PlacedRunType.Widget
                && run.WidgetChildIndex == childIndex)
            {
                return run.Source as WidgetSpan;
            }
        }

        return null;
    }


    private float GetLineStartX(
        float y,
        float lineH,
        float maxWidth
    )
    {
        float x = 0;
        foreach (var f in _floatsLeft)
        {
            if (f.Y < y + lineH && f.Bottom > y)
            {
                x = Math.Max(
                    x,
                    f.Right
                );
            }
        }

        return x;
    }

    private float GetLineEndX(
        float y,
        float lineH,
        float maxWidth
    )
    {
        var end = maxWidth;
        foreach (var f in _floatsRight)
        {
            if (f.Y < y + lineH && f.Bottom > y)
            {
                end = Math.Min(
                    end,
                    f.X
                );
            }
        }

        return end;
    }

    private float GetRightFloatWidth(
        float y
    )
    {
        float w = 0;
        foreach (var f in _floatsRight)
        {
            if (f.Y <= y && f.Bottom > y)
            {
                w += f.Width;
            }
        }

        return w;
    }


    private VisualLine EmitLine(
        List<VisualFragment> fragments,
        float width,
        float height,
        float y
    ) =>
        new() { Fragments = [..fragments], Width = width, Height = height, Y = y };

    private void StoreBounds(
        List<VisualLine> lines
    )
    {
        for (var i = 0; i < _runs.Count; i++)
        {
            var r = _runs[i];
            r.X = float.MaxValue;
            r.Y = float.MaxValue;
            r.Width = 0;
            r.Height = 0;
            _runs[i] = r;
        }

        foreach (var line in lines)
        foreach (var frag in line.Fragments)
        {
            var r = _runs[frag.RunIndex];
            var fx = frag.X;
            var fy = line.Y;
            if (r.X == float.MaxValue)
            {
                r.X = fx;
                r.Y = fy;
                r.Width = frag.Width;
                r.Height = line.Height;
            }
            else
            {
                var right = Math.Max(
                    r.X + r.Width,
                    fx + frag.Width
                );
                var bottom = Math.Max(
                    r.Y + r.Height,
                    fy + line.Height
                );
                r.X = Math.Min(
                    r.X,
                    fx
                );
                r.Y = Math.Min(
                    r.Y,
                    fy
                );
                r.Width = right - r.X;
                r.Height = bottom - r.Y;
            }

            _runs[frag.RunIndex] = r;
        }
    }


    /// <summary>
    ///     Hit-tests a local point against placed runs and returns the
    ///     source <see cref="InlineSpan" /> at that position, or null.
    /// </summary>
    public InlineSpan? HitTestRun(
        float localX,
        float localY
    )
    {
        foreach (var line in _visualLines)
        {
            if (localY < line.Y || localY > line.Y + line.Height)
            {
                continue;
            }

            foreach (var frag in line.Fragments)
            {
                if (localX >= frag.X && localX <= frag.X + frag.Width)
                {
                    return _runs[frag.RunIndex].Source;
                }
            }
        }

        return null;
    }


    /// <inheritdoc />
    public override float GetMinIntrinsicWidth(
        float height
    )
    {
        if (_runsDirty)
        {
            FlattenRuns();
        }

        if (_runs.Count == 0)
        {
            return 0f;
        }

        float maxWordWidth = 0;
        foreach (var run in _runs)
        {
            if (run.Type == PlacedRunType.Widget)
            {
                var idx = run.WidgetChildIndex;
                if (idx >= 0 && idx < Children.Count)
                {
                    maxWordWidth = Math.Max(
                        maxWordWidth,
                        Children[idx].GetMinIntrinsicWidth(height)
                    );
                }

                continue;
            }

            if (run.Type != PlacedRunType.Text)
            {
                continue;
            }

            var font = TextLayoutHelper.GetFont(
                run.Style.FontFamily,
                run.Style.FontSize,
                run.Style.Weight
            );
            foreach (var word in run.Text.Split(
                         ' ',
                         '\n'
                     ))
            {
                if (word.Length > 0)
                {
                    maxWordWidth = Math.Max(
                        maxWordWidth,
                        font.MeasureText(word)
                    );
                }
            }
        }

        return maxWordWidth;
    }

    /// <inheritdoc />
    public override float GetMaxIntrinsicWidth(
        float height
    )
    {
        if (_runsDirty)
        {
            FlattenRuns();
        }

        if (_runs.Count == 0)
        {
            return 0f;
        }

        float totalWidth = 0;
        foreach (var run in _runs)
        {
            if (run.Type == PlacedRunType.Widget)
            {
                var idx = run.WidgetChildIndex;
                if (idx >= 0 && idx < Children.Count)
                {
                    totalWidth += Children[idx].GetMaxIntrinsicWidth(height);
                }

                continue;
            }

            if (run.Type != PlacedRunType.Text)
            {
                continue;
            }

            var font = TextLayoutHelper.GetFont(
                run.Style.FontFamily,
                run.Style.FontSize,
                run.Style.Weight
            );
            totalWidth += font.MeasureText(run.Text);
        }

        return totalWidth;
    }

    /// <inheritdoc />
    public override float GetMinIntrinsicHeight(
        float width
    )
    {
        if (_runsDirty)
        {
            FlattenRuns();
        }

        if (_runs.Count == 0)
        {
            return 0f;
        }

        if (float.IsPositiveInfinity(width) || width <= 0)
        {
            var singleLine = BuildSingleLine();
            return singleLine.Count > 0
                ? singleLine[0].Height
                : 0f;
        }

        var lines = WrapRuns(width);
        float totalHeight = 0;
        foreach (var line in lines)
        {
            totalHeight += line.Height;
        }

        foreach (var f in _floatsLeft)
        {
            totalHeight = Math.Max(
                totalHeight,
                f.Bottom
            );
        }

        foreach (var f in _floatsRight)
        {
            totalHeight = Math.Max(
                totalHeight,
                f.Bottom
            );
        }

        return totalHeight;
    }

    /// <inheritdoc />
    public override float GetMaxIntrinsicHeight(
        float width
    ) =>
        GetMinIntrinsicHeight(width);


    private static float GetLineHeight(
        SKFont font,
        float fontSize
    )
    {
        var metrics = font.Metrics;
        var h = metrics.Descent - metrics.Ascent + metrics.Leading;
        return h > 0
            ? h
            : fontSize * 1.2f;
    }

    private float GetDefaultLineHeight()
    {
        foreach (var run in _runs)
        {
            if (run.Type != PlacedRunType.Text)
            {
                continue;
            }

            var font = TextLayoutHelper.GetFont(
                run.Style.FontFamily,
                run.Style.FontSize,
                run.Style.Weight
            );
            return GetLineHeight(
                font,
                run.Style.FontSize
            );
        }

        return 14 * 1.2f;
    }


    /// <inheritdoc />
    protected override void PaintInternal(
        PaintingContext context
    )
    {
        if (_runs.Count == 0 || _visualLines.Count == 0)
        {
            return;
        }

        if (context.Canvas == null)
        {
            return;
        }

        foreach (var line in _visualLines)
        foreach (var frag in line.Fragments)
        {
            if (frag.RunIndex >= _runs.Count)
            {
                continue;
            }

            var run = _runs[frag.RunIndex];

            // Widget children paint themselves via base RenderObject.Paint
            if (run.Type == PlacedRunType.Widget)
            {
                continue;
            }

            if (run.Type != PlacedRunType.Text)
            {
                continue;
            }

            var style = run.Style;
            var font = TextLayoutHelper.GetFont(
                style.FontFamily,
                style.FontSize,
                style.Weight
            );
            var y = line.Y - font.Metrics.Ascent;

            context.Canvas.DrawText(
                frag.Text,
                new Vector2(
                    frag.X,
                    y
                ),
                style.FontSize,
                style.Color,
                style.FontFamily,
                style.Weight,
                style.Boldness,
                style.OutlineWidth,
                style.OutlineColor,
                style.GlowWidth,
                style.GlowColor,
                context.SharedPaint,
                context.BlurFilterCache
            );

            if (style.Decoration == TextDecoration.Underline)
            {
                var paint = context.SharedPaint;
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 1f;
                paint.Color = new SKColor(
                    (byte)(style.Color.X * 255),
                    (byte)(style.Color.Y * 255),
                    (byte)(style.Color.Z * 255),
                    (byte)(style.Color.W * 255)
                );
                var underlineY = line.Y + line.Height - 1f;
                context.Canvas.DrawLine(
                    frag.X,
                    underlineY,
                    frag.X + frag.Width,
                    underlineY,
                    paint
                );
                paint.Style = SKPaintStyle.Fill;
                paint.StrokeWidth = 0;
            }
        }
    }


    internal struct VisualFragment
    {
        public int RunIndex;
        public string Text;
        public float Width;
        public float X;
        public float Y;
        public float Height;
    }

    internal struct VisualLine
    {
        public List<VisualFragment> Fragments;
        public float Width;
        public float Height;
        public float Y;
    }

    internal struct FloatRect
    {
        public float X, Y, Width, Height;
        public VtmlFloat Side;
        public int WidgetChildIndex;
        public float Bottom => Y + Height;
        public float Right => X + Width;
    }
}
