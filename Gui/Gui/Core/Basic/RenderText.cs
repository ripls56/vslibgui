using System;
using System.Collections.Generic;
using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Rendering.Text;
using OpenTK.Mathematics;

namespace Gui.Core.Basic;

public class RenderText : RenderBox
{
    private readonly List<float> _lineWidths = [];
    private float _lastWrapMaxWidth = float.NaN;
    private TextStyle _lastWrapStyle;
    private string _lastWrapText = string.Empty;
    private float _lineHeight;
    private TextStyle _style = new();
    private string _text = string.Empty;
    private List<string> _visualLines = [];

    public string Text
    {
        get => _text;
        set => SetProperty(
            ref _text,
            value,
            relayout: true
        );
    }

    public TextStyle Style
    {
        get => _style;
        set => SetProperty(
            ref _style,
            value,
            relayout: true
        );
    }

    public override bool IsHitTestTarget => true;

    protected override void PerformLayout()
    {
        if (string.IsNullOrEmpty(_text))
        {
            Size = Vector2.Zero;
            _visualLines = [];
            return;
        }

        var font = TextLayoutHelper.GetFont(
            _style.FontFamily,
            _style.FontSize,
            _style.Weight
        );
        var metrics = font.Metrics;
        _lineHeight = metrics.Descent - metrics.Ascent + metrics.Leading;
        if (_lineHeight <= 0)
        {
            _lineHeight = _style.FontSize * 1.2f;
        }

        var maxWidth = Constraints.MaxWidth;
        var shouldWrap = _style.SoftWrap && !float.IsPositiveInfinity(maxWidth);

        var cacheValid = _text == _lastWrapText && _style.Equals(_lastWrapStyle) &&
                         (shouldWrap
                             ? Math.Abs(maxWidth - _lastWrapMaxWidth) < 0.5f
                             : float.IsNaN(_lastWrapMaxWidth));

        var originalEmbolden = font.Embolden;
        font.Embolden = _style.Boldness > 0;

        if (!cacheValid)
        {
            if (shouldWrap)
            {
                _visualLines = TextLayoutHelper.BreakIntoLines(
                    _text,
                    font,
                    maxWidth
                );
                _lastWrapMaxWidth = maxWidth;
            }
            else
            {
                _visualLines = new List<string>(_text.Split('\n'));
                _lastWrapMaxWidth = float.NaN;
            }

            // MaxLines truncation with optional ellipsis
            if (_style.MaxLines > 0 && _visualLines.Count > _style.MaxLines)
            {
                _visualLines.RemoveRange(
                    _style.MaxLines,
                    _visualLines.Count - _style.MaxLines
                );
                if (_style.Overflow == TextOverflow.Ellipsis && _visualLines.Count > 0)
                {
                    var last = _visualLines.Count - 1;
                    var lastLine = _visualLines[last];
                    var ellipsisW = font.MeasureText("...");
                    var targetW = maxWidth - ellipsisW;
                    if (targetW > 0)
                    {
                        long fits = font.BreakText(
                            lastLine.AsSpan(),
                            targetW,
                            out _
                        );
                        var count = (int)Math.Min(
                            fits,
                            lastLine.Length
                        );
                        _visualLines[last] = lastLine.Substring(
                                0,
                                count
                            )
                            .TrimEnd() + "...";
                    }
                    else
                    {
                        _visualLines[last] = "...";
                    }
                }
            }

            _lastWrapText = _text;
            _lastWrapStyle = _style;
        }

        _lineWidths.Clear();
        float maxLineWidth = 0;
        foreach (var line in _visualLines)
        {
            var w = font.MeasureText(line);
            _lineWidths.Add(w);
            maxLineWidth = Math.Max(
                maxLineWidth,
                w
            );
        }

        font.Embolden = originalEmbolden;

        Size = Constraints.Constrain(
            new Vector2(
                maxLineWidth,
                _visualLines.Count * _lineHeight
            )
        );
    }

    public override float GetMinIntrinsicWidth(
        float height
    )
    {
        if (string.IsNullOrEmpty(_text))
        {
            return 0f;
        }

        var font = TextLayoutHelper.GetFont(
            _style.FontFamily,
            _style.FontSize,
            _style.Weight
        );
        // Minimum width = longest word (cannot break further)
        float maxWordWidth = 0;
        foreach (var word in _text.Split(
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

        return maxWordWidth;
    }

    public override float GetMaxIntrinsicWidth(
        float height
    )
    {
        if (string.IsNullOrEmpty(_text))
        {
            return 0f;
        }

        return TextLayoutHelper.MeasureText(
                _text,
                _style.FontFamily,
                _style.FontSize,
                _style.Weight,
                _style.Boldness
            )
            .X;
    }

    public override float GetMinIntrinsicHeight(
        float width
    )
    {
        if (string.IsNullOrEmpty(_text))
        {
            return 0f;
        }

        var font = TextLayoutHelper.GetFont(
            _style.FontFamily,
            _style.FontSize,
            _style.Weight
        );
        var metrics = font.Metrics;
        var lh = metrics.Descent - metrics.Ascent + metrics.Leading;
        if (lh <= 0)
        {
            lh = _style.FontSize * 1.2f;
        }

        if (!_style.SoftWrap || float.IsPositiveInfinity(width) || width <= 0)
        {
            var hardLines = 1;
            foreach (var c in _text)
            {
                if (c == '\n')
                {
                    hardLines++;
                }
            }

            if (_style.MaxLines > 0)
            {
                hardLines = Math.Min(
                    hardLines,
                    _style.MaxLines
                );
            }

            return hardLines * lh;
        }

        var lines = TextLayoutHelper.BreakIntoLines(
            _text,
            font,
            width
        );
        var lineCount = lines.Count;
        if (_style.MaxLines > 0)
        {
            lineCount = Math.Min(
                lineCount,
                _style.MaxLines
            );
        }

        return lineCount * lh;
    }

    public override float GetMaxIntrinsicHeight(
        float width
    ) =>
        GetMinIntrinsicHeight(width);

    protected override void PaintInternal(
        PaintingContext context
    )
    {
        if (string.IsNullOrEmpty(_text) || _visualLines.Count == 0 || context.Canvas == null)
        {
            return;
        }

        var font = TextLayoutHelper.GetFont(
            _style.FontFamily,
            _style.FontSize,
            _style.Weight
        );
        var metrics = font.Metrics;
        var y = -metrics.Ascent;

        for (var i = 0; i < _visualLines.Count; i++)
        {
            float x = 0;
            if (_style.Align != TextAlignment.Left && i < _lineWidths.Count)
            {
                var lineWidth = _lineWidths[i];
                if (_style.Align == TextAlignment.Center)
                {
                    x = (Size.X - lineWidth) / 2f;
                }
                else if (_style.Align == TextAlignment.Right)
                {
                    x = Size.X - lineWidth;
                }
            }

            context.Canvas.DrawText(
                _visualLines[i],
                new Vector2(
                    x,
                    y
                ),
                _style.FontSize,
                _style.Color,
                _style.FontFamily,
                _style.Weight,
                _style.Boldness,
                _style.OutlineWidth,
                _style.OutlineColor,
                _style.GlowWidth,
                _style.GlowColor,
                context.SharedPaint,
                context.BlurFilterCache
            );
            y += _lineHeight;
        }
    }
}
