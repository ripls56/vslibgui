using Gui.Core.Framework;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Tests.Helpers;

public class TestWidget : SingleChildWidget
{
    private readonly float _initialHeight;
    private readonly float _initialWidth;

    public TestWidget(float width = 0, float height = 0)
    {
        _initialWidth = width;
        _initialHeight = height;
    }

    public override RenderObject CreateRenderObject() => new RenderBox();

    public override void UpdateRenderObject(RenderObject renderObject)
    {
        base.UpdateRenderObject(renderObject);
        if (renderObject is RenderBox box)
        {
            // Initial hint for tests
            if (_initialWidth > 0 || _initialHeight > 0)
            {
                box.Size = new Vector2(_initialWidth, _initialHeight);
            }
            else
            {
                // Predictable default for tests
                box.Size = new Vector2(100, 100);
            }
        }
    }
}
