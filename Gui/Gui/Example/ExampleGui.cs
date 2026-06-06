using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;
using Vintagestory.API.Client;

namespace Gui.Example;

/// <summary>
///     Interactive libGUI showcase. Open with <c>.ui showcase</c>.
/// </summary>
public class ExampleGui(ICoreClientAPI capi) : GuiBase(capi)
{
    public override bool Focusable => true;

    protected override WindowConfig CreateWindowConfig() => new() { Size = new Vector2(1080, 660) };

    protected override Widget Build() =>
        new WindowFrame("Example", new ExampleContent(capi), fillHeight: true);
}
