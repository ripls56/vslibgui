using System.Collections.Generic;
using System.Text;
using Gui.Core.Layout;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Overlay;
using Vintagestory.API.Common;

namespace Gui.Widgets.Inventory;

/// <summary>
///     Displays item information inside a tooltip bubble, replicating the
///     vanilla Vintage Story item tooltip layout: item name in gold, an
///     item icon, and the full description produced by
///     <see cref="CollectibleObject.GetHeldItemInfo" />.
///     Designed to be passed as the <c>content</c> parameter of
///     <see cref="Tooltip" />.
/// </summary>
public class ItemTooltipContent : StatelessWidget
{
    /// <summary>Maximum tooltip width in pixels.</summary>
    private const float MaxWidth = 350f;

    /// <summary>
    ///     Creates a new <see cref="ItemTooltipContent" />.
    /// </summary>
    public ItemTooltipContent(
        ItemSlot slot,
        IWorldAccessor world
    )
    {
        Slot = slot;
        World = world;
    }

    /// <summary>The inventory slot containing the item to describe.</summary>
    public ItemSlot Slot { get; }

    /// <summary>
    ///     The world accessor needed by
    ///     <see cref="CollectibleObject.GetHeldItemInfo" />.
    /// </summary>
    public IWorldAccessor World { get; }

    public override Widget Build(
        BuildContext context
    )
    {
        var theme = Theme.Of(context);
        var itemStack = Slot.Itemstack;
        if (itemStack == null)
        {
            return new Text("ItemStack null");
        }

        var collectible = itemStack.Collectible;
        var children = new List<Widget>();

        var name = collectible.GetHeldItemName(itemStack);
        children.Add(
            new VtmlText(
                name,
                new TextStyle
                {
                    FontSize = 16,
                    Color = theme.ColorScheme.Primary,
                    Weight = FontWeight.Bold,
                    SoftWrap = true
                }
            )
        );

        var sb = new StringBuilder();
        collectible.GetHeldItemInfo(
            Slot,
            sb,
            World,
            true
        );
        var info = sb.ToString().Trim();

        if (!string.IsNullOrEmpty(info))
        {
            const int size = 96;
            children.Add(
                new Padding(
                    EdgeInsets.Only(top: 6),
                    new Row(
                        crossAxisAlignment: CrossAxisAlignment.Start,
                        mainAxisSize: MainAxisSize.Min,
                        spacing: 8,
                        children: new List<Widget>
                        {
                            new ItemStackDisplay(
                                itemStack,
                                size,
                                size,
                                size * 2
                            ),
                            new Expanded(
                                new VtmlText(
                                    info,
                                    new TextStyle { FontSize = 14, SoftWrap = true }
                                )
                            )
                        }
                    )
                )
            );
        }

        return new ConstrainedBox(
            new LayoutConstraints(
                0,
                MaxWidth
            ),
            new Padding(
                EdgeInsets.All(8),
                new Column(
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    mainAxisSize: MainAxisSize.Min,
                    children: children
                )
            )
        );
    }
}
