using System;
using System.Collections.Generic;
using System.Text;
using Gui.Core.Layout;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Overlay;
using Gui.Widgets.Painting;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace Gui.Widgets.Inventory;

/// <summary>
///     Displays item information inside a tooltip bubble, replicating the
///     vanilla Vintage Story item tooltip layout: item name in gold, an
///     item icon, and the full description produced by
///     <see cref="CollectibleObject.GetHeldItemInfo" />.
///     Designed to be passed as the <c>content</c> parameter of
///     <see cref="Tooltip" />.
/// </summary>
public class ItemTooltipContent : StatefulWidget
{
    /// <summary>Maximum tooltip width in pixels.</summary>
    public const float MaxWidth = 350f;

    /// <summary>
    ///     Creates a new <see cref="ItemTooltipContent" />.
    /// </summary>
    public ItemTooltipContent
    (
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

    public override State CreateState() => new ItemTooltipContentState();
}

internal class ItemTooltipContentState : State<ItemTooltipContent>
{
    private AnimationController _controller = null!;
    private CurvedAnimation _curved = null!;

    public override void InitState()
    {
        base.InitState();
        _controller = new AnimationController
        (
            TimeSpan.FromMilliseconds(400),
            Element.Owner!.GetTickerProvider()
        );
        _curved = new CurvedAnimation(_controller, Curves.EaseOut);
        _controller.Forward();
    }

    public override void Dispose()
    {
        _controller.Dispose();
        base.Dispose();
    }

    public override Widget Build
    (
        BuildContext context
    )
    {
        var theme = Theme.Of(context);
        var itemStack = Widget.Slot.Itemstack;
        if (itemStack == null)
        {
            return new Text("ItemStack null");
        }

        var collectible = itemStack.Collectible;
        var children = new List<Widget>();

        var name = collectible.GetHeldItemName(itemStack);
        children.Add
        (
            new VtmlText
            (
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
        collectible.GetHeldItemInfo
        (
            Widget.Slot,
            sb,
            Widget.World,
            ClientSettings.ExtendedDebugInfo
        );
        var info = sb.ToString().Trim();

        if (!string.IsNullOrEmpty(info))
        {
            const int size = 96;
            children.Add
            (
                new Padding
                (
                    EdgeInsets.Only(top: 6),
                    new Row
                    (
                        crossAxisAlignment: CrossAxisAlignment.Start,
                        mainAxisSize: MainAxisSize.Min,
                        spacing: 8,
                        children: new List<Widget>
                        {
                            new AnimatedBuilder
                            (
                                _controller,
                                _ => new Opacity
                                (
                                    (float)_curved.Value,
                                    new ItemStackDisplay
                                    (
                                        itemStack,
                                        size,
                                        size,
                                        size * 2,
                                        showTooltip: false
                                    )
                                )
                            ),
                            new Expanded
                            (
                                new VtmlText
                                (
                                    info,
                                    new TextStyle
                                    {
                                        FontSize = 14, SoftWrap = true
                                    }
                                )
                            )
                        }
                    )
                )
            );
        }

        return new ConstrainedBox
        (
            new LayoutConstraints
            (
                0,
                ItemTooltipContent.MaxWidth
            ),
            new Padding
            (
                EdgeInsets.All(8),
                new Column
                (
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    mainAxisSize: MainAxisSize.Min,
                    children: children
                )
            )
        );
    }
}
