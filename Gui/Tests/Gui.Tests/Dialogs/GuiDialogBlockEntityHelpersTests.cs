using Vintagestory.API.MathTools;

namespace Gui.Tests.Dialogs;

public class GuiDialogBlockEntityHelpersTests
{
    [Test]
    public void IsDuplicateOpen_EmptyList_ReturnsFalse()
    {
        var pos = new BlockPos(10, 64, 20);
        Assert.That(
            GuiDialogBlockEntityBase.IsDuplicateOpen(new List<GuiBase>(), pos),
            Is.False);
    }

    [Test]
    public void IsOutOfRange_NullPlayer_ReturnsFalse()
    {
        var block = new BlockPos(0, 0, 0);
        Assert.That(
            GuiDialogBlockEntityBase.IsOutOfRange(null, block, 8.0),
            Is.False);
    }

    [Test]
    public void IsOutOfRange_NullBlock_ReturnsFalse()
    {
        var player = new Vec3d(0, 0, 0);
        Assert.That(
            GuiDialogBlockEntityBase.IsOutOfRange(player, null, 8.0),
            Is.False);
    }

    [Test]
    public void IsOutOfRange_WithinRange_ReturnsFalse()
    {
        // Block center at (0.5, 0.5, 0.5). Player at (5, 0.5, 0.5) → distance 4.5.
        var player = new Vec3d(5, 0.5, 0.5);
        var block = new BlockPos(0, 0, 0);
        Assert.That(
            GuiDialogBlockEntityBase.IsOutOfRange(player, block, 8.0),
            Is.False);
    }

    [Test]
    public void IsOutOfRange_BeyondRange_ReturnsTrue()
    {
        // Block center at (0.5, 0.5, 0.5). Player at (20, 0.5, 0.5) → distance 19.5.
        var player = new Vec3d(20, 0.5, 0.5);
        var block = new BlockPos(0, 0, 0);
        Assert.That(
            GuiDialogBlockEntityBase.IsOutOfRange(player, block, 8.0),
            Is.True);
    }

    [Test]
    public void IsOutOfRange_OnExactBoundary_ReturnsFalse()
    {
        // Distance exactly equal to range — strictly greater check, so within.
        var player = new Vec3d(8.5, 0.5, 0.5); // distance to (0.5, 0.5, 0.5) = 8.0
        var block = new BlockPos(0, 0, 0);
        Assert.That(
            GuiDialogBlockEntityBase.IsOutOfRange(player, block, 8.0),
            Is.False);
    }
}
