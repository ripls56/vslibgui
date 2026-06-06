using System.Collections.Generic;
using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

public class Stack : MultiChildWidget
{
    public Stack(
        IEnumerable<Widget> children,
        Framework.Key? key = null
    ) : base(children, key)
    {
    }

    public override RenderObject CreateRenderObject() => new RenderStack();

    public override void UpdateRenderObject(
        RenderObject renderObject
    ) =>
        base.UpdateRenderObject(renderObject);
}
