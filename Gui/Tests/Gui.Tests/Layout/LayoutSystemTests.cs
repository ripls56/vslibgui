using Gui.Rendering;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;

namespace Gui.Tests.Layout;

[TestFixture]
public class LayoutSystemTests
{
    [Test]
    public void Padding_Asymmetric_ShouldPositionChildCorrectly()
    {
        var buildOwner = new BuildOwner();
        var child = new Container(new BoxStyle { Width = 100, Height = 100 });
        // Padding: Left=10, Top=20, Right=30, Bottom=40
        var padding = new Padding(EdgeInsets.Only(10, 20, 30, 40), child);

        var element = padding.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);
        buildOwner.BuildDirtyElements();

        var ro = element.RenderObject!;
        ro.Layout(LayoutConstraints.Loose(500, 500));

        // Size should be child size + horizontal/vertical padding
        // 100 + 10 + 30 = 140
        // 100 + 20 + 40 = 160
        Assert.That(ro.Size.X, Is.EqualTo(140));
        Assert.That(ro.Size.Y, Is.EqualTo(160));

        var childRo = ro.Children[0];
        Assert.That(childRo.X, Is.EqualTo(10));
        Assert.That(childRo.Y, Is.EqualTo(20));
    }

    [Test]
    public void Column_MainAxisAlignment_Center_ShouldCenterChildren()
    {
        var buildOwner = new BuildOwner();
        var child1 = new Container(new BoxStyle { Width = 50, Height = 50 });
        var child2 = new Container(new BoxStyle { Width = 50, Height = 50 });

        var column = new Column(
            mainAxisAlignment: MainAxisAlignment.Center,
            children: [child1, child2]
        );

        var element = column.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);
        buildOwner.BuildDirtyElements();

        var ro = element.RenderObject!;
        // Total child height = 50 + 50 = 100. Container height = 200.
        // Centered position start = (200 - 100) / 2 = 50.
        ro.Layout(LayoutConstraints.Tight(100, 200));

        Assert.That(ro.Children[0].Y, Is.EqualTo(50));
        Assert.That(ro.Children[1].Y, Is.EqualTo(100));
    }

    [Test]
    public void Row_CrossAxisAlignment_Stretch_ShouldForceChildSize()
    {
        var buildOwner = new BuildOwner();
        var child = new Container(); // No intrinsic size

        var row = new Row(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children: [child]
        );

        var element = row.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);
        buildOwner.BuildDirtyElements();

        var ro = element.RenderObject!;
        ro.Layout(LayoutConstraints.Tight(200, 100));

        // Child should be stretched to height 100
        Assert.That(ro.Children[0].Size.Y, Is.EqualTo(100));
    }
}
