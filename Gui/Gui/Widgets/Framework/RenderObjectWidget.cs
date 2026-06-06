namespace Gui.Widgets.Framework;

public abstract class RenderObjectWidget : Widget
{
    protected RenderObjectWidget(
        Key? key = null
    ) : base(key)
    {
    }

    public override Element CreateElement() => new RenderObjectElement(this);
}
