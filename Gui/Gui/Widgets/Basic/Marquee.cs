using System;
using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Widgets.Basic;

/// <summary>
///     Displays a single line of text that scrolls horizontally when it does not fit the
///     available width. When the text fits, it is drawn statically. Useful for long item
///     names in a fixed-width slot or label.
/// </summary>
public class Marquee : StatefulWidget
{
    /// <summary>Creates a scrolling text label.</summary>
    /// <param name="text">The text to display.</param>
    /// <param name="style">Text style.</param>
    /// <param name="velocity">Scroll speed in pixels per second.</param>
    /// <param name="gap">Gap in pixels between the end of the text and its repeat.</param>
    public Marquee(
        string text,
        TextStyle style,
        float velocity = 30f,
        float gap = 40f,
        Framework.Key? key = null
    ) : base(key)
    {
        Text = text;
        Style = style;
        Velocity = velocity;
        Gap = gap;
    }

    /// <summary>The text to display.</summary>
    public string Text { get; }

    /// <summary>Text style.</summary>
    public TextStyle Style { get; }

    /// <summary>Scroll speed in pixels per second.</summary>
    public float Velocity { get; }

    /// <summary>Gap in pixels between the end of the text and its repeat.</summary>
    public float Gap { get; }

    /// <inheritdoc />
    public override State CreateState() => new MarqueeState();
}

internal class MarqueeState : State<Marquee>
{
    private float _offset;
    private bool _overflow;
    private float _textWidth;
    private Ticker _ticker = null!;

    public override void InitState()
    {
        base.InitState();
        _ticker = Element.Owner!.GetTickerProvider().CreateTicker(_onTick);
        _ticker.Start();
    }

    public override void Dispose()
    {
        _ticker.Dispose();
        base.Dispose();
    }

    private void _onTick(TimeSpan dt)
    {
        if (!_overflow)
        {
            return;
        }

        var loop = _textWidth + Widget.Gap;
        _offset += Widget.Velocity * (float)dt.TotalSeconds;
        if (_offset >= loop)
        {
            _offset -= loop;
        }

        SetState(() => { });
    }

    private void _onMeasured(float textWidth, float viewportWidth)
    {
        _textWidth = textWidth;
        _overflow = textWidth > viewportWidth;
        if (!_overflow)
        {
            _offset = 0f;
        }
    }

    public override Widget Build(BuildContext context)
    {
        return new MarqueeText(
            Widget.Text,
            Widget.Style,
            _overflow ? _offset : 0f,
            Widget.Gap,
            _onMeasured
        );
    }
}

internal sealed class MarqueeText : RenderObjectWidget
{
    public MarqueeText(
        string text,
        TextStyle style,
        float scrollOffset,
        float gap,
        Action<float, float> onMeasured
    )
    {
        Text = text;
        Style = style;
        ScrollOffset = scrollOffset;
        Gap = gap;
        OnMeasured = onMeasured;
    }

    public string Text { get; }
    public TextStyle Style { get; }
    public float ScrollOffset { get; }
    public float Gap { get; }
    public Action<float, float> OnMeasured { get; }

    /// <inheritdoc />
    public override RenderObject CreateRenderObject()
    {
        return new RenderMarquee(OnMeasured)
        {
            Text = Text, Style = Style, ScrollOffset = ScrollOffset, Gap = Gap
        };
    }

    /// <inheritdoc />
    public override void UpdateRenderObject(RenderObject renderObject)
    {
        var ro = (RenderMarquee)renderObject;
        ro.Text = Text;
        ro.Style = Style;
        ro.ScrollOffset = ScrollOffset;
        ro.Gap = Gap;
    }
}

internal sealed class RenderMarquee : RenderBox
{
    private readonly Action<float, float> _onMeasured;
    private float _gap;
    private float _scrollOffset;
    private TextStyle _style;
    private string _text = string.Empty;
    private float _textWidth;

    public RenderMarquee(Action<float, float> onMeasured)
    {
        _onMeasured = onMeasured;
    }

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value, relayout: true);
    }

    public TextStyle Style
    {
        get => _style;
        set => SetProperty(ref _style, value, relayout: true);
    }

    public float ScrollOffset
    {
        get => _scrollOffset;
        set => SetProperty(ref _scrollOffset, value, true);
    }

    public float Gap
    {
        get => _gap;
        set => SetProperty(ref _gap, value, true);
    }

    /// <inheritdoc />
    protected override void PerformLayout()
    {
        var font = TextLayoutHelper.GetFont(_style.FontFamily, _style.FontSize, _style.Weight);
        var metrics = font.Metrics;
        var lineHeight = metrics.Descent - metrics.Ascent + metrics.Leading;
        if (lineHeight <= 0)
        {
            lineHeight = _style.FontSize * 1.2f;
        }

        _textWidth = font.MeasureText(_text);

        var width = float.IsInfinity(Constraints.MaxWidth) ? _textWidth : Constraints.MaxWidth;
        Size = Constraints.Constrain(new Vector2(width, lineHeight));

        _onMeasured(_textWidth, Size.X);
    }

    /// <inheritdoc />
    protected override void PaintInternal(PaintingContext context)
    {
        if (context.Canvas == null || string.IsNullOrEmpty(_text))
        {
            return;
        }

        var font = TextLayoutHelper.GetFont(_style.FontFamily, _style.FontSize, _style.Weight);
        var baseline = -font.Metrics.Ascent;
        var overflow = _textWidth > Size.X;

        context.Canvas.Save();
        context.Canvas.ClipRect(new SKRect(0f, 0f, Size.X, Size.Y));

        if (!overflow)
        {
            _drawText(context, 0f, baseline);
        }
        else
        {
            var x = -_scrollOffset;
            _drawText(context, x, baseline);
            _drawText(context, x + _textWidth + _gap, baseline);
        }

        context.Canvas.Restore();
    }

    private void _drawText(PaintingContext context, float x, float baseline)
    {
        context.DrawText(
            _text,
            new Vector2(x, baseline),
            _style.FontSize,
            _style.Color,
            _style.FontFamily,
            _style.Weight,
            _style.Boldness,
            _style.OutlineWidth,
            _style.OutlineColor,
            _style.GlowWidth,
            _style.GlowColor
        );
    }
}
