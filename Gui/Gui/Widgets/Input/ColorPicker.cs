using System;
using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Rendering;
using Gui.Sound;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Widgets.Input;

public class ColorPicker : StatefulWidget
{
    public ColorPicker
    (
        Vector4 value,
        Action<Vector4>? onChanged = null,
        Action<Vector4>? onChangeEnd = null,
        float panelSize = 180f,
        float barWidth = 20f,
        bool disabled = false,
        Framework.Key? key = null
    ) : base(key)
    {
        Value = value;
        OnChanged = onChanged;
        OnChangeEnd = onChangeEnd;
        PanelSize = panelSize;
        BarWidth = barWidth;
        Disabled = disabled;
    }

    public Vector4 Value { get; }
    public Action<Vector4>? OnChanged { get; }
    public Action<Vector4>? OnChangeEnd { get; }
    public float PanelSize { get; }
    public float BarWidth { get; }
    public bool Disabled { get; }

    public override State CreateState() => new ColorPickerState();
}

internal sealed class ColorPickerState : State<ColorPicker>
{
    private bool _dragSquare, _dragHue, _dragAlpha;
    private float _h, _s, _v, _a;
    private bool _hoverSquare, _hoverHue, _hoverAlpha;
    private RenderObject? _squareRo, _hueRo, _alphaRo;

    public override void InitState()
    {
        base.InitState();
        var hsv = Widget.Value.ToHsv();
        _h = hsv.H;
        _s = hsv.S;
        _v = hsv.V;
        _a = Widget.Value.W;
    }

    public override void UpdateWidget(ColorPicker oldWidget)
    {
        base.UpdateWidget(oldWidget);
        if (_dragSquare || _dragHue || _dragAlpha)
        {
            return;
        }
        if (Widget.Value != oldWidget.Value)
        {
            var hsv = Widget.Value.ToHsv();
            _h = hsv.H;
            _s = hsv.S;
            _v = hsv.V;
            _a = Widget.Value.W;
        }
    }

    public override Widget Build(BuildContext context)
    {
        var panelSize = new Vector2(Widget.PanelSize, Widget.PanelSize);
        var barSize = new Vector2(Widget.BarWidth, Widget.PanelSize);

        Widget square = new SatValSquareWidget
        (
            _h, _s, _v, !Widget.Disabled && (_hoverSquare || _dragSquare),
            panelSize, ro => _squareRo = ro
        );

        Widget hue = new HueBarWidget
        (
            _h, !Widget.Disabled && (_hoverHue || _dragHue),
            barSize, ro => _hueRo = ro
        );

        Widget alpha = new AlphaBarWidget
        (
            ColorHsvExtensions.FromHsv(_h, _s, _v), _a,
            !Widget.Disabled && (_hoverAlpha || _dragAlpha),
            barSize, ro => _alphaRo = ro
        );

        Widget preview = new ColorPreviewWidget(CurrentColor(), new Vector2(32, Widget.PanelSize));

        if (!Widget.Disabled)
        {
            square = new GestureDetector
            (
                square,
                onPress: e =>
                {
                    _dragSquare = true;
                    this.Tick();
                    _UpdateSquare(e);
                },
                onMove: e =>
                {
                    if (_dragSquare)
                    {
                        _UpdateSquare(e);
                    }
                },
                onRelease: _ => _EndDrag(ref _dragSquare),
                onEnter: _ => SetState(() => _hoverSquare = true),
                onExit: _ => SetState(() => _hoverSquare = false)
            );
            hue = new GestureDetector
            (
                hue,
                onPress: e =>
                {
                    _dragHue = true;
                    this.Tick();
                    _UpdateHue(e);
                },
                onMove: e =>
                {
                    if (_dragHue)
                    {
                        _UpdateHue(e);
                    }
                },
                onRelease: _ => _EndDrag(ref _dragHue),
                onEnter: _ => SetState(() => _hoverHue = true),
                onExit: _ => SetState(() => _hoverHue = false)
            );
            alpha = new GestureDetector
            (
                alpha,
                onPress: e =>
                {
                    _dragAlpha = true;
                    this.Tick();
                    _UpdateAlpha(e);
                },
                onMove: e =>
                {
                    if (_dragAlpha)
                    {
                        _UpdateAlpha(e);
                    }
                },
                onRelease: _ => _EndDrag(ref _dragAlpha),
                onEnter: _ => SetState(() => _hoverAlpha = true),
                onExit: _ => SetState(() => _hoverAlpha = false)
            );
        }

        return new Row
        (
            12,
            mainAxisSize: MainAxisSize.Min,
            crossAxisAlignment: CrossAxisAlignment.Start,
            children:
            [
                new Opacity(Widget.Disabled ? 0.5f : 1.0f, square),
                new Opacity(Widget.Disabled ? 0.5f : 1.0f, hue),
                new Opacity(Widget.Disabled ? 0.5f : 1.0f, alpha),
                new Opacity(Widget.Disabled ? 0.5f : 1.0f, preview)
            ]
        );
    }

    private Vector4 CurrentColor() => ColorHsvExtensions.FromHsv(_h, _s, _v, _a);

    private void _UpdateSquare(PointerEvent e)
    {
        if (_squareRo == null)
        {
            return;
        }
        var local = _squareRo.GlobalToLocal(new Vector2(e.X, e.Y));
        SetState
        (() =>
            {
                _s = Math.Clamp(local.X / Widget.PanelSize, 0f, 1f);
                _v = Math.Clamp(1f - local.Y / Widget.PanelSize, 0f, 1f);
            }
        );
        Widget.OnChanged?.Invoke(CurrentColor());
    }

    private void _UpdateHue(PointerEvent e)
    {
        if (_hueRo == null)
        {
            return;
        }
        var local = _hueRo.GlobalToLocal(new Vector2(e.X, e.Y));
        SetState(() => _h = Math.Clamp(local.Y / Widget.PanelSize, 0f, 1f) * 360f);
        Widget.OnChanged?.Invoke(CurrentColor());
    }

    private void _UpdateAlpha(PointerEvent e)
    {
        if (_alphaRo == null)
        {
            return;
        }
        var local = _alphaRo.GlobalToLocal(new Vector2(e.X, e.Y));
        SetState(() => _a = Math.Clamp(1f - local.Y / Widget.PanelSize, 0f, 1f));
        Widget.OnChanged?.Invoke(CurrentColor());
    }

    private void _EndDrag(ref bool flag)
    {
        if (!flag)
        {
            return;
        }
        flag = false;
        Widget.OnChangeEnd?.Invoke(CurrentColor());
        SetState(() => { });
    }
}

internal sealed class SatValSquareWidget : RenderObjectWidget
{
    public SatValSquareWidget
    (
        float h,
        float s,
        float v,
        bool active,
        Vector2 size,
        Action<RenderObject> onMount
    )
    {
        H = h;
        S = s;
        V = v;
        Active = active;
        Size = size;
        OnMount = onMount;
    }

    public float H { get; }
    public float S { get; }
    public float V { get; }
    public bool Active { get; }
    public Vector2 Size { get; }
    public Action<RenderObject> OnMount { get; }

    public override RenderObject CreateRenderObject() => new RenderSatValSquare(OnMount)
    {
        H = H,
        S = S,
        V = V,
        Active = Active,
        SurfaceSize = Size
    };

    public override void UpdateRenderObject(RenderObject ro)
    {
        var r = (RenderSatValSquare)ro;
        r.H = H;
        r.S = S;
        r.V = V;
        r.Active = Active;
        r.SurfaceSize = Size;
    }
}

internal sealed class RenderSatValSquare : RenderBox
{
    private static SKShader? _satShader, _valShader;
    private readonly Action<RenderObject> _onMount;
    private bool _active;
    private float _h, _s, _v;
    private Vector2 _size;
    public RenderSatValSquare(Action<RenderObject> onMount) { _onMount = onMount; }

    public float H { get => _h; set => SetProperty(ref _h, value, true); }
    public float S { get => _s; set => SetProperty(ref _s, value, true); }
    public float V { get => _v; set => SetProperty(ref _v, value, true); }
    public bool Active { get => _active; set => SetProperty(ref _active, value, true); }
    public Vector2 SurfaceSize
    {
        get => _size;
        set => SetProperty(ref _size, value, relayout: true);
    }

    protected override void PerformLayout()
    {
        Size = _size;
        _onMount(this);
    }

    protected override void PaintInternal(PaintingContext context)
    {
        var canvas = context.Canvas;
        if (canvas == null || Size.X <= 0 || Size.Y <= 0)
        {
            return;
        }
        var paint = context.SharedPaint;
        paint.Reset();
        paint.IsAntialias = true;

        DrawPanel(canvas, paint);
        DrawHandle(canvas, paint);
    }

    private void DrawPanel(SKCanvas canvas, SKPaint paint)
    {
        paint.Color = ColorHsvExtensions.FromHsv(_h, 1f, 1f).ToSkColor();
        canvas.DrawRoundRect(0, 0, Size.X, Size.Y, 4, 4, paint);

        _satShader ??= SKShader.CreateLinearGradient
        (
            new SKPoint(0, 0), new SKPoint(Size.X, 0), new[]
            {
                SKColors.White,
                SKColors.Transparent
            }, null, SKShaderTileMode.Clamp
        );
        paint.Shader = _satShader;
        canvas.DrawRoundRect(0, 0, Size.X, Size.Y, 4, 4, paint);

        _valShader ??= SKShader.CreateLinearGradient
        (
            new SKPoint(0, 0), new SKPoint(0, Size.Y), new[]
            {
                SKColors.Transparent,
                SKColors.Black
            }, null, SKShaderTileMode.Clamp
        );
        paint.Shader = _valShader;
        canvas.DrawRoundRect(0, 0, Size.X, Size.Y, 4, 4, paint);
        paint.Shader = null;
    }

    private void DrawHandle(SKCanvas canvas, SKPaint paint)
    {
        var cx = _s * Size.X;
        var cy = (1f - _v) * Size.Y;
        var radius = _active ? 8f : 6f;

        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 2.5f;
        paint.Color = _v > 0.5f ? SKColors.Black.WithAlpha(100) : SKColors.White.WithAlpha(100);
        canvas.DrawCircle(cx, cy, radius + 1.25f, paint);

        paint.Color = SKColors.White;
        paint.StrokeWidth = 2f;
        canvas.DrawCircle(cx, cy, radius, paint);

        paint.Style = SKPaintStyle.Fill;
        paint.Color = ColorHsvExtensions.FromHsv(_h, _s, _v).ToSkColor();
        canvas.DrawCircle(cx, cy, radius - 1.5f, paint);
    }
}

internal sealed class HueBarWidget : RenderObjectWidget
{
    public HueBarWidget
    (
        float h,
        bool active,
        Vector2 size,
        Action<RenderObject> onMount
    )
    {
        H = h;
        Active = active;
        Size = size;
        OnMount = onMount;
    }

    public float H { get; }
    public bool Active { get; }
    public Vector2 Size { get; }
    public Action<RenderObject> OnMount { get; }

    public override RenderObject CreateRenderObject() => new RenderHueBar(OnMount)
    {
        H = H, Active = Active, SurfaceSize = Size
    };

    public override void UpdateRenderObject(RenderObject ro)
    {
        var r = (RenderHueBar)ro;
        r.H = H;
        r.Active = Active;
        r.SurfaceSize = Size;
    }
}

internal sealed class RenderHueBar : RenderBox
{
    private static readonly SKColor[] HueColors =
    {
        Hue(0),
        Hue(60),
        Hue(120),
        Hue(180),
        Hue(240),
        Hue(300),
        Hue(360)
    };
    private readonly Action<RenderObject> _onMount;
    private bool _active;
    private float _h;
    private SKShader? _hueShader;
    private Vector2 _size;
    public RenderHueBar(Action<RenderObject> onMount) { _onMount = onMount; }
    public float H { get => _h; set => SetProperty(ref _h, value, true); }
    public bool Active { get => _active; set => SetProperty(ref _active, value, true); }
    public Vector2 SurfaceSize
    {
        get => _size;
        set => SetProperty(ref _size, value, relayout: true);
    }
    private static SKColor Hue(float h) => ColorHsvExtensions.FromHsv(h, 1f, 1f).ToSkColor();

    protected override void PerformLayout()
    {
        Size = _size;
        _onMount(this);
    }

    protected override void PaintInternal(PaintingContext context)
    {
        var canvas = context.Canvas;
        if (canvas == null)
        {
            return;
        }
        var paint = context.SharedPaint;
        paint.Reset();
        paint.IsAntialias = true;

        DrawRainbow(canvas, paint);
        DrawHandle(canvas, paint);
    }

    private void DrawRainbow(SKCanvas canvas, SKPaint paint)
    {
        _hueShader ??= SKShader.CreateLinearGradient
            (new SKPoint(0, 0), new SKPoint(0, Size.Y), HueColors, null, SKShaderTileMode.Clamp);
        paint.Shader = _hueShader;
        canvas.DrawRoundRect(0, 0, Size.X, Size.Y, 4, 4, paint);
        paint.Shader = null;
    }

    private void DrawHandle(SKCanvas canvas, SKPaint paint)
    {
        var cy = _h / 360f * Size.Y;
        var handleHeight = _active ? 10f : 8f;

        paint.Color = SKColors.Black.WithAlpha(40);
        canvas.DrawRoundRect(-2, cy - handleHeight / 2 + 1, Size.X + 4, handleHeight, 2, 2, paint);

        paint.Color = SKColors.White;
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 2f;
        canvas.DrawRoundRect(-2, cy - handleHeight / 2, Size.X + 4, handleHeight, 2, 2, paint);
    }
}

internal sealed class AlphaBarWidget : RenderObjectWidget
{
    public AlphaBarWidget
    (
        Vector4 color,
        float a,
        bool active,
        Vector2 size,
        Action<RenderObject> onMount
    )
    {
        BarColor = color;
        A = a;
        Active = active;
        Size = size;
        OnMount = onMount;
    }

    public Vector4 BarColor { get; }
    public float A { get; }
    public bool Active { get; }
    public Vector2 Size { get; }
    public Action<RenderObject> OnMount { get; }

    public override RenderObject CreateRenderObject() => new RenderAlphaBarFixed(OnMount)
    {
        AlphaBarColor = BarColor, A = A, Active = Active, SurfaceSize = Size
    };

    public override void UpdateRenderObject(RenderObject ro)
    {
        var r = (RenderAlphaBarFixed)ro;
        r.AlphaBarColor = BarColor;
        r.A = A;
        r.Active = Active;
        r.SurfaceSize = Size;
    }
}

internal sealed class RenderAlphaBarFixed : RenderBox
{
    private const float CheckerSize = 8f;
    private static SKShader? _checkerShader;
    private readonly Action<RenderObject> _onMount;
    private float _a;
    private bool _active;
    private Vector4 _barColor;
    private Vector2 _size;
    public RenderAlphaBarFixed(Action<RenderObject> onMount) { _onMount = onMount; }
    public Vector4 AlphaBarColor
    {
        get => _barColor;
        set => SetProperty(ref _barColor, value, true);
    }
    public float A { get => _a; set => SetProperty(ref _a, value, true); }
    public bool Active { get => _active; set => SetProperty(ref _active, value, true); }
    public Vector2 SurfaceSize
    {
        get => _size;
        set => SetProperty(ref _size, value, relayout: true);
    }

    protected override void PerformLayout()
    {
        Size = _size;
        _onMount(this);
    }

    protected override void PaintInternal(PaintingContext context)
    {
        var canvas = context.Canvas;
        if (canvas == null)
        {
            return;
        }
        var paint = context.SharedPaint;
        paint.Reset();

        DrawBackground(canvas, paint);
        DrawHandle(canvas, paint);
    }

    private void DrawBackground(SKCanvas canvas, SKPaint paint)
    {
        canvas.Save();
        canvas.ClipRoundRect
            (new SKRoundRect(new SKRect(0, 0, Size.X, Size.Y), 4, 4), antialias: true);

        if (_checkerShader == null)
        {
            using var bmp = new SKBitmap((int)CheckerSize * 2, (int)CheckerSize * 2);
            using (var c = new SKCanvas(bmp))
            {
                var p = new SKPaint
                {
                    Color = new SKColor(220, 220, 220)
                };
                c.DrawRect(0, 0, CheckerSize, CheckerSize, p);
                c.DrawRect(CheckerSize, CheckerSize, CheckerSize, CheckerSize, p);
                p.Color = SKColors.White;
                c.DrawRect(CheckerSize, 0, CheckerSize, CheckerSize, p);
                c.DrawRect(0, CheckerSize, CheckerSize, CheckerSize, p);
            }
            _checkerShader = SKShader.CreateBitmap
                (bmp, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
        }
        paint.Shader = _checkerShader;
        canvas.DrawRect(0, 0, Size.X, Size.Y, paint);

        var opaque = _barColor.ToSkColor();
        using (var shader = SKShader.CreateLinearGradient
               (
                   new SKPoint(0, 0), new SKPoint(0, Size.Y), new[]
                   {
                       opaque,
                       SKColors.Transparent
                   }, null, SKShaderTileMode.Clamp
               ))
        {
            paint.Shader = shader;
            canvas.DrawRect(0, 0, Size.X, Size.Y, paint);
        }
        paint.Shader = null;
        canvas.Restore();
    }

    private void DrawHandle(SKCanvas canvas, SKPaint paint)
    {
        var cy = (1f - _a) * Size.Y;
        var handleHeight = _active ? 10f : 8f;

        paint.Color = SKColors.Black.WithAlpha(40);
        canvas.DrawRoundRect(-2, cy - handleHeight / 2 + 1, Size.X + 4, handleHeight, 2, 2, paint);

        paint.Color = SKColors.White;
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 2f;
        canvas.DrawRoundRect(-2, cy - handleHeight / 2, Size.X + 4, handleHeight, 2, 2, paint);
    }
}

internal sealed class ColorPreviewWidget : RenderObjectWidget
{
    public ColorPreviewWidget(Vector4 color, Vector2 size)
    {
        PreviewColor = color;
        Size = size;
    }

    public Vector4 PreviewColor { get; }
    public Vector2 Size { get; }

    public override RenderObject CreateRenderObject() => new RenderColorPreview
    {
        PreviewColor = PreviewColor, SurfaceSize = Size
    };

    public override void UpdateRenderObject(RenderObject ro)
    {
        var r = (RenderColorPreview)ro;
        r.PreviewColor = PreviewColor;
        r.SurfaceSize = Size;
    }
}

internal sealed class RenderColorPreview : RenderBox
{
    private static SKShader? _checkerShader;
    private Vector4 _previewColor;
    private Vector2 _size;
    public Vector4 PreviewColor
    {
        get => _previewColor;
        set => SetProperty(ref _previewColor, value, true);
    }
    public Vector2 SurfaceSize
    {
        get => _size;
        set => SetProperty(ref _size, value, relayout: true);
    }
    protected override void PerformLayout() => Size = _size;

    protected override void PaintInternal(PaintingContext context)
    {
        var canvas = context.Canvas;
        if (canvas == null)
        {
            return;
        }
        var paint = context.SharedPaint;
        paint.Reset();
        paint.IsAntialias = true;

        canvas.Save();
        canvas.ClipRoundRect
            (new SKRoundRect(new SKRect(0, 0, Size.X, Size.Y), 6, 6), antialias: true);

        if (_checkerShader == null)
        {
            using var bmp = new SKBitmap(16, 16);
            using (var c = new SKCanvas(bmp))
            {
                var p = new SKPaint
                {
                    Color = new SKColor(200, 200, 200)
                };
                c.DrawRect(0, 0, 8, 8, p);
                c.DrawRect(8, 8, 8, 8, p);
                p.Color = SKColors.White;
                c.DrawRect(8, 0, 8, 8, p);
                c.DrawRect(0, 8, 8, 8, p);
            }
            _checkerShader = SKShader.CreateBitmap
                (bmp, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
        }
        paint.Shader = _checkerShader;
        canvas.DrawRect(0, 0, Size.X, Size.Y, paint);
        paint.Shader = null;

        paint.Color = _previewColor.ToSkColor();
        canvas.DrawRect(0, 0, Size.X, Size.Y, paint);
        canvas.Restore();

        paint.Color = new SKColor(0, 0, 0, 40);
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 1f;
        canvas.DrawRoundRect(0, 0, Size.X, Size.Y, 6, 6, paint);
    }
}
