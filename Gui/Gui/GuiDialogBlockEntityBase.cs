using System;
using System.Collections.Generic;
using Gui.Rendering;
using Gui.Shared;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Gui;

/// <summary>
///     Abstract base for dialogs bound to a block entity. Handles auto-close on
///     out-of-range, duplicate detection, packet tunneling, optional inventory
///     open/close synchronization, and optional floaty (world-anchored) positioning.
///     See <c>docs/dialogs.md</c> for usage.
/// </summary>
public abstract class GuiDialogBlockEntityBase : GuiBase, IClosableDialog
{
    /// <summary>Construct an inventory-less block-entity dialog.</summary>
    /// <param name="pos">The block entity position.</param>
    /// <param name="capi">The client API.</param>
    protected GuiDialogBlockEntityBase(BlockPos pos, ICoreClientAPI capi) : base(capi)
    {
        IsDuplicate = IsDuplicateOpen(
            capi.ModLoader.GetModSystem<GuiModSystem>().OpenWindows,
            pos);
        if (IsDuplicate)
        {
            return;
        }

        BlockEntityPos = pos;
    }

    /// <summary>Construct a block-entity dialog backed by an inventory.</summary>
    /// <param name="pos">The block entity position.</param>
    /// <param name="inventory">The inventory the dialog displays.</param>
    /// <param name="capi">The client API.</param>
    protected GuiDialogBlockEntityBase(
        BlockPos pos,
        InventoryBase? inventory,
        ICoreClientAPI capi) : base(capi)
    {
        IsDuplicate = inventory != null
                      && capi.World.Player.InventoryManager.Inventories.ContainsValue(inventory);
        if (!IsDuplicate)
        {
            IsDuplicate = IsDuplicateOpen(
                capi.ModLoader.GetModSystem<GuiModSystem>().OpenWindows,
                pos);
        }

        if (IsDuplicate)
        {
            return;
        }

        BlockEntityPos = pos;
        Inventory = inventory;
    }

    /// <summary>The block entity position this dialog is bound to. Null when duplicate.</summary>
    protected BlockPos? BlockEntityPos { get; }

    /// <summary>
    ///     The inventory associated with this block entity, or null if the dialog
    ///     has no inventory backing (e.g. read-only signs, packet-only widgets).
    ///     Null when duplicate.
    /// </summary>
    protected InventoryBase? Inventory { get; }

    /// <summary>
    ///     True when another <see cref="GuiDialogBlockEntityBase" /> on the same
    ///     block position is already open. <see cref="TryOpen" /> short-circuits
    ///     to false in that case.
    /// </summary>
    public bool IsDuplicate { get; }

    /// <summary>Sound played when the dialog opens. Null = silent.</summary>
    protected virtual SoundAttributes? OpenSound => null;

    /// <summary>Sound played when the dialog closes. Null = silent.</summary>
    protected virtual SoundAttributes? CloseSound => null;

    /// <summary>
    ///     Maximum distance in blocks the player may stand from the block before
    ///     the dialog auto-closes. Defaults to the player's pick range.
    /// </summary>
    protected virtual double InteractionRange =>
        capi.World.Player.WorldData.PickingRange;

    /// <summary>
    ///     Vertical world offset of the anchor above the block origin. Mirrors
    ///     vanilla <c>GuiDialogBlockEntity.FloatyDialogPosition</c> (default 0.75).
    /// </summary>
    protected virtual double FloatyDialogPosition => 0.75;

    /// <summary>
    ///     Vertical alignment of the dialog around the projected anchor point.
    ///     Mirrors vanilla <c>GuiDialogBlockEntity.FloatyDialogAlign</c>
    ///     (0 = top, 0.5 = centered, 1 = bottom; default 0.75).
    /// </summary>
    protected virtual double FloatyDialogAlign => 0.75;

    /// <summary>
    ///     World-anchored positioning. Defaults to a cached anchor on the bound
    ///     block (centered XZ, <see cref="FloatyDialogPosition" /> on Y) using
    ///     <see cref="FloatyDialogAlign" />. Active only when the player has
    ///     <c>immersiveMouseMode</c> enabled. Override to return a different
    ///     cached <see cref="WorldAnchor" /> instance (do NOT allocate per call),
    ///     or null to use plain screen-space positioning.
    /// </summary>
    protected virtual WorldAnchor? Anchor
    {
        get
        {
            if (BlockEntityPos == null)
            {
                return null;
            }

            return field ??= new WorldAnchor
            {
                WorldPos = new Vec3d(
                    BlockEntityPos.X + 0.5,
                    BlockEntityPos.Y + FloatyDialogPosition,
                    BlockEntityPos.Z + 0.5),
                Align = FloatyDialogAlign
            };
        }
    }

    /// <summary>
    ///     Raised once after the dialog has fully closed (after <see cref="OnGuiClosed" />'s
    ///     base call, inventory close, packet 1001 send, and close-sound playback have all run).
    ///     Subscribers see fully-closed state.
    /// </summary>
    public event Action? Closed;

    /// <summary>
    ///     Returns true if any dialog in <paramref name="openWindows" /> already
    ///     targets <paramref name="target" />.
    /// </summary>
    /// <param name="openWindows">List of currently open dialogs to scan.</param>
    /// <param name="target">The block position to check for an existing dialog.</param>
    internal static bool IsDuplicateOpen(
        IReadOnlyList<GuiBase> openWindows,
        BlockPos target)
    {
        for (var i = 0; i < openWindows.Count; i++)
        {
            if (openWindows[i] is GuiDialogBlockEntityBase dlg
                && dlg.BlockEntityPos != null
                && dlg.BlockEntityPos.Equals(target))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Returns true if the player is farther than <paramref name="range" /> from
    ///     the center of the target block. Returns false on null inputs.
    /// </summary>
    /// <param name="playerPos">The player's world position.</param>
    /// <param name="blockPos">The target block position.</param>
    /// <param name="range">Maximum interaction distance in blocks.</param>
    internal static bool IsOutOfRange(
        Vec3d? playerPos,
        BlockPos? blockPos,
        double range)
    {
        if (playerPos == null || blockPos == null)
        {
            return false;
        }

        var blockCenter = new Vec3d(
            blockPos.X + 0.5,
            blockPos.Y + 0.5,
            blockPos.Z + 0.5);
        return playerPos.DistanceTo(blockCenter) > range;
    }

    /// <summary>
    ///     Tunnels a packet through Vintage Story's block-entity packet path so the
    ///     server-side block entity handles routing. Safe to call after construction.
    /// </summary>
    /// <param name="payload">Application-defined payload object.</param>
    protected void SendBlockEntityPacket(object payload)
    {
        if (BlockEntityPos == null)
        {
            return;
        }

        capi.Network.SendBlockEntityPacket(
            BlockEntityPos.X,
            BlockEntityPos.InternalY,
            BlockEntityPos.Z,
            payload);
    }

    /// <summary>
    ///     Tunnels a packet ID with no payload (used for VS sentinel IDs like 1001).
    /// </summary>
    /// <param name="packetId">Packet ID.</param>
    protected void SendBlockEntityPacket(int packetId)
    {
        if (BlockEntityPos == null)
        {
            return;
        }

        capi.Network.SendBlockEntityPacket(BlockEntityPos, packetId);
    }

    /// <inheritdoc />
    public override bool TryOpen(bool withFocus)
    {
        if (IsDuplicate)
        {
            return false;
        }

        return base.TryOpen(withFocus);
    }

    /// <inheritdoc />
    public override void OnGuiOpened()
    {
        base.OnGuiOpened();
        if (Inventory != null)
        {
            capi.World.Player.InventoryManager.OpenInventory(Inventory);
        }

        if (OpenSound != null)
        {
            capi.Gui.PlaySound(OpenSound.Value);
        }
    }

    /// <inheritdoc />
    public override void OnGuiClosed()
    {
        if (Inventory != null)
        {
            capi.World.Player.InventoryManager.CloseInventoryAndSync(Inventory);
        }

        SendBlockEntityPacket(1001);
        if (CloseSound != null)
        {
            capi.Gui.PlaySound(CloseSound.Value);
        }

        base.OnGuiClosed();
        Closed?.Invoke();
    }

    /// <inheritdoc />
    public override void OnFinalizeFrame(float dt)
    {
        base.OnFinalizeFrame(dt);
        if (BlockEntityPos == null)
        {
            return;
        }

        var playerPos = capi.World.Player?.Entity?.Pos.XYZ;
        if (!IsOutOfRange(playerPos, BlockEntityPos, InteractionRange))
        {
            return;
        }

        capi.Event.EnqueueMainThreadTask(() => TryClose(), "closedlg-be");
    }

    /// <inheritdoc />
    public override void OnRenderGUI(float deltaTime)
    {
        var anchor = Anchor;
        if (anchor != null && capi.Settings.Bool["immersiveMouseMode"])
        {
            if (anchor.TryProject(
                    capi.Render.PerspectiveProjectionMat,
                    capi.Render.PerspectiveViewMat,
                    capi.Render.FrameWidth,
                    capi.Render.FrameHeight,
                    RuntimeEnv.GUIScale,
                    WindowSize,
                    out var projected))
            {
                WindowPos = projected;
            }
        }

        base.OnRenderGUI(deltaTime);
    }
}
