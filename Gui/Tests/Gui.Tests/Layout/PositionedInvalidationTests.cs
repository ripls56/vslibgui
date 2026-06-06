using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;

namespace Gui.Tests.Layout;

[TestFixture]
public class PositionedInvalidationTests
{
    private static (RenderStack stack, RenderObject positioned) BuildStackWithPositioned(
        Positioned widget
    )
    {
        var ro = widget.CreateRenderObject();
        widget.UpdateRenderObject(ro);
        var stack = new RenderStack();
        stack.AddChild(ro);
        stack.Layout(LayoutConstraints.Tight(100, 100));
        stack.ResetDirtyFlags();
        return (stack, ro);
    }

    [Test]
    public void Height_Change_Marks_Parent_NeedsLayout()
    {
        var initial = new Positioned(
            bottom: 0, left: 0, right: 0, height: 20,
            child: new SizedBox()
        );
        var (stack, ro) = BuildStackWithPositioned(initial);

        var updated = new Positioned(
            bottom: 0, left: 0, right: 0, height: 40,
            child: new SizedBox()
        );
        updated.UpdateRenderObject(ro);

        Assert.That(stack.NeedsLayout, Is.True,
            "Parent stack should need layout after height change");
    }

    [Test]
    public void Same_Values_Does_Not_Mark_Parent()
    {
        var initial = new Positioned(
            bottom: 0, left: 0, right: 0, height: 20,
            child: new SizedBox()
        );
        var (stack, ro) = BuildStackWithPositioned(initial);

        var same = new Positioned(
            bottom: 0, left: 0, right: 0, height: 20,
            child: new SizedBox()
        );
        same.UpdateRenderObject(ro);

        Assert.That(stack.NeedsLayout, Is.False,
            "Parent stack should not need layout when values unchanged");
    }

    [Test]
    public void Top_Change_Marks_Parent_NeedsLayout()
    {
        var initial = new Positioned(
            top: 10, left: 0, child: new SizedBox()
        );
        var (stack, ro) = BuildStackWithPositioned(initial);

        var updated = new Positioned(
            top: 30, left: 0, child: new SizedBox()
        );
        updated.UpdateRenderObject(ro);

        Assert.That(stack.NeedsLayout, Is.True,
            "Parent stack should need layout after top change");
    }

    [Test]
    public void Left_Right_Change_Marks_Parent_NeedsLayout()
    {
        var initial = new Positioned(
            5, right: 5, child: new SizedBox()
        );
        var (stack, ro) = BuildStackWithPositioned(initial);

        var updated = new Positioned(
            10, right: 20, child: new SizedBox()
        );
        updated.UpdateRenderObject(ro);

        Assert.That(stack.NeedsLayout, Is.True,
            "Parent stack should need layout after left/right change");
    }

    [Test]
    public void Null_To_Value_Marks_Parent_NeedsLayout()
    {
        var initial = new Positioned(
            bottom: 0, left: 0, right: 0,
            child: new SizedBox()
        );
        var (stack, ro) = BuildStackWithPositioned(initial);

        var updated = new Positioned(
            bottom: 0, left: 0, right: 0, height: 30,
            child: new SizedBox()
        );
        updated.UpdateRenderObject(ro);

        Assert.That(stack.NeedsLayout, Is.True,
            "Parent stack should need layout when height goes from null to value");
    }

    [Test]
    public void Value_To_Null_Marks_Parent_NeedsLayout()
    {
        var initial = new Positioned(
            bottom: 0, left: 0, right: 0, height: 30,
            child: new SizedBox()
        );
        var (stack, ro) = BuildStackWithPositioned(initial);

        var updated = new Positioned(
            bottom: 0, left: 0, right: 0,
            child: new SizedBox()
        );
        updated.UpdateRenderObject(ro);

        Assert.That(stack.NeedsLayout, Is.True,
            "Parent stack should need layout when height goes from value to null");
    }
}
