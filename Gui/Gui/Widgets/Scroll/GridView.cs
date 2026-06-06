using System;
using System.Collections.Generic;
using Gui.Core.Framework;
using Gui.Core.Scroll;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Widgets.Scroll;

public class GridView : StatefulWidget
{
    /// <summary>Static mode: all children provided up-front.</summary>
    public GridView(
        IEnumerable<Widget> children,
        SliverGridDelegate gridDelegate,
        ScrollController? controller = null,
        bool reverse = false,
        Framework.Key? key = null
    ) : base(key)
    {
        Children = new List<Widget>(children);
        ItemCount = Children.Count;
        GridDelegate = gridDelegate;
        Controller = controller;
        Reverse = reverse;
    }

    private GridView(
        IndexedWidgetBuilder itemBuilder,
        int itemCount,
        SliverGridDelegate gridDelegate,
        ScrollController? controller,
        bool reverse,
        Framework.Key? key
    ) : base(key)
    {
        ItemBuilder = itemBuilder;
        ItemCount = itemCount;
        GridDelegate = gridDelegate;
        Controller = controller;
        Reverse = reverse;
    }

    public IReadOnlyList<Widget>? Children { get; }
    public IndexedWidgetBuilder? ItemBuilder { get; }
    public int ItemCount { get; }
    public SliverGridDelegate GridDelegate { get; }
    public ScrollController? Controller { get; }

    /// <summary>
    ///     When true, scroll origin is at the bottom: offset 0 shows the
    ///     last row of content.
    /// </summary>
    public bool Reverse { get; }

    /// <summary>Builder mode: children created lazily with virtualization.</summary>
    public static GridView Builder(
        IndexedWidgetBuilder itemBuilder,
        int itemCount,
        SliverGridDelegate gridDelegate,
        ScrollController? controller = null,
        bool reverse = false,
        Framework.Key? key = null
    )
    {
        return new GridView(
            itemBuilder,
            itemCount,
            gridDelegate,
            controller,
            reverse,
            key
        );
    }

    public override State CreateState() => new GridViewState();


    private class GridViewState : State<GridView>
    {
        private const float WheelImpulseStrength = 1200f;
        private float _contentHeight;
        private float _currentVelocity;

        private ScrollController? _internalController;
        private DateTime _lastWheelTime = DateTime.MinValue;

        private float _viewportHeight;
        private float _viewportWidth;
        private ScrollController Controller => Widget.Controller ?? _internalController!;

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
            var effectiveWidth = _viewportWidth > 0
                ? _viewportWidth
                : 400;
            var geometry = Widget.GridDelegate.GetGeometry(effectiveWidth);
            _contentHeight = geometry.TotalContentHeight(Widget.ItemCount);
            var maxScroll = Math.Max(
                0,
                _contentHeight - _viewportHeight
            );

            if (Controller.Offset < -50 || Controller.Offset > maxScroll + 50)
            {
                Controller.JumpTo(
                    Math.Clamp(
                        Controller.Offset,
                        0,
                        maxScroll
                    )
                );
            }

            SetState(() => { });
        }

        private void OnMouseWheel(
            PointerEvent e
        )
        {
            var effectiveWidth = _viewportWidth > 0
                ? _viewportWidth
                : 400;
            var geometry = Widget.GridDelegate.GetGeometry(effectiveWidth);
            _contentHeight = geometry.TotalContentHeight(Widget.ItemCount);
            var maxScroll = Math.Max(
                0,
                _contentHeight - _viewportHeight
            );

            if (!ScrollWheelHandler.CanScroll(Controller, e.Delta, maxScroll, false))
            {
                return;
            }

            if (maxScroll <= 0)
            {
                return;
            }

            var now = DateTime.Now;
            var dt = (now - _lastWheelTime).TotalSeconds;
            _lastWheelTime = now;

            var impulse = -e.Delta * WheelImpulseStrength;
            if (Widget.Reverse)
            {
                impulse = -impulse;
            }

            if (Math.Sign(impulse) != Math.Sign(_currentVelocity) &&
                Math.Abs(_currentVelocity) > 1)
            {
                _currentVelocity = impulse;
            }
            else if (dt < 0.1)
            {
                _currentVelocity += impulse;
            }
            else
            {
                _currentVelocity = impulse;
            }

            _currentVelocity = Math.Clamp(
                _currentVelocity,
                -8000,
                8000
            );
            Controller.StartSimulation(
                -_currentVelocity,
                0,
                maxScroll
            );
            e.Handled = true;
        }

        public override Widget Build(
            BuildContext context
        )
        {
            var effectiveWidth = _viewportWidth > 0
                ? _viewportWidth
                : 400;
            var geometry = Widget.GridDelegate.GetGeometry(effectiveWidth);
            _contentHeight = geometry.TotalContentHeight(Widget.ItemCount);

            var maxScroll = Math.Max(0, _contentHeight - _viewportHeight);
            var effectiveOffset = Widget.Reverse
                ? Math.Max(0, maxScroll - Controller.Offset)
                : Controller.Offset;

            return new GestureDetector(
                onWheel: OnMouseWheel,
                child: new Viewport(
                    new Vector2(
                        0,
                        effectiveOffset
                    ),
                    new GridContent(
                        Widget.ItemBuilder,
                        Widget.Children,
                        Widget.ItemCount,
                        Widget.GridDelegate,
                        Controller.Offset,
                        h =>
                        {
                            _viewportHeight = h;
                            Controller.ViewportSize = h;
                            Controller.ContentSize = _contentHeight;
                        },
                        w => _viewportWidth = w
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


    private class GridContent : Widget
    {
        public readonly SliverGridDelegate GridDelegate;
        public readonly IndexedWidgetBuilder? ItemBuilder;
        public readonly int ItemCount;
        public readonly IReadOnlyList<Widget>? Items;
        public readonly Action<float> OnViewportLayout;
        public readonly Action<float> OnViewportWidth;
        public readonly float ScrollOffset;

        public GridContent(
            IndexedWidgetBuilder? itemBuilder,
            IReadOnlyList<Widget>? items,
            int itemCount,
            SliverGridDelegate gridDelegate,
            float scrollOffset,
            Action<float> onViewportLayout,
            Action<float> onViewportWidth
        )
        {
            ItemBuilder = itemBuilder;
            Items = items;
            ItemCount = itemCount;
            GridDelegate = gridDelegate;
            ScrollOffset = scrollOffset;
            OnViewportLayout = onViewportLayout;
            OnViewportWidth = onViewportWidth;
        }

        public override Element CreateElement() => new GridContentElement(this);
    }


    private class GridContentElement : Element
    {
        private readonly Dictionary<int, Element> _activeChildren = new();
        private readonly RenderGridContent _renderObject;

        public GridContentElement(
            GridContent widget
        ) : base(widget)
        {
            _renderObject = new RenderGridContent(
                widget.OnViewportLayout,
                widget.OnViewportWidth
            );
        }

        public override RenderObject? RenderObject => _renderObject;

        public override void Mount(
            Element? parent
        )
        {
            base.Mount(parent);
            Rebuild();
        }

        public override void Rebuild()
        {
            base.Rebuild();
            UpdateVisibleCells();
        }

        public override void Update(
            Widget newWidget
        )
        {
            var oldW = (GridContent)Widget;
            var newW = (GridContent)newWidget;

            _renderObject.OnViewportLayout = newW.OnViewportLayout;
            _renderObject.OnViewportWidth = newW.OnViewportWidth;

            base.Update(newWidget);
            UpdateVisibleCells();
        }

        private void UpdateVisibleCells()
        {
            var widget = (GridContent)Widget;

            var effectiveWidth = _renderObject.ViewportWidth;
            if (effectiveWidth <= 0)
            {
                effectiveWidth = 400;
            }

            var geometry = widget.GridDelegate.GetGeometry(effectiveWidth);
            _renderObject.Geometry = geometry;
            _renderObject.TotalHeight = geometry.TotalContentHeight(widget.ItemCount);

            var viewportHeight = _renderObject.ViewportHeight;
            if (viewportHeight <= 0)
            {
                viewportHeight = 400;
            }

            var (firstIndex, lastIndex) = geometry.ComputeVisibleRange(
                widget.ScrollOffset,
                viewportHeight,
                widget.ItemCount
            );

            var targetIndices = new HashSet<int>();
            for (var i = firstIndex; i <= lastIndex; i++)
            {
                targetIndices.Add(i);
            }

            // Remove children that scrolled out of view
            var toRemove = new List<int>();
            foreach (var index in _activeChildren.Keys)
            {
                if (!targetIndices.Contains(index))
                {
                    toRemove.Add(index);
                }
            }

            foreach (var index in toRemove)
            {
                var el = _activeChildren[index];
                if (el.RenderObject != null)
                {
                    _renderObject.RemoveChild(el.RenderObject);
                }

                el.Unmount();
                _activeChildren.Remove(index);
            }

            // Add or update visible children
            var ctx = new BuildContext(
                Widget,
                this
            );
            foreach (var index in targetIndices)
            {
                var itemWidget = widget.Items != null
                    ? widget.Items[index]
                    : widget.ItemBuilder!(
                        ctx,
                        index
                    );

                if (_activeChildren.TryGetValue(
                        index,
                        out var existing
                    ))
                {
                    var oldRo = existing.RenderObject;
                    var updated = UpdateChild(
                        existing,
                        itemWidget
                    )!;
                    _activeChildren[index] = updated;
                    var newRo = updated.RenderObject;
                    if (oldRo != newRo)
                    {
                        if (oldRo != null)
                        {
                            _renderObject.RemoveChild(oldRo);
                        }

                        if (newRo != null)
                        {
                            _renderObject.AddChild(newRo);
                        }
                    }
                }
                else
                {
                    var newEl = UpdateChild(
                        null,
                        itemWidget
                    )!;
                    _activeChildren[index] = newEl;
                    if (newEl.RenderObject != null)
                    {
                        _renderObject.AddChild(newEl.RenderObject);
                    }
                }

                var (x, y) = geometry.CellOrigin(index);
                if (_activeChildren[index].RenderObject != null)
                {
                    _activeChildren[index].RenderObject!.X = x;
                    _activeChildren[index].RenderObject!.Y = y;
                }
            }

            _renderObject.MarkNeedsLayout();
        }

        public override void Unmount()
        {
            foreach (var el in _activeChildren.Values)
            {
                if (el.RenderObject != null)
                {
                    _renderObject.RemoveChild(el.RenderObject);
                }

                el.Unmount();
            }

            _activeChildren.Clear();
            base.Unmount();
        }

        public override void VisitChildren(
            Action<Element> visitor
        )
        {
            var keys = new List<int>(_activeChildren.Keys);
            keys.Sort();
            foreach (var key in keys)
            {
                visitor(_activeChildren[key]);
            }
        }

        protected override void VisitChildrenInReverse(
            Action<Element> visitor
        )
        {
            var keys = new List<int>(_activeChildren.Keys);
            keys.Sort();
            for (var i = keys.Count - 1; i >= 0; i--)
            {
                visitor(_activeChildren[keys[i]]);
            }
        }
    }
}
