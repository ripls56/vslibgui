using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Tests.Helpers;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Tests.Layout;

public class CoordinateTests
{
    [Test]
    public void AbsoluteCoordinates_ShouldPropagateCorrectly()
    {
        var child = new TestWidget(50, 50);
        var padding = new Padding(EdgeInsets.Only(10, 20), child);
        var root = new RootWidget(100, 100, padding);

        var buildOwner = new BuildOwner();
        var rootElement = root.CreateElement();
        rootElement.AssignOwner(buildOwner);
        rootElement.Mount(null);

        buildOwner.BuildDirtyElements();

        var rootRo = rootElement.RenderObject!;
        rootRo.Layout(LayoutConstraints.Tight(500, 500));

        var paddingRo = rootRo.Children[0];
        var childRo = paddingRo.Children[0];

        // Padding is at (0,0) relative to root, so (100,100) absolute
        Assert.That(paddingRo.LocalToGlobal(Vector2.Zero).X, Is.EqualTo(100));
        Assert.That(paddingRo.LocalToGlobal(Vector2.Zero).Y, Is.EqualTo(100));

        // Child is at (10,20) relative to padding, so (110,120) absolute
        Assert.That(childRo.LocalToGlobal(Vector2.Zero).X, Is.EqualTo(110));
        Assert.That(childRo.LocalToGlobal(Vector2.Zero).Y, Is.EqualTo(120));
    }

    private class RootWidget : SingleChildWidget
    {
        public readonly float InitialX;
        public readonly float InitialY;

        public RootWidget(float x, float y, Widget child) : base(child)
        {
            InitialX = x;
            InitialY = y;
        }

        public override RenderObject CreateRenderObject() =>
            new RenderProxyBox { X = InitialX, Y = InitialY };
    }
}
