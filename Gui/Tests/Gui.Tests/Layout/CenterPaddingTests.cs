using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;

namespace Gui.Tests.Layout;

[TestFixture]
public class CenterPaddingTests
{
    private static RenderObject LayoutWidget(Widget widget, LayoutConstraints constraints)
    {
        var buildOwner = new BuildOwner();
        var element = widget.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);
        buildOwner.BuildDirtyElements();

        var ro = element.RenderObject!;
        ro.Layout(constraints);
        return ro;
    }

    [Test]
    public void Padding_WrappingCenter_CentersChildInsidePaddedArea()
    {
        var child = new Container(new BoxStyle { Width = 50, Height = 50 });
        var widget = new Padding(EdgeInsets.All(20), new Center(child));

        var paddingRo = LayoutWidget(widget, LayoutConstraints.Tight(200, 200));

        var centerRo = paddingRo.Children[0];
        Assert.That(centerRo.X, Is.EqualTo(20), "Center should be offset by left padding");
        Assert.That(centerRo.Y, Is.EqualTo(20), "Center should be offset by top padding");
        Assert.That(centerRo.Size.X, Is.EqualTo(160), "Center should fill padded width");
        Assert.That(centerRo.Size.Y, Is.EqualTo(160), "Center should fill padded height");

        var childRo = centerRo.Children[0];
        Assert.That(childRo.X, Is.EqualTo(55), "Child centered in 160 wide area: (160-50)/2");
        Assert.That(childRo.Y, Is.EqualTo(55), "Child centered in 160 tall area: (160-50)/2");
    }

    [Test]
    public void Center_WrappingPadding_CentersPaddedChild()
    {
        var child = new Container(new BoxStyle { Width = 50, Height = 50 });
        var widget = new Center(new Padding(EdgeInsets.All(20), child));

        var centerRo = LayoutWidget(widget, LayoutConstraints.Tight(200, 200));

        Assert.That(centerRo.Size.X, Is.EqualTo(200));
        Assert.That(centerRo.Size.Y, Is.EqualTo(200));

        var paddingRo = centerRo.Children[0];
        Assert.That(paddingRo.Size.X, Is.EqualTo(90), "Padded box: 50 + 20 + 20");
        Assert.That(paddingRo.Size.Y, Is.EqualTo(90));
        Assert.That(paddingRo.X, Is.EqualTo(55), "Padding centered: (200-90)/2");
        Assert.That(paddingRo.Y, Is.EqualTo(55));

        var childRo = paddingRo.Children[0];
        Assert.That(childRo.X, Is.EqualTo(20), "Child offset by left padding");
        Assert.That(childRo.Y, Is.EqualTo(20), "Child offset by top padding");
    }

    [Test]
    public void SizedBox_Padding_Center_Stack_AspectRatio_ShrinksToAspectRatioWidth()
    {
        var widget = new SizedBox(
            520,
            260,
            new Padding(
                EdgeInsets.All(12),
                new Center(
                    new Stack([
                        new AspectRatio(
                            new Container(),
                            16f / 9f)
                    ]))));

        var sizedBoxRo = LayoutWidget(widget, LayoutConstraints.Tight(520, 260));

        var paddingRo = sizedBoxRo.Children[0];
        var centerRo = paddingRo.Children[0];
        var stackRo = centerRo.Children[0];

        Assert.That(centerRo.Size.X, Is.EqualTo(496), "Center fills padded width 520-24");
        Assert.That(centerRo.Size.Y, Is.EqualTo(236), "Center fills padded height 260-24");

        Assert.That(stackRo.Size.Y, Is.EqualTo(236).Within(0.5f),
            "Stack height limited by available 236");
        Assert.That(stackRo.Size.X, Is.EqualTo(236f * 16f / 9f).Within(0.5f),
            "Stack width shrinks to aspect ratio: 236 * 16/9 ~= 419, NOT 496");

        Assert.That(stackRo.X, Is.GreaterThan(0),
            "Stack centered horizontally because it is narrower than the padded area");
    }
}
