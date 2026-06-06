using System;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Widgets.Input;

public class Checkbox : StatefulWidget
{
    public Checkbox(
        bool value,
        Action<bool>? onChanged = null,
        string? label = null,
        float size = 24,
        CheckboxStyle? style = null
    )
    {
        Value = value;
        OnChanged = onChanged;
        Label = label;
        Size = size;
        Style = style;
    }

    public bool Value { get; }
    public Action<bool>? OnChanged { get; }
    public string? Label { get; }
    public float Size { get; }

    /// <summary>
    ///     Optional style override. When <c>null</c>, uses
    ///     <see cref="ThemeData.CheckboxStyle" />.
    /// </summary>
    public CheckboxStyle? Style { get; }

    public override State CreateState() => new CheckboxState();
}

internal class CheckboxState : State<Checkbox>
{
    private SKBitmap? _backgroundTexture;

    public override void InitState()
    {
        base.InitState();
        _backgroundTexture = GuiModSystem.Instance?.SkiaAssetLoader?.LoadBitmap(
            "game",
            "textures/gui/gui-parchment.png"
        );
    }

    public override Widget Build(
        BuildContext context
    )
    {
        var theme = Theme.Of(context);
        var style = Widget.Style ?? theme.CheckboxStyle;

        var checkMark = new AnimatedOpacity(
            Widget.Value
                ? 1f
                : 0f,
            TimeSpan.FromMilliseconds(150),
            Curves.EaseOut,
            child: new AnimatedScale(
                Widget.Value
                    ? 1f
                    : 0f,
                TimeSpan.FromMilliseconds(150),
                Curves.EaseOut,
                child: new Center(
                    new Container(
                        new BoxStyle
                        {
                            Width = Widget.Size * 0.6f,
                            Height = Widget.Size * 0.6f,
                            Color = style.CheckColor,
                            CornerRadius = new Vector4(style.CornerRadius)
                        }
                    )
                )
            )
        );

        var box = new Container(
            new BoxStyle
            {
                Width = Widget.Size,
                Height = Widget.Size,
                Color = _backgroundTexture == null
                    ? style.BackgroundColor
                    : Vector4.One,
                Texture = _backgroundTexture,
                BorderThickness = style.BorderThickness,
                BorderColor = style.BorderColor,
                CornerRadius = new Vector4(style.CornerRadius)
            },
            checkMark
        );

        var content = Widget.Label == null
            ? (Widget)box
            : new Row(
                mainAxisAlignment: MainAxisAlignment.Start,
                crossAxisAlignment: CrossAxisAlignment.Center,
                children:
                [
                    box,
                    new SizedBox(8),
                    new Text(Widget.Label, style.LabelStyle)
                ]
            );

        return new GestureDetector(
            onTap: e => { Widget.OnChanged?.Invoke(!Widget.Value); },
            child: content
        );
    }

    public override void Dispose() => base.Dispose();
}
