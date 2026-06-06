using System;
using Gui.Core.Framework;
using Gui.Core.Scroll;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Widgets.Scroll;

public class SingleChildScrollView : StatefulWidget
{
    public SingleChildScrollView(
        Widget? child = null,
        ScrollController? controller = null,
        bool reverse = false
    )
    {
        Content = child;
        Controller = controller;
        Reverse = reverse;
    }

    public Widget? Content { get; }
    public ScrollController? Controller { get; }

    /// <summary>
    ///     When true, scroll origin is at the bottom: offset 0 shows the
    ///     bottom of content.
    /// </summary>
    public bool Reverse { get; }

    public override State CreateState() => new SingleChildScrollViewState();

    private class SingleChildScrollViewState : State<SingleChildScrollView>, IScrollableContext
    {
        private readonly ScrollWheelHandler _wheelHandler = new();
        private float _contentHeight;

        private ScrollController? _internalController;

        private float _viewportHeight;
        private ScrollController Controller => Widget.Controller ?? _internalController!;

        ScrollController IScrollableContext.ScrollController => Controller;
        float IScrollableContext.ViewportHeight => _viewportHeight;
        float IScrollableContext.ContentHeight => _contentHeight;

        public override void InitState()
        {
            base.InitState();
            if (Widget.Controller == null)
            {
                _internalController = new ScrollController();
            }

            Controller.Attach(Element.Owner!.GetTickerProvider());
            Controller.OnChanged += HandleScrollUpdate;
        }

        private void HandleScrollUpdate()
        {
            var maxScroll = Math.Max(0, _contentHeight - _viewportHeight);
            ScrollWheelHandler.ClampOffset(Controller, maxScroll);
            SetState(() => { });
        }

        public void OnMouseWheel(
            PointerEvent e
        )
        {
            var maxScroll = Math.Max(0, _contentHeight - _viewportHeight);
            if (!_wheelHandler.Apply(e.Delta, Widget.Reverse, maxScroll, Controller))
            {
                return;
            }

            e.Handled = true;
        }

        public override Widget Build(
            BuildContext context
        )
        {
            var effectiveOffset = Widget.Reverse
                ? Math.Max(0, Math.Max(0, _contentHeight - _viewportHeight) - Controller.Offset)
                : Controller.Offset;

            return new GestureDetector(
                onWheel: OnMouseWheel,
                child: new Viewport(
                    new Vector2(
                        0,
                        effectiveOffset
                    ),
                    new ScrollableContentNotifier(
                        (
                            vh,
                            ch
                        ) =>
                        {
                            _viewportHeight = vh;
                            _contentHeight = ch;
                            Controller.ViewportSize = vh;
                            Controller.ContentSize = ch;
                        },
                        Widget.Content
                    )
                )
            );
        }

        public override void Dispose()
        {
            Controller.OnChanged -= HandleScrollUpdate;
            _internalController?.Dispose();
            base.Dispose();
        }
    }

    private class ScrollableContentNotifier : SingleChildWidget
    {
        public ScrollableContentNotifier(
            Action<float, float> onLayout,
            Widget? child
        ) : base(child)
        {
            OnLayout = onLayout;
        }

        public Action<float, float> OnLayout { get; }

        public override Element CreateElement() => new ScrollableContentElement(this);

        public override RenderObject CreateRenderObject() => new RenderScrollableContent(OnLayout);
    }

    private class ScrollableContentElement : SingleChildElement
    {
        public ScrollableContentElement(
            ScrollableContentNotifier widget
        ) : base(widget)
        {
        }
    }
}
