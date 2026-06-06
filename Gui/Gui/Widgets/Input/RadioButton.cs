using System;
using Gui.Rendering;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Widgets.Input;

public class RadioButton<T> : StatefulWidget where T : IEquatable<T>
{
    public RadioButton(
        T value,
        T groupValue,
        Action<T>? onChanged = null,
        string? label = null,
        float size = 24,
        RadioButtonStyle? style = null
    )
    {
        Value = value;
        GroupValue = groupValue;
        OnChanged = onChanged;
        Label = label;
        Size = size;
        Style = style;
    }

    public T Value { get; }
    public T GroupValue { get; }
    public Action<T>? OnChanged { get; }
    public string? Label { get; }
    public float Size { get; }

    /// <summary>
    ///     Optional style override. When <c>null</c>, uses
    ///     <see cref="ThemeData.RadioButtonStyle" />.
    /// </summary>
    public RadioButtonStyle? Style { get; }

    public override State CreateState() => new RadioButtonState<T>();
}

internal class RadioButtonState<T> : State<RadioButton<T>> where T : IEquatable<T>
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
        var style = Widget.Style ?? Theme.Of(context).RadioButtonStyle;
        var radius = Widget.Size / 2f;

        var isSelected = Widget.Value.Equals(Widget.GroupValue);
        var dot = new AnimatedScale(
            isSelected
                ? 1f
                : 0f,
            TimeSpan.FromMilliseconds(150),
            Curves.EaseOut,
            child: new Center(
                new Container(
                    new BoxStyle
                    {
                        Width = Widget.Size * 0.5f,
                        Height = Widget.Size * 0.5f,
                        Color = style.DotColor,
                        CornerRadius = new Vector4(radius)
                    }
                )
            )
        );

        var circle = new Container(
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
                CornerRadius = new Vector4(radius)
            },
            dot
        );

        var content = Widget.Label == null
            ? (Widget)circle
            : new Row(
                mainAxisAlignment: MainAxisAlignment.Start,
                crossAxisAlignment: CrossAxisAlignment.Center,
                children:
                [
                    circle,
                    new Padding(
                        EdgeInsets.Only(8),
                        new Text(Widget.Label, style.LabelStyle)
                    )
                ]
            );

        return new GestureDetector(
            onTap: e =>
            {
                SetState(() => { });
                if (!Widget.Value.Equals(Widget.GroupValue))
                {
                    Widget.OnChanged?.Invoke(Widget.Value);
                }
            },
            child: content
        );
    }

    public override void Dispose() => base.Dispose();
}
