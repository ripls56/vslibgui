using Gui.Core.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using SkiaSharp;

namespace Gui.Tests.Rendering;

[TestFixture]
public class ShaderMaskTests
{
    [Test]
    public void CreateRenderObject_ReturnsRenderProxyBox()
    {
        var widget = new ShaderMask(
            null,
            SKBlendMode.Multiply
        );
        var ro = widget.CreateRenderObject();
        Assert.That(ro, Is.Not.Null);
        Assert.That(ro, Is.InstanceOf<RenderProxyBox>());
    }

    [Test]
    public void UpdateRenderObject_SyncsProperties()
    {
        var shader = SKShader.CreateColor(SKColors.Red);
        var widget = new ShaderMask(
            shader,
            SKBlendMode.SrcOver
        );
        var ro = widget.CreateRenderObject();
        widget.UpdateRenderObject(ro);
        Assert.That(ro, Is.Not.Null);
        shader.Dispose();
    }

    [Test]
    public void ShaderMask_WithChild_CreatesElement()
    {
        var child = new SizedBox(10, 10);
        var widget = new ShaderMask(
            null,
            SKBlendMode.Multiply,
            child
        );
        var element = widget.CreateElement();
        Assert.That(element, Is.Not.Null);
    }
}
