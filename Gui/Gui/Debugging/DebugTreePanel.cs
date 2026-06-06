using System.Linq;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Scroll;
using OpenTK.Mathematics;

namespace Gui.Debugging;

/// <summary>
///     Renders a scrollable widget/element/render-object tree dump using
///     <see cref="TreeVisitor.GetTreeDebugStrings" />.
/// </summary>
public class DebugTreePanel : StatelessWidget
{
    private static readonly TextStyle LineStyle = new()
    {
        FontFamily = "monospace", FontSize = 10, Color = Vector4.One, SoftWrap = false
    };

    private readonly Element? _root;

    /// <summary>Initializes a new <see cref="DebugTreePanel" />.</summary>
    /// <param name="root">Root element of the tree to display, or <see langword="null" />.</param>
    public DebugTreePanel(Element? root)
    {
        _root = root;
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        if (_root == null)
        {
            return new Text("No active UI", new TextStyle { FontSize = 11, Color = Vector4.One });
        }

        var lines = TreeVisitor.GetTreeDebugStrings(_root);
        var lineWidgets = lines.Select(line => (Widget)new Text(line, LineStyle));

        return new SizedBox(
            height: 300,
            child: new ListView(lineWidgets, 14f)
        );
    }
}
