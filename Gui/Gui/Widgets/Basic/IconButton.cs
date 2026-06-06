using System;
using Gui.Widgets.Animations;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Widgets.Basic;

public class IconButton : StatefulWidget
{
    public IconButton(
        Icon icon,
        Vector4 color,
        float size = 40f,
        float cornerRadius = 20f,
        Action<PointerEvent>? onTap = null,
        Framework.Key? key = null
    ) : base(key)
    {
        Icon = icon;
        Size = size;
        CornerRadius = cornerRadius;
        Color = color;
        OnTap = onTap;
    }

    public Icon Icon { get; }
    public float Size { get; }
    public float CornerRadius { get; }
    public Vector4 Color { get; }
    public Action<PointerEvent>? OnTap { get; }

    public override State CreateState() => new IconButtonState();
}

public class IconButtonState : State<IconButton>
{
    private bool _isHovered;

    public override Widget Build(
        BuildContext context
    )
    {
        var displayColor = _isHovered
            ? Widget.Color + new Vector4(
                0.15f,
                0.15f,
                0.15f,
                0f
            )
            : Widget.Color;

        return new GestureDetector(
            onTap: Widget.OnTap,
            onEnter: _ => SetState(() => _isHovered = true),
            onExit: _ => SetState(() => _isHovered = false),
            child: new AnimatedContainer(
                duration: TimeSpan.FromMilliseconds(200),
                curve: Curves.EaseOut,
                style: new BoxStyle
                {
                    Color = displayColor,
                    Width = Widget.Size,
                    Height = Widget.Size,
                    CornerRadius = new Vector4(Widget.CornerRadius)
                },
                child: new Center(Widget.Icon)
            )
        );
    }
}
