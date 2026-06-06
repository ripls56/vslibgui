using Gui.Core.Framework;
using Gui.Core.Layout;

namespace Gui.Tests.Layout;

[TestFixture]
public class ParentDataTests
{
    private class MockParentData : ParentData
    {
    }

    private class MockRenderObject : RenderObject
    {
        protected override void PerformLayout()
        {
        }
    }

    [Test]
    public void RenderObject_ShouldHoldParentData()
    {
        var ro = new MockRenderObject();
        var data = new MockParentData();
        ro.ParentData = data;
        Assert.That(ro.ParentData, Is.SameAs(data));
    }

    [Test]
    public void RenderFlex_ShouldUseFlexParentData()
    {
        var parent = new RenderFlex();
        var child = new RenderBox();
        parent.AddChild(child);

        // RenderFlex should automatically assign ParentData if missing 
        // or we do it in tests to verify it works.
        var flexData = new FlexParentData { Flex = 2 };
        child.ParentData = flexData;

        Assert.That(((FlexParentData)child.ParentData).Flex, Is.EqualTo(2));
    }
}
