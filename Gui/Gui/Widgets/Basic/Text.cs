using Gui.Core.Basic;
using Gui.Core.Framework;
using Gui.Rendering.Text;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Basic;

/// <summary>
///     A widget that displays text using direct Skia rendering.
/// </summary>
public class Text : RenderObjectWidget
{
    public Text(
        string content,
        TextStyle? style = null,
        Framework.Key? key = null
    ) : base(key)
    {
        Content = content;
        Style = style ?? new TextStyle();
    }

    public string Content { get; }
    public TextStyle Style { get; }

    public override RenderObject CreateRenderObject() => new RenderText();

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        var ro = (RenderText)renderObject;
        ro.Text = Content;
        ro.Style = Style;
    }
}
