using Gui.Rendering;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;
using Gui.Widgets.Inventory;
using Gui.Widgets.Layout;
using SkiaSharp;

namespace Gui.Tests.Widgets;

[TestFixture]
public class ItemSlotRefactorTests
{
    private class MockTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(Action<TimeSpan> onTick) => new(onTick);
    }

    private static AnimationController MakeController() =>
        new(TimeSpan.FromMilliseconds(150), new MockTickerProvider());

    private class ReaderWidget : StatelessWidget
    {
        private readonly Func<BuildContext, Widget> _builder;

        public ReaderWidget(Func<BuildContext, Widget> builder)
        {
            _builder = builder;
        }

        public override Widget Build(BuildContext ctx) => _builder(ctx);
    }


    [Test]
    public void HoverData_UpdateShouldNotify_AlwaysReturnsTrue()
    {
        var ctrl = MakeController();
        var anim = new CurvedAnimation(ctrl, Curves.EaseOut);
        var data1 = new ItemSlotHoverData(anim, new SizedBox());
        var data2 = new ItemSlotHoverData(anim, new SizedBox());

        Assert.That(data1.UpdateShouldNotify(data2), Is.True);
    }

    [Test]
    public void HoverData_Of_ReturnsNull_WhenNoAncestor()
    {
        ItemSlotHoverData? captured = null;
        var built = false;

        var widget = new ReaderWidget(ctx =>
        {
            captured = ItemSlotHoverData.Of(ctx);
            built = true;
            return new SizedBox();
        });

        var owner = new BuildOwner();
        var el = widget.CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);

        Assert.That(built, Is.True);
        Assert.That(captured, Is.Null);
    }


    [Test]
    public void Overlay_WithNullSlot_DoesNotThrow() =>
        Assert.DoesNotThrow(() => new ItemSlotOverlay(null, 48f));

    [Test]
    public void Overlay_WithNullSlot_BuildsWithoutThrow()
    {
        var overlay = new ItemSlotOverlay(null, 48f);
        var owner = new BuildOwner();
        owner.SetTickerProvider(new MockTickerProvider());
        var el = overlay.CreateElement();
        el.AssignOwner(owner);
        Assert.DoesNotThrow(() => el.Mount(null));
    }


    [Test]
    public void GestureLayer_WithNullSlotAndController_DoesNotThrow() =>
        Assert.DoesNotThrow(() => new ItemSlotGestureLayer(() => new SizedBox()));

    [Test]
    public void GestureLayer_MountsWithoutThrow_WhenNoControllerOrSlot()
    {
        var layer = new ItemSlotGestureLayer(() => new SizedBox());
        var owner = new BuildOwner();
        owner.SetTickerProvider(new MockTickerProvider());
        var el = layer.CreateElement();
        el.AssignOwner(owner);
        Assert.DoesNotThrow(() => el.Mount(null));
    }


    [Test]
    public void FlatItemSlot_WithDefaults_DoesNotThrow() =>
        Assert.DoesNotThrow(() => new FlatItemSlot());

    [Test]
    public void FlatItemSlot_NullStyleUsesDefault()
    {
        var slot = new FlatItemSlot(style: null);
        Assert.That(slot.Style.Size, Is.EqualTo(ItemSlotStyle.Default.Size));
    }


    [Test]
    public void NineSliceItemSlot_WithNullSlotAndController_DoesNotThrow()
    {
        using var bitmap = new SKBitmap(1, 1);
        Assert.DoesNotThrow(() =>
            new NineSliceItemSlot(bitmap, EdgeInsets.All(1)));
    }

    [Test]
    public void NineSliceItemSlot_ExposesConstructorArgs()
    {
        using var bitmap = new SKBitmap(1, 1);
        var slice = EdgeInsets.All(10);
        var slot = new NineSliceItemSlot(bitmap, slice, 48f);

        Assert.That(slot.Size, Is.EqualTo(48f));
        Assert.That(slot.Slice, Is.EqualTo(slice));
    }
}
