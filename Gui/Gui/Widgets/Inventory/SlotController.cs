using System.Collections.Generic;
using Gui.Widgets.Framework;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace Gui.Widgets.Inventory;

/// <summary>
///     A <see cref="ChangeNotifier" /> that encapsulates all inventory slot
///     interaction logic: click, right-click, shift-click, and mouse-wheel
///     transfers. Holds the <see cref="ICoreClientAPI" /> reference and sends
///     network packets after each <c>ActivateSlot</c> call.
///     <para>
///         The controller also subscribes to <see cref="IInventory.SlotModified" />
///         on every inventory it watches, so widgets are automatically notified
///         when slots change externally (e.g. server-side moves, crafting output).
///         Call <see cref="WatchInventory" /> to start observing an inventory, and
///         <see cref="UnwatchInventory" /> (or <see cref="Dispose" />) to stop.
///     </para>
/// </summary>
public class SlotController : ChangeNotifier
{
    private readonly ICoreClientAPI _capi;

    private readonly Dictionary<ItemSlot, int>
        _dragAddedStackSize = new();

    private readonly List<(ItemSlot slot, int prevStackSize)>
        _dragVisitedSlots = new();

    private readonly HashSet<InventoryBase> _pausedInventories = new();

    private readonly HashSet<IInventory> _watched = new();

    private bool _isLeftDragActive;
    private bool _isRightDragActive;
    private ItemStack? _referenceDistributStack;

    public SlotController(
        ICoreClientAPI capi
    )
    {
        _capi = capi;
    }

    /// <summary>
    ///     The game world accessor, exposed for tooltip rendering
    ///     via <see cref="Vintagestory.API.Common.CollectibleObject.GetHeldItemInfo" />.
    /// </summary>
    public IWorldAccessor World => _capi.World;

    /// <summary>Returns true when the mouse inventory contains an item stack.</summary>
    public bool HasMouseItems => MouseSlot?.Itemstack != null;

    /// <summary>
    ///     Optional predicate that controls whether a slot may be clicked, wheeled,
    ///     or dragged into. Return <c>false</c> to suppress interaction for that slot.
    /// </summary>
    public System.Func<ItemSlot, bool>? CanClickSlot { get; set; }

    /// <summary>The mouse cursor inventory slot.</summary>
    private ItemSlot MouseSlot =>
        _capi.World.Player.InventoryManager.MouseItemSlot;

    /// <summary>The item stack in the slot currently hovered by the pointer, or <c>null</c>.</summary>
    public ItemStack? HoveredItemStack =>
        _capi.World.Player.InventoryManager.CurrentHoveredSlot?.Itemstack;

    /// <summary>
    ///     Registers <paramref name="slot" /> as the currently hovered slot in VS's inventory manager
    ///     and suppresses the vanilla HUD tooltip. Call when the pointer enters a slot so that
    ///     VS hotkeys (e.g. drop key) operate on the correct slot.
    /// </summary>
    public void EnterSlot(ItemSlot slot)
    {
        _capi.World.Player.InventoryManager.CurrentHoveredSlot = slot;
        HudMouseTools.showTooltip = false;
    }

    /// <summary>
    ///     Clears the currently hovered slot in VS's inventory manager and restores the vanilla
    ///     HUD tooltip. Call when the pointer leaves a slot.
    /// </summary>
    public void LeaveSlot()
    {
        _capi.World.Player.InventoryManager.CurrentHoveredSlot = null;
        HudMouseTools.showTooltip = true;
    }

    /// <summary>
    ///     Begins observing <paramref name="inventory" /> for
    ///     <see cref="IInventory.SlotModified" /> events. When any slot
    ///     in the inventory changes, all listeners are notified so that
    ///     bound widgets can rebuild.
    /// </summary>
    public void WatchInventory(
        IInventory inventory
    )
    {
        if (!_watched.Add(inventory))
        {
            return;
        }

        inventory.SlotModified += OnSlotModified;
    }

    /// <summary>Stops observing <paramref name="inventory" />.</summary>
    public void UnwatchInventory(
        IInventory inventory
    )
    {
        if (!_watched.Remove(inventory))
        {
            return;
        }

        inventory.SlotModified -= OnSlotModified;
    }

    /// <summary>
    ///     Activates <paramref name="slot" /> as if the player clicked it with
    ///     <paramref name="button" />. Reads modifier keys from the current
    ///     keyboard state, constructs an <see cref="ItemStackMoveOperation" />,
    ///     calls <see cref="IInventory.ActivateSlot" />, and sends the
    ///     resulting packet to the server.
    /// </summary>
    public void ClickSlot(
        ItemSlot slot,
        EnumMouseButton button,
        IInventory? outerInventory = null
    )
    {
        if (slot.Inventory == null)
        {
            return;
        }

        if (CanClickSlot != null && !CanClickSlot(slot))
        {
            return;
        }

        var modifiers = ReadModifiers();
        var mouse = MouseSlot;
        var slotInv = slot.Inventory;
        var slotId = slotInv.GetSlotId(slot);
        var activateInv = outerInventory ?? slotInv;

        var wasMouseEmpty = mouse.Itemstack == null;
        var prevCollectible = mouse.Itemstack?.Collectible;

        var op = new ItemStackMoveOperation(
            _capi.World,
            button,
            modifiers,
            EnumMergePriority.AutoMerge
        ) { ActingPlayer = _capi.World.Player };

        object packet;
        var shiftPressed = (modifiers & EnumModifierKey.SHIFT) != 0;

        if (shiftPressed)
        {
            op.RequestedQuantity = slot.StackSize;
            packet = activateInv.ActivateSlot(
                slotId,
                slot,
                ref op
            );
        }
        else
        {
            op.CurrentPriority = EnumMergePriority.DirectMerge;
            packet = activateInv.ActivateSlot(
                slotId,
                mouse,
                ref op
            );
        }

        SendPacket(packet);
        PlaySlotSound(wasMouseEmpty, prevCollectible);
        NotifyListeners();
    }

    /// <summary>
    ///     Activates <paramref name="slot" /> with a mouse-wheel transfer,
    ///     moving one item at a time between the slot and the mouse inventory.
    ///     Returns <c>true</c> when the wheel was consumed (a transfer was
    ///     issued), <c>false</c> otherwise. Callers use the return value to
    ///     decide whether to mark the underlying <c>PointerEvent</c> handled
    ///     so unhandled wheel events bubble up to ancestor scrollables.
    /// </summary>
    public bool WheelSlot(
        ItemSlot slot,
        int wheelDir,
        IInventory? outerInventory = null
    )
    {
        if (slot.Inventory == null)
        {
            return false;
        }

        if (CanClickSlot != null && !CanClickSlot(slot))
        {
            return false;
        }

        var mods = ReadModifiers();
        if ((mods & EnumModifierKey.SHIFT) == 0 &&
            (mods & EnumModifierKey.CTRL) == 0)
        {
            return false;
        }

        var slotInv = slot.Inventory;
        var slotId = slotInv.GetSlotId(slot);
        var activateInv = outerInventory ?? slotInv;

        var op = new ItemStackMoveOperation(
            _capi.World,
            EnumMouseButton.Wheel,
            0,
            EnumMergePriority.AutoMerge,
            1
        ) { WheelDir = wheelDir, ActingPlayer = _capi.World.Player };

        var packet = activateInv.ActivateSlot(
            slotId,
            MouseSlot,
            ref op
        );

        SendPacket(packet);
        NotifyListeners();
        return true;
    }

    private void PlaySlotSound(
        bool wasMouseEmpty,
        CollectibleObject? prevCollectible
    )
    {
        var mouse = MouseSlot;
        var isMouseEmpty = mouse.Itemstack == null;
        var curCollectible = mouse.Itemstack?.Collectible;

        if (wasMouseEmpty && !isMouseEmpty)
        {
            var sound = curCollectible?.HeldSounds?.InvPickup
                        ?? HeldSounds.InvPickUpDefault;
            _capi.World.PlaySound(sound);
        }
        else if (!wasMouseEmpty &&
                 (isMouseEmpty || curCollectible?.Id != prevCollectible?.Id))
        {
            var sound = prevCollectible?.HeldSounds?.InvPlace
                        ?? HeldSounds.InvPlaceDefault;
            _capi.World.PlaySound(sound);
        }
    }

    /// <summary>
    ///     Begins a drag operation. Called <em>before</em>
    ///     <see cref="ClickSlot" /> so that the mouse inventory state is
    ///     captured while items are still on the cursor. Only activates
    ///     when the mouse already holds items — picking up from an empty
    ///     cursor does not start a drag.
    ///     <para>
    ///         For left-drag, pauses network updates on the initial slot's
    ///         inventory and the mouse inventory so that intermediate
    ///         per-slot packets are batched until <see cref="EndDrag" />.
    ///     </para>
    /// </summary>
    public void BeginDrag(
        EnumMouseButton button,
        ItemSlot initialSlot
    )
    {
        var mouseStack = MouseSlot.Itemstack;
        if (mouseStack == null)
        {
            return;
        }

        if (CanClickSlot != null && !CanClickSlot(initialSlot))
        {
            return;
        }

        _dragVisitedSlots.Clear();
        _dragAddedStackSize.Clear();
        _dragVisitedSlots.Add((initialSlot, initialSlot.StackSize));
        _referenceDistributStack = mouseStack.Clone();

        if (button == EnumMouseButton.Left)
        {
            _isLeftDragActive = true;
            PauseInventory(initialSlot.Inventory);
            PauseInventory(MouseSlot.Inventory);
        }
        else if (button == EnumMouseButton.Right)
        {
            _isRightDragActive = true;
        }
    }

    /// <summary>
    ///     Called when the pointer enters a new slot during an active drag.
    ///     For right-drag, places one item per slot. For left-drag,
    ///     distributes items evenly across all visited slots.
    /// </summary>
    public void DragEnterSlot(
        ItemSlot slot,
        IInventory? outerInventory = null
    )
    {
        if (slot.Inventory == null)
        {
            return;
        }

        if (!_isLeftDragActive && !_isRightDragActive)
        {
            return;
        }

        if (CanClickSlot != null && !CanClickSlot(slot))
        {
            return;
        }

        var mouse = MouseSlot;

        if (_isRightDragActive && mouse.Itemstack != null)
        {
            if (IsAlreadyVisited(slot))
            {
                return;
            }

            if (!IsCompatibleSlot(slot, mouse.Itemstack))
            {
                return;
            }

            _dragVisitedSlots.Add((slot, slot.StackSize));
            ClickSlot(slot, EnumMouseButton.Right, outerInventory);
        }
        else if (_isLeftDragActive)
        {
            if (IsAlreadyVisited(slot))
            {
                return;
            }

            if (_referenceDistributStack == null)
            {
                return;
            }

            if (!IsCompatibleSlot(slot, _referenceDistributStack))
            {
                return;
            }

            PauseInventory(slot.Inventory);

            var prevSize = slot.StackSize;
            _dragVisitedSlots.Add((slot, prevSize));

            if (mouse.StackSize > 0)
            {
                ClickSlot(slot, EnumMouseButton.Left, outerInventory);
                _dragAddedStackSize[slot] = slot.StackSize - prevSize;
            }

            if (mouse.Itemstack == null || mouse.StackSize <= 0)
            {
                RedistributeStacks();
            }
        }
    }

    /// <summary>
    ///     Ends the current drag operation, resumes network updates on all
    ///     paused inventories, and clears all drag state.
    /// </summary>
    public void EndDrag()
    {
        foreach (var inv in _pausedInventories)
        {
            inv.InvNetworkUtil.PauseInventoryUpdates = false;
        }

        _pausedInventories.Clear();

        _isLeftDragActive = false;
        _isRightDragActive = false;
        _referenceDistributStack = null;
        _dragVisitedSlots.Clear();
        _dragAddedStackSize.Clear();
        NotifyListeners();
    }

    private void PauseInventory(
        IInventory? inventory
    )
    {
        if (inventory is InventoryBase b && _pausedInventories.Add(b))
        {
            b.InvNetworkUtil.PauseInventoryUpdates = true;
        }
    }

    private bool IsAlreadyVisited(
        ItemSlot slot
    )
    {
        for (var i = 0; i < _dragVisitedSlots.Count; i++)
        {
            if (_dragVisitedSlots[i].slot == slot)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsCompatibleSlot(
        ItemSlot slot,
        ItemStack reference
    )
    {
        if (slot.Itemstack == null)
        {
            return true;
        }

        return slot.Itemstack.Equals(
            _capi.World,
            reference,
            GlobalConstants.IgnoredStackAttributes
        );
    }

    private void RedistributeStacks()
    {
        if (_referenceDistributStack == null ||
            _dragVisitedSlots.Count <= 1)
        {
            return;
        }

        var total = _referenceDistributStack.StackSize;
        var perSlot = total / _dragVisitedSlots.Count;
        var lastSlot = _dragVisitedSlots[^1].slot;

        for (var i = 0; i < _dragVisitedSlots.Count - 1; i++)
        {
            var (slot, prevSize) = _dragVisitedSlots[i];
            if (slot == lastSlot)
            {
                continue;
            }

            var added = slot.StackSize - prevSize;
            if (added <= perSlot)
            {
                continue;
            }

            var excess = added - perSlot;
            var op = new ItemStackMoveOperation(
                _capi.World,
                EnumMouseButton.Left,
                0,
                EnumMergePriority.AutoMerge
            ) { ActingPlayer = _capi.World.Player, RequestedQuantity = excess };
            var packet = _capi.World.Player.InventoryManager
                .TryTransferTo(slot, lastSlot, ref op);
            SendPacket(packet);
        }

        NotifyListeners();
    }

    private void OnSlotModified(
        int slotId
    )
    {
        _capi.Event.EnqueueMainThreadTask(
            NotifyListeners,
            "SlotController.OnSlotModified"
        );
    }

    private EnumModifierKey ReadModifiers()
    {
        var keys = _capi.Input.KeyboardKeyState;
        EnumModifierKey m = 0;
        if (keys[(int)GlKeys.ShiftLeft] || keys[(int)GlKeys.ShiftRight])
        {
            m |= EnumModifierKey.SHIFT;
        }

        if (keys[(int)GlKeys.ControlLeft] || keys[(int)GlKeys.ControlRight])
        {
            m |= EnumModifierKey.CTRL;
        }

        if (keys[(int)GlKeys.AltLeft] || keys[(int)GlKeys.AltRight])
        {
            m |= EnumModifierKey.ALT;
        }

        return m;
    }

    private void SendPacket(
        object? packet
    )
    {
        if (packet == null)
        {
            return;
        }

        if (packet is object[] packets)
        {
            for (var i = 0; i < packets.Length; i++)
            {
                _capi.Network.SendPacketClient(packets[i]);
            }
        }
        else
        {
            _capi.Network.SendPacketClient(packet);
        }
    }

    /// <summary>
    ///     Disposes the controller, unsubscribing from all watched
    ///     inventories and clearing listeners.
    /// </summary>
    public override void Dispose()
    {
        foreach (var inv in _watched)
        {
            inv.SlotModified -= OnSlotModified;
        }

        _watched.Clear();

        foreach (var inv in _pausedInventories)
        {
            inv.InvNetworkUtil.PauseInventoryUpdates = false;
        }

        _pausedInventories.Clear();

        base.Dispose();
    }
}
