using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

/// <summary>Fills remaining space in a <see cref="Flex" /> container along the main axis.</summary>
public class Spacer : StatelessWidget
{
    /// <summary>Creates a spacer with the given flex factor.</summary>
    public Spacer(int flex = 1, Framework.Key? key = null) : base(key)
    {
        Flex = flex;
    }

    /// <summary>Flex factor passed to the underlying <see cref="Expanded" />.</summary>
    public int Flex { get; }

    /// <inheritdoc />
    public override Widget Build(BuildContext context) => new Expanded(new SizedBox(), Flex);
}
