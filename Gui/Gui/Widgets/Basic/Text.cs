using Gui.Core.Basic;
using Gui.Core.Framework;
using Gui.Rendering.Text;
using Gui.Widgets.Basic.Theming;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Basic;

public class Text : StatelessWidget
{
    public Text
    (
        string content,
        TextStyle? style = null,
        Framework.Key? key = null
    ) : base(key)
    {
        Content = content;
        StyleOverride = style;
    }

    public string Content { get; }
    public TextStyle? StyleOverride { get; }
    public TextStyle Style => StyleOverride ?? new TextStyle();

    public override Widget Build(BuildContext context)
    {
        var inherited = DefaultTextStyle.Of(context);
        var effectiveStyle = StyleOverride?.Merge(inherited) ?? inherited;
        return new _RawText(Content, effectiveStyle);
    }
}

internal class _RawText : RenderObjectWidget
{
    public _RawText(string content, TextStyle style)
    {
        Content = content;
        Style = style;
    }

    public string Content { get; }
    public TextStyle Style { get; }

    public override RenderObject CreateRenderObject() => new RenderText();

    public override void UpdateRenderObject(RenderObject renderObject)
    {
        var ro = (RenderText)renderObject;
        ro.Text = Content;
        ro.Style = Style;
    }
}
