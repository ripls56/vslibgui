using Gui.Core.Layout;
using Gui.Widgets.Layout;

namespace Gui.Tests.Layout;

[TestFixture]
public class FlexTests
{
    [Test]
    public void Row_CreatesRenderFlex_WithHorizontalDirection()
    {
        var row = new Row();
        var ro = row.CreateRenderObject() as RenderFlex;
        Assert.That(ro, Is.Not.Null);
        Assert.That(ro!.Direction, Is.EqualTo(FlexDirection.Horizontal));
    }

    [Test]
    public void Column_CreatesRenderFlex_WithVerticalDirection()
    {
        var col = new Column();
        var ro = col.CreateRenderObject() as RenderFlex;
        Assert.That(ro, Is.Not.Null);
        Assert.That(ro!.Direction, Is.EqualTo(FlexDirection.Vertical));
    }

    [Test]
    public void Row_PassesAllProperties()
    {
        var row = new Row(
            8,
            MainAxisAlignment.Center,
            CrossAxisAlignment.End);
        var ro = row.CreateRenderObject() as RenderFlex;
        Assert.That(ro!.Spacing, Is.EqualTo(8));
        Assert.That(ro.MainAxisAlignment,
            Is.EqualTo(MainAxisAlignment.Center));
        Assert.That(ro.CrossAxisAlignment,
            Is.EqualTo(CrossAxisAlignment.End));
    }

    [Test]
    public void Column_PassesAllProperties()
    {
        var col = new Column(
            12,
            MainAxisAlignment.SpaceBetween,
            CrossAxisAlignment.Stretch);
        var ro = col.CreateRenderObject() as RenderFlex;
        Assert.That(ro!.Spacing, Is.EqualTo(12));
        Assert.That(ro.MainAxisAlignment,
            Is.EqualTo(MainAxisAlignment.SpaceBetween));
        Assert.That(ro.CrossAxisAlignment,
            Is.EqualTo(CrossAxisAlignment.Stretch));
    }

    [Test]
    public void UpdateRenderObject_SyncsAllProperties()
    {
        var row = new Row(5,
            MainAxisAlignment.End);
        var ro = new RenderFlex();
        row.UpdateRenderObject(ro);
        Assert.That(ro.Spacing, Is.EqualTo(5));
        Assert.That(ro.MainAxisAlignment,
            Is.EqualTo(MainAxisAlignment.End));
        Assert.That(ro.Direction,
            Is.EqualTo(FlexDirection.Horizontal));
    }
}
