using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Gui.Shared;

/// <summary>
///     Sends the open/close packets for a player-owned inventory (backpack,
///     crafting grid, creative). Used by player-inventory dialogs that don't
///     live inside <see cref="GuiDialogBlockEntityBase" />'s
///     <c>InventoryManager.OpenInventory</c> lifecycle.
/// </summary>
public static class OwnInventoryNetworking
{
    /// <summary>Sends an Open packet for <paramref name="inv" />; null-safe.</summary>
    public static void SendOpen(ICoreClientAPI capi, IInventory? inv)
    {
        if (inv == null)
        {
            return;
        }

        var packet = inv.Open(capi.World.Player);
        if (packet != null)
        {
            capi.Network.SendPacketClient(packet);
        }
    }

    /// <summary>Sends a Close packet for <paramref name="inv" />; null-safe.</summary>
    public static void SendClose(ICoreClientAPI capi, IInventory? inv)
    {
        if (inv == null)
        {
            return;
        }

        var packet = inv.Close(capi.World.Player);
        if (packet != null)
        {
            capi.Network.SendPacketClient(packet);
        }
    }
}
