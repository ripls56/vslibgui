using System.Reflection;
using Gui.Tests.Helpers;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;

namespace Gui.Tests.Framework;

public class WidgetRenderObjectSyncTests
{
    [Test]
    public void Element_ShouldCreateRenderObjectOnMount()
    {
        var widget = new TestWidget(100, 100);
        var buildOwner = new BuildOwner();
        var element = widget.CreateElement();
        element.AssignOwner(buildOwner);

        Assert.That(element.RenderObject, Is.Null);
        element.Mount(null);
        Assert.That(element.RenderObject, Is.Not.Null);
    }

    [Test]
    public void ElementTree_ShouldSyncRenderObjectTree()
    {
        var child = new TestWidget(100, 100);
        var parent = new Container(new BoxStyle { Width = 500, Height = 500 }, child);

        var buildOwner = new BuildOwner();
        var rootElement = parent.CreateElement();
        rootElement.AssignOwner(buildOwner);
        rootElement.Mount(null);

        buildOwner.BuildDirtyElements();

        Assert.That(rootElement.RenderObject, Is.Not.Null);
        // Container -> ComponentElement -> _ContainerBox -> SingleChildElement -> child
        var containerBoxElement = GetChild(rootElement);
        var childElement = GetChild(containerBoxElement!);

        Assert.That(childElement!.RenderObject, Is.Not.Null);
        Assert.That(childElement.RenderObject!.Parent,
            Is.EqualTo(containerBoxElement!.RenderObject));
    }

    private Element? GetChild(Element element)
    {
        var field = element.GetType()
            .GetField("_child", BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(element) as Element;
    }
}
