using Gui.Rendering;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;
using SkiaSharp;
using Vintagestory.API.Common;

namespace Gui.Widgets.Inventory;

/// <summary>
///     An inventory slot widget with a nine-slice bitmap background.
///     Composes <see cref="ItemSlotGestureLayer" />, a <see cref="NineSliceBox" />, and
///     <see cref="ItemSlotOverlay" />.
/// </summary>
public class NineSliceItemSlot : StatelessWidget
{
    /// <summary>Creates a nine-slice inventory slot.</summary>
    public NineSliceItemSlot(
        SKBitmap texture,
        EdgeInsets slice,
        float size = 64f,
        ItemSlot? slot = null,
        SlotController? controller = null,
        float scale = 1f,
        Vector4? hoverColor = null,
        EdgeInsets? padding = null,
        Framework.Key? key = null
    ) : base(key)
    {
        Texture = texture;
        Slice = slice;
        Size = size;
        Slot = slot;
        Controller = controller;
        Scale = scale;
        HoverColor = hoverColor;
        Padding = padding;
    }

    /// <summary>Background bitmap used for nine-slice rendering.</summary>
    public SKBitmap Texture { get; }

    /// <summary>Insets defining the fixed corners of the nine-slice.</summary>
    public EdgeInsets Slice { get; }

    /// <summary>Side length of the slot in pixels.</summary>
    public float Size { get; }

    /// <summary>The inventory slot to display. May be null.</summary>
    public ItemSlot? Slot { get; }

    /// <summary>Handles click, wheel, and drag interactions. May be null.</summary>
    public SlotController? Controller { get; }

    /// <summary>Texture scale multiplier applied to nine-slice rendering.</summary>
    public float Scale { get; }

    /// <summary>Overrides the theme hover overlay color when set.</summary>
    public Vector4? HoverColor { get; }

    /// <summary>Overrides the theme item padding when set.</summary>
    public EdgeInsets? Padding { get; }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        return new ItemSlotGestureLayer(
            slot: Slot,
            controller: Controller,
            childBuilder: () => new NineSliceBox(
                Texture,
                Slice,
                width: Size,
                height: Size,
                scale: Scale,
                child: new ItemSlotOverlay(Slot, Size, HoverColor, Padding)
                {
                    EnableSpotlight = false
                }
            )
        );
    }
}
