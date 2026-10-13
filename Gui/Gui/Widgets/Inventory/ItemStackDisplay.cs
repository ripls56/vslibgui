using System;
using Gui.Core.Basic;
using Gui.Core.Framework;
using Gui.Widgets.Framework;
using Gui.Widgets.Overlay;
using Vintagestory.API.Common;

namespace Gui.Widgets.Inventory;

/// <summary>
///     A widget that displays an <see cref="ItemStack" /> icon. The icon is
///     rendered via <see cref="Gui.Rendering.ItemStackRenderer" /> (offscreen GL → cached SKBitmap).
///     <para>
///         By default hovering the icon shows the vanilla-style item tooltip
///         (<see cref="ItemTooltipContent" />). Pass <c>showTooltip: false</c> when the icon
///         already lives inside something that owns a tooltip (e.g. a full inventory slot).
///     </para>
///     <para>
///         Use this widget directly when you just need to display an item icon. For a
///         full interactive inventory slot (background, hover, click, stack size), use
///         <see cref="FlatItemSlot" /> or <see cref="NineSliceItemSlot" />.
///     </para>
/// </summary>
public class ItemStackDisplay : StatelessWidget
{
    public ItemStackDisplay
    (
        ItemStack? itemStack = null,
        float? width = null,
        float? height = null,
        int renderSize = 48,
        bool showTooltip = true,
        Framework.Key? key = null
    ) : base(key)
    {
        ItemStack = itemStack;
        Width = width;
        Height = height;
        RenderSize = renderSize;
        ShowTooltip = showTooltip;
    }

    /// <summary>
    ///     Binary-compatibility overload matching the pre-tooltip signature
    ///     (<c>key</c> as the fifth positional argument). Tooltips are enabled.
    ///     <paramref name="key" /> has no default so this overload never competes with the
    ///     primary constructor during overload resolution.
    /// </summary>
    [Obsolete("Use the constructor with the showTooltip parameter.")]
    public ItemStackDisplay
    (
        ItemStack? itemStack,
        float? width,
        float? height,
        int renderSize,
        Framework.Key? key
    ) : this
    (
        itemStack,
        width,
        height,
        renderSize,
        true,
        key
    )
    {
    }

    public ItemStack? ItemStack { get; }
    public float? Width { get; }
    public float? Height { get; }
    public int RenderSize { get; }

    /// <summary>When true (default), hovering the icon shows the item tooltip.</summary>
    public bool ShowTooltip { get; }

    /// <inheritdoc />
    public override Widget Build
    (
        BuildContext context
    )
    {
        Widget icon = new RawItemStackDisplay
        (
            ItemStack,
            Width,
            Height,
            RenderSize
        );

        var world = GuiModSystem.Instance?.Capi?.World;
        if (!ShowTooltip || ItemStack?.Collectible == null || world == null)
        {
            return icon;
        }

        return new Tooltip
        (
            icon,
            new ItemTooltipContent
            (
                new DummySlot(ItemStack),
                world
            ),
            TimeSpan.FromMilliseconds(350),
            useGlobalOverlay: true
        );
    }
}

/// <summary>
///     The leaf render-object widget behind <see cref="ItemStackDisplay" />: paints the
///     item icon and nothing else. Use <see cref="ItemStackDisplay" /> unless you
///     explicitly need a widget with no tooltip wrapper at all.
/// </summary>
public class RawItemStackDisplay : RenderObjectWidget
{
    public RawItemStackDisplay
    (
        ItemStack? itemStack = null,
        float? width = null,
        float? height = null,
        int renderSize = 48,
        Framework.Key? key = null
    ) : base(key)
    {
        ItemStack = itemStack;
        Width = width;
        Height = height;
        RenderSize = renderSize;
    }

    public ItemStack? ItemStack { get; }
    public float? Width { get; }
    public float? Height { get; }
    public int RenderSize { get; }

    public override RenderObject CreateRenderObject() => new RenderItemStack();

    public override void UpdateRenderObject
    (
        RenderObject renderObject
    )
    {
        var ro = (RenderItemStack)renderObject;
        ro.ItemStack = ItemStack;
        ro.Width = Width;
        ro.Height = Height;
        ro.RenderSize = RenderSize;
    }
}
