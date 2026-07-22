using System;
using System.Collections.Generic;
using Gui.Core.Layout;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Widgets.Basic;

/// <summary>
///     Three dots that pulse in sequence — a compact indeterminate loading indicator.
///     Each dot fades in and out on a staggered phase so a wave of brightness travels
///     left to right, looping forever while mounted.
/// </summary>
public class LoadingDots : StatefulWidget
{
    /// <summary>Creates a loading-dots indicator.</summary>
    /// <param name="color">Dot color.</param>
    /// <param name="dotSize">Diameter of each dot, in pixels.</param>
    /// <param name="spacing">Horizontal gap between dots, in pixels.</param>
    public LoadingDots(
        Vector4 color,
        float dotSize = 6f,
        float spacing = 6f,
        Framework.Key? key = null
    ) : base(key)
    {
        Color = color;
        DotSize = dotSize;
        Spacing = spacing;
    }

    /// <summary>Dot color.</summary>
    public Vector4 Color { get; }

    /// <summary>Diameter of each dot, in pixels.</summary>
    public float DotSize { get; }

    /// <summary>Horizontal gap between dots, in pixels.</summary>
    public float Spacing { get; }

    /// <inheritdoc />
    public override State CreateState() => new LoadingDotsState();
}

internal class LoadingDotsState : State<LoadingDots>
{
    private AnimationController? _controller;

    public override void InitState()
    {
        base.InitState();
        _controller = new AnimationController(
            TimeSpan.FromMilliseconds(1200),
            Element.Owner!.GetTickerProvider()
        );
        _controller.OnStatusChanged += HandleStatusChanged;
        _controller.OnValueChanged += HandleValueChanged;
        _controller.Forward();
    }

    private void HandleValueChanged(double _) => SetState(() => { });

    private void HandleStatusChanged(AnimationStatus status)
    {
        if (status != AnimationStatus.Completed || _controller == null)
        {
            return;
        }
        _controller.Reset();
        _controller.Forward();
    }

    public override Widget Build(BuildContext context)
    {
        var t = _controller?.Value ?? 0.0;
        return new Row(
            spacing: Widget.Spacing,
            mainAxisAlignment: MainAxisAlignment.Center,
            crossAxisAlignment: CrossAxisAlignment.Center,
            mainAxisSize: MainAxisSize.Min,
            children: new List<Widget>
            {
                Dot(t, 0),
                Dot(t, 1),
                Dot(t, 2)
            }
        );
    }

    private Widget Dot(double t, int index)
    {
        var phase = (t + index * (1.0 / 3.0)) % 1.0;
        var pulse = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * phase);
        var opacity = 0.3 + 0.7 * pulse;

        return new Opacity(
            (float)opacity,
            new Container(
                new BoxStyle
                {
                    Color = Widget.Color,
                    Width = Widget.DotSize,
                    Height = Widget.DotSize,
                    CornerRadius = new Vector4(Widget.DotSize / 2f)
                }
            )
        );
    }

    public override void Dispose()
    {
        if (_controller != null)
        {
            _controller.OnStatusChanged -= HandleStatusChanged;
            _controller.OnValueChanged -= HandleValueChanged;
            _controller.Dispose();
            _controller = null;
        }

        base.Dispose();
    }
}
