# Dialogs

The Library ships specialized dialog base classes on top of `GuiBase`.

## `GuiDialogBlockEntityBase`

Abstract `GuiBase` subclass for dialogs bound to a specific block entity (chests, furnaces, custom container blocks, etc.). It handles:

### Minimal subclass

```csharp
public class MyChestGui : GuiDialogBlockEntityBase
{
    public MyChestGui(BlockPos pos, InventoryBase inv, ICoreClientAPI capi)
        : base(pos, inv, capi) { }

    protected override Widget Build() =>
        new InventoryRoot(capi, () => TryClose());

    protected override SoundAttributes? OpenSound =>
        new SoundAttributes { location = new AssetLocation("sounds/block/chestopen") };
}
```

### Floaty positioning

Override `Anchor` and return a cached instance (do NOT allocate per call).

```csharp
protected override WorldAnchor? Anchor =>
    field ??= new WorldAnchor
    {
        WorldPos = pos.ToVec3d().Add(0.5, 0.75, 0.5),
        Align = 0.75
    };
```

The dialog is anchored above the block whenever `immersiveMouseMode` is on. When off, standard `GuiBase` positioning applies.

## `WorldAnchor`

Pure helper — projects a world-space `Vec3d` to a window-space top-left position via the perspective matrices. Used by `GuiDialogBlockEntityBase`. Takes primitives (matrices, frame size, GUI scale) rather than `ICoreClientAPI`, so it is unit-testable without a game runtime.

`TryProject` returns `false` when the target is behind the camera; callers should keep the previous window position in that case.
