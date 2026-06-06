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

public delegate Widget IndexedWidgetBuilder(
    BuildContext context,
    int index
);

public class ListView : StatefulWidget
{
    /// <summary>Fixed-height static-children mode.</summary>
    public ListView(
        IEnumerable<Widget> children,
        float itemHeight,
        ScrollController? controller = null,
        bool reverse = false,
        Axis scrollDirection = Axis.Vertical,
        Framework.Key? key = null
    ) : base(key)
    {
        var childList = new List<Widget>(children);
        ItemBuilder = (_, index) => childList[index];
        ItemCount = childList.Count;
        ItemHeight = itemHeight;
        EstimatedItemHeight = itemHeight;
        VariableHeight = false;
        StickToBottom = false;
        Reverse = reverse;
        ScrollDirection = scrollDirection;
        Controller = controller;
    }

    /// <summary>Variable-height static-children mode.</summary>
    public ListView(
        IEnumerable<Widget> children,
        float estimatedItemHeight,
        bool variableHeight,
        ScrollController? controller = null,
        bool stickToBottom = false,
        bool reverse = false,
        Axis scrollDirection = Axis.Vertical,
        Framework.Key? key = null
    ) : base(key)
    {
        var childList = new List<Widget>(children);
        ItemBuilder = (_, index) => childList[index];
        ItemCount = childList.Count;
        ItemHeight = 0;
        EstimatedItemHeight = estimatedItemHeight;
        VariableHeight = variableHeight;
        StickToBottom = stickToBottom;
        Reverse = reverse;
        ScrollDirection = scrollDirection;
        Controller = controller;
    }

    public ListView(
        IndexedWidgetBuilder itemBuilder,
        int itemCount,
        float itemHeight,
        ScrollController? controller = null,
        bool reverse = false,
        Axis scrollDirection = Axis.Vertical,
        Framework.Key? key = null
    ) : base(key)
    {
        ItemBuilder = itemBuilder;
        ItemCount = itemCount;
        ItemHeight = itemHeight;
        EstimatedItemHeight = itemHeight;
        VariableHeight = false;
        StickToBottom = false;
        Reverse = reverse;
        ScrollDirection = scrollDirection;
        Controller = controller;
    }

    public ListView(
        IndexedWidgetBuilder itemBuilder,
        int itemCount,
        float estimatedItemHeight,
        bool variableHeight,
        ScrollController? controller = null,
        bool stickToBottom = false,
        bool reverse = false,
        Axis scrollDirection = Axis.Vertical,
        Framework.Key? key = null
    ) : base(key)
    {
        ItemBuilder = itemBuilder;
        ItemCount = itemCount;
        ItemHeight = 0;
        EstimatedItemHeight = estimatedItemHeight;
        VariableHeight = variableHeight;
        StickToBottom = stickToBottom;
        Reverse = reverse;
        ScrollDirection = scrollDirection;
        Controller = controller;
    }

    public IndexedWidgetBuilder ItemBuilder { get; }
    public int ItemCount { get; }
    public float ItemHeight { get; }
    public float EstimatedItemHeight { get; }
    public bool VariableHeight { get; }
    public bool StickToBottom { get; }

    /// <summary>
    ///     When true, the list is anchored at the bottom: scroll offset 0 shows
    ///     the last items and scrolling increases the offset upward (toward
    ///     older content).
    /// </summary>
    public bool Reverse { get; }

    /// <summary>The axis along which the list scrolls.</summary>
    public Axis ScrollDirection { get; }

    public ScrollController? Controller { get; }

    public override State CreateState() => new ListViewState();

    private class ListViewState : State<ListView>, IScrollableContext
    {
        private readonly ScrollWheelHandler _wheelHandler = new();
        private float _contentHeight;

        private ScrollController? _internalController;
        private bool _stickyBottom;

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

            _stickyBottom = Widget.StickToBottom;
            AttachController(Controller);
        }

        public override void UpdateWidget(
            ListView old
        )
        {
            base.UpdateWidget(old);
            var oldCtrl = old.Controller ?? _internalController!;
            var newCtrl = Widget.Controller ?? _internalController!;
            if (oldCtrl != newCtrl)
            {
                DetachController(oldCtrl);
                AttachController(newCtrl);
                _stickyBottom = Widget.StickToBottom;
                _contentHeight = 0;
                _viewportHeight = 0;
            }
        }

        private void AttachController(
            ScrollController ctrl
        )
        {
            ctrl.Attach(Element.Owner!.GetTickerProvider());
            ctrl.OnChanged += HandleScrollUpdate;
        }

        private void DetachController(
            ScrollController ctrl
        ) =>
            ctrl.OnChanged -= HandleScrollUpdate;

        private void HandleScrollUpdate()
        {
            var maxScroll = Math.Max(0, _contentHeight - _viewportHeight);
            ScrollWheelHandler.ClampOffset(Controller, maxScroll);

            if (Widget.StickToBottom && !Controller.IsAnimating)
            {
                if (Widget.Reverse)
                {
                    _stickyBottom = Controller.Offset <= 1;
                }
                else
                {
                    _stickyBottom = Controller.Offset >= maxScroll - 1;
                }
            }

            SetState(() => { });
        }

        private void HandleLayoutUpdate(
            float viewportHeight,
            float contentHeight
        )
        {
            _viewportHeight = viewportHeight;
            _contentHeight = contentHeight;

            // Save sticky state before property setters fire
            // HandleScrollUpdate via OnChanged, which would falsely
            // clear _stickyBottom while offset hasn't caught up yet.
            var wasSticky = _stickyBottom || Controller.PendingScrollToEnd;

            Controller.ViewportSize = viewportHeight;
            Controller.ContentSize = contentHeight;

            // Auto-scroll to bottom when sticky or explicitly requested
            if (Widget.StickToBottom && wasSticky)
            {
                var maxScroll = Math.Max(
                    0,
                    contentHeight - viewportHeight
                );
                if (Widget.Reverse)
                {
                    // Reverse mode: "bottom" = offset 0
                    if (Controller.Offset > 0.5f)
                    {
                        var pending = Controller.PendingScrollToEnd;
                        Controller.PendingScrollToEnd = false;
                        _stickyBottom = true;
                        if (pending)
                        {
                            Controller.JumpTo(0);
                        }
                        else
                        {
                            Controller.AnimateTo(
                                0,
                                TimeSpan.FromMilliseconds(150),
                                maxScroll: maxScroll
                            );
                        }
                    }
                    else
                    {
                        Controller.PendingScrollToEnd = false;
                        _stickyBottom = true;
                    }
                }
                else if (maxScroll > 0 && Controller.Offset < maxScroll - 0.5f)
                {
                    var pending = Controller.PendingScrollToEnd;
                    Controller.PendingScrollToEnd = false;
                    _stickyBottom = true;
                    if (pending)
                    {
                        Controller.JumpTo(maxScroll);
                    }
                    else
                    {
                        Controller.AnimateTo(
                            maxScroll,
                            TimeSpan.FromMilliseconds(150),
                            maxScroll: maxScroll
                        );
                    }
                }
                else if (maxScroll > 0)
                {
                    // Already at (or past) bottom — stay pinned
                    Controller.PendingScrollToEnd = false;
                    _stickyBottom = true;
                }
            }
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

            if (Widget.StickToBottom)
            {
                _stickyBottom = false;
            }

            e.Handled = true;
        }

        public override Widget Build(
            BuildContext context
        )
        {
            float effectiveOffset;
            if (Widget.Reverse)
            {
                var contentEst = _contentHeight > 0
                    ? _contentHeight
                    : Widget.ItemCount * (Widget.VariableHeight
                        ? Widget.EstimatedItemHeight
                        : Widget.ItemHeight);
                var vpEst = _viewportHeight > 0
                    ? _viewportHeight
                    : 400f;
                var maxScroll = Math.Max(
                    0,
                    contentEst - vpEst
                );
                effectiveOffset = Math.Max(
                    0,
                    maxScroll - Controller.Offset
                );
            }
            else
            {
                effectiveOffset = Controller.Offset;
            }

            var viewportOffset = Widget.ScrollDirection == Axis.Horizontal
                ? new Vector2(effectiveOffset, 0)
                : new Vector2(0, effectiveOffset);

            return new GestureDetector(
                onWheel: OnMouseWheel,
                child: new Viewport(
                    viewportOffset,
                    new ListViewContent(
                        Widget.ItemBuilder,
                        Widget.ItemCount,
                        Widget.ItemHeight,
                        Widget.EstimatedItemHeight,
                        Widget.VariableHeight,
                        effectiveOffset,
                        HandleLayoutUpdate,
                        Widget.Reverse
                            ? null
                            : delta => { Controller.JumpTo(Controller.Offset + delta); },
                        Controller,
                        Widget.ScrollDirection
                    ),
                    Widget.ScrollDirection
                )
            );
        }

        public override void Dispose()
        {
            DetachController(Controller);
            _internalController?.Dispose();
            base.Dispose();
        }
    }

    private class ListViewContent : Widget
    {
        public ListViewContent(
            IndexedWidgetBuilder itemBuilder,
            int itemCount,
            float itemHeight,
            float estimatedItemHeight,
            bool variableHeight,
            float scrollOffset,
            Action<float, float> onLayout,
            Action<float>? onOffsetCorrection = null,
            object? dataIdentity = null,
            Axis scrollDirection = Axis.Vertical
        )
        {
            ItemBuilder = itemBuilder;
            ItemCount = itemCount;
            ItemHeight = itemHeight;
            EstimatedItemHeight = estimatedItemHeight;
            VariableHeight = variableHeight;
            ScrollOffset = scrollOffset;
            OnLayout = onLayout;
            OnOffsetCorrection = onOffsetCorrection;
            DataIdentity = dataIdentity;
            ScrollDirection = scrollDirection;
        }

        public IndexedWidgetBuilder ItemBuilder { get; }
        public int ItemCount { get; }
        public float ItemHeight { get; }
        public float EstimatedItemHeight { get; }
        public bool VariableHeight { get; }
        public float ScrollOffset { get; }
        public Action<float, float> OnLayout { get; }
        public Action<float>? OnOffsetCorrection { get; }
        public Axis ScrollDirection { get; }

        /// <summary>
        ///     Reference-compared identity object (e.g. ScrollController).
        ///     When it changes between updates, the element fully resets its
        ///     height cache and widget cache.
        /// </summary>
        public object? DataIdentity { get; }

        public override Element CreateElement() => new ListViewContentElement(this);
    }

    private class ListViewContentElement : Element
    {
        private readonly Dictionary<int, Element> _activeChildren = new();
        private readonly ItemHeightCache _cache = new();
        private readonly Dictionary<int, Widget> _cachedWidgets = new();
        private readonly RenderListViewContent _renderObject;

        public ListViewContentElement(
            ListViewContent widget
        ) : base(widget)
        {
            _renderObject = new RenderListViewContent(widget.OnLayout)
            {
                ScrollDirection = widget.ScrollDirection
            };
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
            UpdateVisibleItems();
        }

        public override void Update(
            Widget newWidget
        )
        {
            var old = (ListViewContent)Widget;
            var next = (ListViewContent)newWidget;

            _renderObject.ScrollDirection = next.ScrollDirection;

            var dataChanged = !ReferenceEquals(
                old.DataIdentity,
                next.DataIdentity
            );

            if (dataChanged)
            {
                // Data source changed — full reset so stale widgets
                // and measured heights from the old source are discarded.
                _cachedWidgets.Clear();
                if (next.VariableHeight)
                {
                    _cache.Reset(
                        next.ItemCount,
                        next.EstimatedItemHeight
                    );
                }
            }
            else if (next.VariableHeight)
            {
                if (next.ItemCount > _cache.Count)
                {
                    _cache.GrowTo(
                        next.ItemCount,
                        next.EstimatedItemHeight
                    );
                }
                else if (next.ItemCount != _cache.Count)
                {
                    _cache.Reset(
                        next.ItemCount,
                        next.EstimatedItemHeight
                    );
                    _cachedWidgets.Clear();
                }

                if (Math.Abs(next.EstimatedItemHeight - _cache.DefaultEstimate) > 0.01f)
                {
                    _cache.SetEstimatedHeight(next.EstimatedItemHeight);
                }
            }
            else
            {
                if (Math.Abs(old.ItemHeight - next.ItemHeight) > 0.01
                    || old.ItemCount != next.ItemCount)
                {
                    _cachedWidgets.Clear();
                }
            }

            base.Update(newWidget);
            UpdateVisibleItems();
        }

        private void UpdateVisibleItems()
        {
            var widget = (ListViewContent)Widget;

            if (widget.VariableHeight)
            {
                UpdateVisibleItemsVariable(widget);
            }
            else
            {
                UpdateVisibleItemsFixed(widget);
            }
        }

        private void UpdateVisibleItemsFixed(
            ListViewContent widget
        )
        {
            _renderObject.TotalHeight = widget.ItemCount * widget.ItemHeight;
            _renderObject.FixedItemHeight = widget.ItemHeight;
            _renderObject.IsVariableHeight = false;

            var viewportHeight = _renderObject.ViewportHeight;
            if (viewportHeight <= 0)
            {
                viewportHeight = 400;
            }

            var firstVisible = (int)Math.Floor(widget.ScrollOffset / widget.ItemHeight);
            var lastVisible =
                (int)Math.Ceiling((widget.ScrollOffset + viewportHeight) / widget.ItemHeight);

            firstVisible = Math.Max(
                0,
                firstVisible - 1
            );
            lastVisible = Math.Min(
                widget.ItemCount - 1,
                lastVisible + 1
            );

            var targetIndices = new HashSet<int>();
            for (var i = firstVisible; i <= lastVisible; i++)
            {
                targetIndices.Add(i);
            }

            ReconcileChildren(
                widget,
                targetIndices,
                index => index * widget.ItemHeight
            );
            _renderObject.MarkNeedsLayout();
        }

        private void UpdateVisibleItemsVariable(
            ListViewContent widget
        )
        {
            if (_cache.Count == 0 && widget.ItemCount > 0)
            {
                _cache.Reset(
                    widget.ItemCount,
                    widget.EstimatedItemHeight
                );
            }

            _renderObject.TotalHeight = _cache.TotalHeight;
            _renderObject.IsVariableHeight = true;
            _renderObject.HeightCache = _cache;
            _renderObject.ScrollOffset = widget.ScrollOffset;
            _renderObject.OnOffsetCorrection = widget.OnOffsetCorrection;

            var viewportHeight = _renderObject.ViewportHeight;
            if (viewportHeight <= 0)
            {
                viewportHeight = 400;
            }

            var firstVisible = _cache.FindFirstVisible(widget.ScrollOffset);
            var lastVisible = _cache.FindLastVisible(
                widget.ScrollOffset,
                viewportHeight
            );

            firstVisible = Math.Max(
                0,
                firstVisible - 1
            );
            lastVisible = Math.Min(
                widget.ItemCount - 1,
                lastVisible + 1
            );

            var targetIndices = new HashSet<int>();
            for (var i = firstVisible; i <= lastVisible; i++)
            {
                targetIndices.Add(i);
            }

            ReconcileChildren(
                widget,
                targetIndices,
                index => _cache.GetPosition(index)
            );
            _renderObject.MarkNeedsLayout();
        }

        private void ReconcileChildren(
            ListViewContent widget,
            HashSet<int> targetIndices,
            Func<int, float> getPosition
        )
        {
            UnmountChildrenOutsideRange(targetIndices);

            foreach (var index in targetIndices)
            {
                var itemWidget = GetOrBuildWidget(
                    widget,
                    index
                );
                var element = MountOrUpdateChild(
                    index,
                    itemWidget
                );
                PositionChild(
                    element,
                    index,
                    getPosition
                );
            }
        }

        private void UnmountChildrenOutsideRange(
            HashSet<int> keepIndices
        )
        {
            var toRemove = new List<int>();
            foreach (var index in _activeChildren.Keys)
            {
                if (!keepIndices.Contains(index))
                {
                    toRemove.Add(index);
                }
            }

            foreach (var index in toRemove)
            {
                var el = _activeChildren[index];
                DetachChild(el);
                el.Unmount();
                _activeChildren.Remove(index);
            }
        }

        private Widget GetOrBuildWidget(
            ListViewContent widget,
            int index
        )
        {
            if (!_cachedWidgets.TryGetValue(
                    index,
                    out var itemWidget
                ))
            {
                itemWidget = widget.ItemBuilder(
                    new BuildContext(
                        Widget,
                        this
                    ),
                    index
                );
                _cachedWidgets[index] = itemWidget;
            }

            return itemWidget;
        }

        private Element MountOrUpdateChild(
            int index,
            Widget itemWidget
        )
        {
            if (_activeChildren.TryGetValue(
                    index,
                    out var existing
                ))
            {
                var updated = UpdateChild(
                    existing,
                    itemWidget
                )!;
                if (updated != existing)
                {
                    // Key mismatch — old element was replaced.
                    DetachChild(existing);
                    AttachChild(updated);
                }

                _activeChildren[index] = updated;
                return updated;
            }

            var created = UpdateChild(
                null,
                itemWidget
            )!;
            _activeChildren[index] = created;
            AttachChild(created);
            return created;
        }

        private void PositionChild(
            Element element,
            int index,
            Func<int, float> getPosition
        )
        {
            var ro = element.RenderObject;
            if (ro == null)
            {
                return;
            }

            var pos = getPosition(index);
            if (_renderObject.ScrollDirection == Axis.Horizontal)
            {
                ro.X = pos;
            }
            else
            {
                ro.Y = pos;
            }

            _renderObject.ChildIndexMap[ro] = index;
        }

        private void AttachChild(
            Element element
        )
        {
            if (element.RenderObject != null)
            {
                _renderObject.AddChild(element.RenderObject);
            }
        }

        private void DetachChild(
            Element element
        )
        {
            if (element.RenderObject != null)
            {
                _renderObject.ChildIndexMap.Remove(element.RenderObject);
                _renderObject.RemoveChild(element.RenderObject);
            }
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
    }
}
