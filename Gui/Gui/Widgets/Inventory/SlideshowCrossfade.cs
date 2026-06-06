using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using Vintagestory.API.Common;

namespace Gui.Widgets.Inventory;

/// <summary>
///     Renders a crossfade between two <see cref="ItemStack" /> icons, blending
///     <see cref="Outgoing" /> (opacity = 1 − <see cref="T" />) and
///     <see cref="Incoming" /> (opacity = <see cref="T" />) via a <see cref="Stack" />.
/// </summary>
public class SlideshowCrossfade : StatelessWidget
{
    /// <summary>Creates a crossfade widget.</summary>
    /// <param name="outgoing">Stack fading out.</param>
    /// <param name="incoming">Stack fading in.</param>
    /// <param name="t">Blend factor in [0, 1]; 0 = fully outgoing, 1 = fully incoming.</param>
    /// <param name="size">Icon width and height in pixels.</param>
    public SlideshowCrossfade(ItemStack outgoing, ItemStack incoming, float t, float size)
    {
        Outgoing = outgoing;
        Incoming = incoming;
        T = t;
        Size = size;
    }

    /// <summary>The stack fading out.</summary>
    public ItemStack Outgoing { get; }

    /// <summary>The stack fading in.</summary>
    public ItemStack Incoming { get; }

    /// <summary>Blend factor in [0, 1]; 0 = fully outgoing, 1 = fully incoming.</summary>
    public float T { get; }

    /// <summary>Icon width and height in pixels.</summary>
    public float Size { get; }

    /// <inheritdoc />
    public override Widget Build(BuildContext context) =>
        new Stack([
            new Opacity(1f - T, new ItemStackDisplay(Outgoing, Size, Size, (int)(Size * 2))),
            new Opacity(T, new ItemStackDisplay(Incoming, Size, Size, (int)(Size * 2)))
        ]);
}
