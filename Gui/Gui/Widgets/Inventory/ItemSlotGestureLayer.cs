using System;
using Gui.Widgets.Animations;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using Gui.Widgets.Overlay;
using OpenTK.Mathematics;
using Vintagestory.API.Common;

namespace Gui.Widgets.Inventory;

/// <summary>
///     Stateful wrapper owning hover animation, gesture handling, tooltip, drag/click/wheel
///     interactions, and event-driven slot change detection. Publishes hover animation to
///     descendants via <see cref="ItemSlotHoverData" />.
/// </summary>
public class ItemSlotGestureLayer : StatefulWidget
{
    /// <summary>Creates a gesture layer that builds its visual child via <paramref name="childBuilder" />.</summary>
    public ItemSlotGestureLayer(
        Func<Widget> childBuilder,
        ItemSlot? slot = null,
        SlotController? controller = null,
        ValueNotifier<bool>? isHighlighted = null,
        IInventory? outerInventory = null,
        Framework.Key? key = null
    ) : base(key)
    {
        ChildBuilder = childBuilder;
        Slot = slot;
        Controller = controller;
        IsHighlighted = isHighlighted;
        OuterInventory = outerInventory;
    }

    /// <summary>
    ///     Factory called on every rebuild to produce the slot visual subtree
    ///     (background + overlay). Invoked fresh each time so slot content is
    ///     always up-to-date.
    /// </summary>
    public Func<Widget> ChildBuilder { get; }

    /// <summary>The inventory slot for interaction and tooltip. May be null.</summary>
    public ItemSlot? Slot { get; }

    /// <summary>Handles click, wheel, and drag forwarding. May be null.</summary>
    public SlotController? Controller { get; }

    /// <summary>
    ///     When set, the slot shows a persistent highlight border whenever <c>Value</c> is <c>true</c>,
    ///     independently of pointer hover.
    /// </summary>
    public ValueNotifier<bool>? IsHighlighted { get; }

    /// <summary>
    ///     When set, <see cref="SlotController" /> calls are routed through this inventory
    ///     for <c>ActivateSlot</c> instead of <c>Slot.Inventory</c>. Required when the slot
    ///     belongs to a tab sub-inventory (e.g. creative) whose parent overrides
    ///     <c>ActivateSlot</c> to inject tab-routing data into the network packet.
    /// </summary>
    public IInventory? OuterInventory { get; }

    /// <inheritdoc />
    public override Framework.State CreateState() => new State();

    private class State : State<ItemSlotGestureLayer>
    {
        private readonly ValueNotifier<Vector2?> _pointerGlobal = new(null);
        private SlotChangeFilter _changeFilter;
        private CurvedAnimation _hoverAnimation = null!;
        private AnimationController _hoverController = null!;
        private float _itemScale = 1f;

        public override void InitState()
        {
            base.InitState();
            _hoverController = new AnimationController(
                TimeSpan.FromMilliseconds(150),
                Element.Owner!.GetTickerProvider()
            );
            _hoverAnimation = new CurvedAnimation(_hoverController, Curves.EaseOut);
            _hoverController.OnValueChanged += _onAnimationTick;
            _changeFilter.ShouldSkipRebuild(Widget.Controller?.World, Widget.Slot?.Itemstack);
            Widget.Controller?.AddListener(_onControllerChanged);
            Widget.IsHighlighted?.AddListener(_onHighlightChanged);
        }

        public override void UpdateWidget(ItemSlotGestureLayer oldWidget)
        {
            if (oldWidget.Controller != Widget.Controller)
            {
                oldWidget.Controller?.RemoveListener(_onControllerChanged);
                Widget.Controller?.AddListener(_onControllerChanged);
            }

            if (oldWidget.IsHighlighted != Widget.IsHighlighted)
            {
                oldWidget.IsHighlighted?.RemoveListener(_onHighlightChanged);
                Widget.IsHighlighted?.AddListener(_onHighlightChanged);
            }
        }

        public override Widget Build(BuildContext context)
        {
            var itemStack = Widget.Slot?.Itemstack;
            var hasTooltip = itemStack?.Collectible != null;

            Widget slotContent = new ItemSlotHoverData(
                _hoverAnimation,
                Widget.ChildBuilder(),
                Widget.IsHighlighted?.Value ?? false,
                _itemScale,
                _onPunchEnd
            ) { PointerPosition = _pointerGlobal };

            return new Tooltip(
                new GestureDetector(
                    onPress: _onPress,
                    onRelease: _onRelease,
                    onWheel: _onWheel,
                    onEnter: _onEnter,
                    onExit: _onExit,
                    onMove: _onMove,
                    child: slotContent
                ),
                useGlobalOverlay: true,
                content: hasTooltip && Widget.Controller != null
                    ? new ItemTooltipContent(Widget.Slot!, Widget.Controller.World)
                    : new SizedBox(),
                waitDuration: hasTooltip
                    ? TimeSpan.FromMilliseconds(350)
                    : TimeSpan.FromHours(1)
            );
        }

        private void _onAnimationTick(double _) => SetState(() => { });

        private void _onHighlightChanged() => SetState(() => { });

        private void _onControllerChanged()
        {
            if (_changeFilter.ShouldSkipRebuild(Widget.Controller?.World, Widget.Slot?.Itemstack))
            {
                return;
            }

            SetState(() => _itemScale = 1.12f);
        }

        private void _onPunchEnd() => SetState(() => _itemScale = 1f);

        private void _onPress(PointerEvent e)
        {
            if (Widget.Slot == null || Widget.Controller == null)
            {
                return;
            }

            var button = e.Button switch
            {
                PointerButton.Left => EnumMouseButton.Left,
                PointerButton.Right => EnumMouseButton.Right,
                PointerButton.Middle => EnumMouseButton.Middle,
                _ => (EnumMouseButton?)null
            };
            if (button == null)
            {
                return;
            }

            Widget.Controller.BeginDrag(button.Value, Widget.Slot);
            Widget.Controller.ClickSlot(Widget.Slot, button.Value, Widget.OuterInventory);
        }

        private void _onRelease(PointerEvent e) => Widget.Controller?.EndDrag();

        private void _onWheel(PointerEvent e)
        {
            if (Widget.Slot == null || Widget.Controller == null)
            {
                return;
            }

            if (Widget.Controller.WheelSlot(Widget.Slot, e.Delta > 0 ? 1 : -1,
                    Widget.OuterInventory))
            {
                e.Handled = true;
            }
        }

        private void _onEnter(PointerEvent e)
        {
            if (Widget.Slot != null && Widget.Controller != null)
            {
                Widget.Controller.DragEnterSlot(Widget.Slot, Widget.OuterInventory);
                Widget.Controller.EnterSlot(Widget.Slot);
            }

            _pointerGlobal.Value = new Vector2(e.X, e.Y);
            _hoverController.Forward();
        }

        private void _onMove(PointerEvent e)
        {
            // Update the notifier only — the spotlight render object repaints itself via its
            // listener, avoiding a full slot rebuild on every pointer move.
            _pointerGlobal.Value = new Vector2(e.X, e.Y);
        }

        private void _onExit(PointerEvent e)
        {
            Widget.Controller?.LeaveSlot();
            _hoverController.Reverse();
            // Keep the last pointer position so the spotlight fades out where the cursor
            // left instead of snapping to the slot centre for a frame. It is overwritten
            // on the next enter.
            SetState(() => { });
        }

        public override void Dispose()
        {
            _hoverController.OnValueChanged -= _onAnimationTick;
            Widget.Controller?.RemoveListener(_onControllerChanged);
            Widget.IsHighlighted?.RemoveListener(_onHighlightChanged);
            _hoverController.Dispose();
            _pointerGlobal.Dispose();
            base.Dispose();
        }
    }
}

/// <summary>
///     Tracks slot visual state to skip unnecessary rebuilds when
///     a <see cref="SlotController" /> fires but the slot data hasn't changed.
///     Uses <see cref="ItemStack.Equals(IWorldAccessor,ItemStack)" /> for full attribute
///     comparison, covering liquid content, temperature, and all other visual state.
/// </summary>
internal struct SlotChangeFilter
{
    private ItemStack? _lastSnapshot;
    private bool _initialized;

    /// <summary>
    ///     Returns <c>true</c> when <paramref name="current" /> is visually identical
    ///     to the previously recorded snapshot and the rebuild can be skipped.
    /// </summary>
    public bool ShouldSkipRebuild(IWorldAccessor? world, ItemStack? current)
    {
        if (_initialized)
        {
            var same = (current == null && _lastSnapshot == null)
                       || (world != null
                           && current != null
                           && _lastSnapshot != null
                           && current.StackSize == _lastSnapshot.StackSize
                           && current.Equals(world, _lastSnapshot));

            if (same)
            {
                return true;
            }
        }

        _lastSnapshot = current?.Clone();
        _initialized = true;
        return false;
    }
}
