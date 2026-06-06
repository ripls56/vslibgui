using Gui.Core.Basic;
using Gui.Core.Framework;
using Gui.Widgets.Framework;
using Vintagestory.API.Common;

namespace Gui.Widgets.Inventory;

/// <summary>
///     A leaf widget that displays an <see cref="ItemStack" /> icon. The icon is
///     rendered via <see cref="ItemStackRenderer" /> (offscreen GL → cached SKBitmap).
///     <para>
///         Use this widget directly when you just need to display an item icon. For a
///         full interactive inventory slot (background, hover, click, stack size), use
///         <see cref="FlatItemSlot" /> or <see cref="NineSliceItemSlot" />.
///     </para>
/// </summary>
public class ItemStackDisplay : RenderObjectWidget
{
    public ItemStackDisplay(
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

    public override void UpdateRenderObject(
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
