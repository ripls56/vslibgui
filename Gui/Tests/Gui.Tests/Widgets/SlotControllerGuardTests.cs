using Gui.Widgets.Inventory;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gui.Tests.Widgets;

[TestFixture]
public class SlotControllerGuardTests
{
    [Test]
    public void IsActivatable_False_WhenInvNetworkUtilNull()
    {
        var inv = new UninitializedInventory();
        Assert.That(SlotController.IsActivatable(inv, 0), Is.False);
    }

    [Test]
    public void IsActivatable_False_WhenSlotIdNegative()
    {
        var inv = new UninitializedInventory { InvNetworkUtil = new StubNetworkUtil() };
        Assert.That(SlotController.IsActivatable(inv, -1), Is.False);
    }

    [Test]
    public void IsActivatable_True_WhenNetworkReadyAndSlotIdValid()
    {
        var inv = new UninitializedInventory { InvNetworkUtil = new StubNetworkUtil() };
        Assert.That(SlotController.IsActivatable(inv, 0), Is.True);
    }

    private sealed class UninitializedInventory : InventoryBase
    {
        private readonly ItemSlot _slot;

        public UninitializedInventory() : base("test", "1", null)
        {
            _slot = new ItemSlot(this);
        }

        public override int Count => 1;

        public override ItemSlot this[int slotId]
        {
            get => _slot;
            set { }
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
        }
    }

    private sealed class StubNetworkUtil : IInventoryNetworkUtil
    {
        public ICoreAPI Api { get; set; } = null!;
        public bool PauseInventoryUpdates { get; set; }

        public object GetActivateSlotPacket(int slotId, ItemStackMoveOperation op) =>
            throw new System.NotSupportedException();

        public object GetFlipSlotsPacket(IInventory sourceInv, int sourceSlotId, int targetSlotId) =>
            throw new System.NotSupportedException();

        public void HandleClientPacket(IPlayer byPlayer, int packetId, byte[] data) =>
            throw new System.NotSupportedException();

        public void SendInventoryRollback(
            Vintagestory.API.Server.IServerPlayer player,
            int slotId,
            System.ReadOnlySpan<byte> data) =>
            throw new System.NotSupportedException();

        public object DidOpen(IPlayer player) => throw new System.NotSupportedException();

        public object DidClose(IPlayer player) => throw new System.NotSupportedException();
    }
}
