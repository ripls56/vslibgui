# Rendering

## Pipeline Overview

```
[PreSkiaPipeline — once per frame]
    └── SkiaRenderer.Begin()               // acquire Skia GL surface

[GuiBase.OnRenderGUI — per open window]
    └── RenderObject.AdvanceFrame()
    └── _tickerScheduler.Update(elapsed)
    └── new/reset PaintingContext(canvas)
    └── BuildOwner.BuildDirtyElements()    // rebuild dirty widget subtrees (parents first)
    └── rootRo.Layout(constraints)        // if NeedsLayout or ChildNeedsLayout
    └── canvas.Translate(WindowPos)        // position window on screen
    └── canvas.ClipRect(windowBounds)
    └── rootRo.Paint(context)             // recursive tree walk
        ├── PaintInternal()               // draw background, border, text, image
        └── children (translated by X, Y)
            └── child.Paint(context)
    └── DebugPainter overlay              // if debug mode enabled

[PostSkiaPipeline — once per frame]
    └── SkiaRenderer.End()                // flush to OpenGL framebuffer
```

Each `GuiBase` window runs this pipeline independently. The canvas translate/clip ensures
widgets paint in window-local coordinates while rendering at the correct screen position.

## SkiaRenderer

`SkiaRenderer` bridges Skia (SkiaSharp) and OpenGL. It is created by `GuiModSystem` and
shared across all open GUI screens.

`Begin(width, height, scale)`:
- Acquires the GL framebuffer as an `SKSurface`
- Scales the canvas by `uiScale` so all coordinates are in UI points, not pixels
- Returns an `SKCanvas` ready for drawing

`End()`:
- Flushes the Skia command buffer to OpenGL
- Restores OpenGL state that Skia modifies (viewport, depth mask, blend, scissor, textures)

You never call `SkiaRenderer` directly — `GuiBase.OnRenderGUI` handles it.

## PaintingContext

`PaintingContext` wraps an `SKCanvas` and provides a stable drawing API to all `RenderObject`
subclasses. It maintains a **canvas stack** so that `RepaintBoundary` can push a recording
canvas during cache capture without affecting the active draw target.

### DrawBox

Draws a filled rounded rectangle with an optional border:

```csharp
// Called by RenderBox.PaintInternal internally.
// Direct use is rare — prefer Container/BoxStyle.
context.DrawBox(
    pos: Vector2.Zero,
    size: Size,
    color: new Vector4(0.1f, 0.1f, 0.1f, 0.9f),
    radii: new Vector4(8),   // X=TR, Y=BR, Z=TL, W=BL
    border: 2f,
    borderColor: new Vector4(1, 1, 1, 0.4f)
);
```

### DrawImage

Draws a bitmap using pre-computed `srcRect` and `dstRect` (computed by `BoxFit` logic),
clipped to a rounded rectangle:

```csharp
context.DrawImage(bitmap, pos, size, srcRect, dstRect, radii, border, borderColor);
```

### DrawText

Draws text with automatic Unicode font fallback and optional glow/outline passes:

```csharp
context.DrawText(
    text: "Hello 世界！",             // mixed-script supported
    pos: new Vector2(0, fontSize),   // Y is baseline
    fontSize: 14f,
    color: Vector4.One,
    fontFamily: "sans-serif",
    weight: FontWeight.Normal,
    boldness: 0.1f,
    outlineWidth: 0.05f,
    outlineColor: new Vector4(0, 0, 0, 1),
    glowWidth: 0.3f,
    glowColor: new Vector4(0.5f, 0.8f, 1, 0.5f)
);
```

Drawing order: glow (blur filter) → outline (stroke) → fill.

Internally the string is split into font runs by `FontRunSplitter` (primary typeface first,
system fallback for uncovered codepoints), shaped per-run by `TextShaper` via HarfBuzz, and
drawn as one `SKTextBlob` per run. Both `MeasureText` and `BreakIntoLines` use the same
shaped widths so wrapping is accurate for CJK and mixed-script text.

Font aliases `"sans-serif"` and `"serif"` resolve to `GuiStyle.StandardFontName` and
`GuiStyle.DecorativeFontName` respectively. Both are registered at startup by `GuiModSystem`.

### Box Shadows

`RenderBox` supports CSS-like box shadows via the `Shadows` property (set from
`BoxStyle.BoxShadows`). The paint order in `PaintInternal` is:

```
1. Outer shadows  (behind the box)
2. Fill + border  (the box itself)
3. Inner shadows  (inside the box, above fill, below children)
```

**Outer shadows** use `SKMaskFilter.CreateBlur` on a rounded rect expanded by `SpreadRadius`
and offset by `Offset`. A `ClipRoundRect(Difference)` clips out the box interior so the
shadow never bleeds through transparent fills.

**Inner shadows** use a `SaveLayer` compositing technique:
1. Clip to the box boundary (respecting corner radii)
2. `SaveLayer` with an `SKImageFilter` blur
3. Flood the layer with the shadow color
4. Punch a transparent hole with `DstOut` blend (contracted by spread, shifted by offset)
5. `Restore` — the blur filter applies to the composited result

Blur filters (`SKMaskFilter` for outer, `SKImageFilter` for inner) are cached in
`PaintingContext` by rounded sigma value, avoiding per-frame filter recreation.

`RenderRepaintBoundary` inflates its `SKPictureRecorder` bounds by the maximum outer shadow
extent in the subtree so shadows are not clipped by the recording rect.

### SaveLayer / Restore

Used by `Opacity` to create an isolated compositing layer:

```csharp
context.SaveLayer(bounds: null, paint: alphaPaint);
// draw children at full opacity
context.Restore();   // composite with alpha
```

## RenderObject Paint Pass

`RenderObject.Paint(context)` is the recursive paint entry point:

```
1. Mark WasPaintedThisFrame (frame-ID comparison, no reset needed)
2. Fire OnAnyPaint if dirty (performance metrics)
3. Save canvas + apply ClipBehavior if != None
4. Call PaintInternal(context)     ← subclass draws its visual content here
5. Clear NeedsPaint
6. foreach child:
       canvas.Save()
       canvas.Translate(child.X, child.Y)   ← position child in local space
       child.Paint(context)
       canvas.Restore()
7. Clear ChildNeedsPaint
8. Restore canvas (from clip)
```

The canvas translation model means every `RenderObject` draws at local origin `(0, 0)`. The
parent is responsible for positioning via `Translate`.

## RepaintBoundary

`RepaintBoundary` is the primary performance tool for large, mostly-static subtrees. It caches
its entire subtree as an `SKPicture` object.

```csharp
// Wrap stable subtrees adjacent to animated content:
new RepaintBoundary(
    new Sidebar(...)   // static — cache hits every animation frame
)

// DO NOT wrap the animated widget itself — cache is invalidated every frame:
// new RepaintBoundary(new AnimatedBuilder(...))  ← wrong, pure overhead
```

### How the cache works

On every `Paint` call, `RenderRepaintBoundary` decides between:

**Cache hit** — `NeedsPaint == false && ChildNeedsPaint == false && _cache != null`:
```
canvas.DrawPicture(_cache)   // replay recorded commands, no widget rebuild needed
```

**Cache miss** — subtree is dirty or cache is empty:
```
SKPictureRecorder recorder
context.PushCanvas(recorder.BeginRecording(...))
// paint self and all children into recording canvas
context.PopCanvas()
_cache = recorder.EndRecording()
canvas.DrawPicture(_cache)
```

`OnMarkNeedsPaint()` is overridden to immediately dispose `_cache` (clearing it), so the next
frame will re-record. The cache is never stale.

### When to use RepaintBoundary

- **Sidebar / static panels** that don't change but share a frame with animated content
- **Item grids / lists** that are stable while animation plays elsewhere

Wrap the **static neighbors** of animated content, not the animated widget itself.

### When NOT to use RepaintBoundary

- On **animated subtrees** — an animated child calls `MarkNeedsPaint()` every frame, which
  walks up past the boundary setting `ChildNeedsPaint` and clearing `_cache`. The result is a
  cache miss every frame: record overhead with zero hits.
- On **frequently-changing** subtrees for the same reason
- On **tiny widgets** (the `SKPicture` overhead outweighs the savings)

## Custom RenderObject

Implementing a custom `RenderObject` lets you control layout and painting directly.

```csharp
// 1. Define the widget:
class CircleWidget : RenderObjectWidget
{
    public Vector4 Color { get; }
    public float Radius { get; }

    public CircleWidget(Vector4 color, float radius)
    {
        Color = color;
        Radius = radius;
    }

    public override Element CreateElement() => new RenderObjectElement(this);

    public override RenderObject CreateRenderObject() =>
        new _RenderCircle { Color = Color, Radius = Radius };

    public override void UpdateRenderObject(RenderObject ro)
    {
        var circle = (_RenderCircle)ro;
        circle.Color = Color;
        circle.Radius = Radius;
    }
}

// 2. Implement the RenderObject:
class _RenderCircle : RenderBox
{
    private Vector4 _color;
    public Vector4 Color
    {
        get => _color;
        set => SetProperty(ref _color, value, repaint: true);
    }

    private float _radius;
    public float Radius
    {
        get => _radius;
        set => SetProperty(ref _radius, value, relayout: true);
    }

    protected override void PerformLayout()
    {
        float diameter = _radius * 2;
        Size = new Vector2(diameter, diameter);
    }

    protected override void PaintInternal(PaintingContext context)
    {
        var paint = context.SharedPaint;
        paint.Color = _color.ToSkColor();
        paint.Style = SKPaintStyle.Fill;
        context.Canvas.DrawCircle(_radius, _radius, _radius, paint);
    }
}
```

**Important rules for `PerformLayout`:**
- Set `Size` to the desired render object size
- Pass appropriate constraints to each child and call `child.Layout(constraints)`
- Set `child.X` and `child.Y` to position each child

**Important rules for `PaintInternal`:**
- Draw at local origin `(0, 0)` — the canvas is already translated
- Do **not** translate the canvas yourself for children (the base `Paint` does this)
- Use `SetProperty(ref _field, value, repaint: true/false, relayout: true/false)` in setters — never call `MarkNeeds*` directly
- Use `context.SharedPaint` — never allocate `new SKPaint` inside `PaintInternal`

## Performance Tips

**Use `RenderRepaintBoundary`** for stable subtrees adjacent to animated content.

**Avoid per-frame allocations in `PaintInternal`.** The `SKPaint` in a custom renderer should
be reused. `PaintingContext` uses a shared `_sharedPaint` instance internally for this reason.

**Keep `Build()` pure.** Do not perform layout calculations in `Build()`. `Build()` may run
multiple times per frame during cascaded rebuilds.

**Font caching.** `TextLayoutHelper` caches `SKFont` instances keyed by family + size (rounded
to 0.5pt) + weight. Animations that smoothly change font size pay a small cost for cache misses
near .5pt boundaries but avoid object allocation on every frame.

**`WasPaintedThisFrame`** uses a frame-ID counter rather than a boolean flag, eliminating the
need for a full tree walk at the end of each frame to reset it.

## ItemStack render cache

`RenderItemStack` (widget: `ItemStackDisplay`) paints an item icon, but item icons are 3D
renders — too expensive to redraw every frame. `ItemStackRenderer` renders each stack once into
an offscreen GL framebuffer, wraps the result as an `SKImage`, and caches it. The render runs in
`ProcessQueue` before the Skia frame begins, while GL state is clean.

### Visual bucketing

Some attributes change almost every frame but only affect appearance in coarse steps — a hot
ingot's glow as it cools, food slowly perishing. Keying the cache on the raw attribute would miss
on every frame, re-rendering constantly and briefly showing a stale fallback (flicker).

Instead the cache key combines the collectible, render size, and a composite **visual bucket** that
*quantizes* such attributes. Two stacks in the same bucket share one render; crossing a bucket
boundary produces a fresh, visually-distinct image. Temperature and transition (spoilage/curing)
progress are built-in. Register your own for any other continuous visual attribute:

```csharp
ItemStackRenderer.RegisterVisualBucket("charge", (world, stack) =>
    (int)(stack.Attributes.GetFloat("charge") / 10f)); // one bucket per 10% charge
```

Registering excludes the raw attribute from exact cache identity (via `IgnoreAttribute`), so only
the quantized bucket distinguishes renders. Tune granularity with `TemperatureBuckets`,
`GlowStartTemperature`, `GlowFullTemperature`, and `TransitionBuckets`.

### Pending fallback

While the exact visual state of an item is still rendering, the slot falls back to the most recent
cached image for the same collectible (`GetCachedAny`, filtered by class/id/render size), so it never
blanks out and never shows a different item. With temperature bucketing and the glow-refresh pass
(below) keeping the current bucket rendered and MRU, that fallback is the adjacent/last-shown bucket —
a tiny, imperceptible step.

The slot must **not** hold an `SKImage` across frames. Cache images wrap a GL texture whose lifetime
the cache owns: on eviction it disposes the image and `GL.DeleteTexture`s the id, which a later render
recycles. A retained reference would then sample a dead/recycled texture — a use-after-free that flashes
the wrong item or hard-crashes the GL context. `GetCachedAny` returns a live entry drawn the same frame,
so there is nothing to dangle.

### Keeping glowing slots current

GUI paints are dirty-only, so a slot that isn't interacted with stops repainting. A hot item left
cooling in an un-hovered slot would otherwise freeze on its last drawn frame, never request the
intermediate temperature buckets, and then jump when you finally hover it. To prevent this,
`ItemStackRenderer` tracks slots whose item is hot enough to glow and, on a coarse interval, repaints
them whenever cooling carries the item into a new temperature bucket (`TrackGlowingIfHot` /
`RefreshGlowingSlots`). The glow steps down on its own; a re-render happens only at an actual bucket
crossing, and detached or cooled slots are pruned automatically.

## Frametime profiling

The GUI ships a per-window profiler for finding why a `GuiBase` screen costs a lot
of frame time, without external tools. It surfaces two signals:

- **Re-record frequency** — how often the window re-records its `SKPicture`. The
  picture is cached and only re-recorded when something marks the tree dirty
  (`NeedsPaint`/`ChildNeedsPaint`). A window re-recording every frame (`60/60`) is
  the usual cause of a steadily high `ms` reading when vanilla is fine.
- **Dirty initiators** — which widgets called `MarkNeedsPaint`/`MarkNeedsLayout`,
  ranked by how many frames they initiated dirtying. The top entry on a `60/60`
  window is almost always the culprit (an animation or `ValueNotifier` ticking
  every frame).

It also prints the coarse Begin/Build/Layout/Paint/End stage breakdown already
collected by `PerformanceMetrics`, so you can tell whether the cost is in paint at
all before drilling into widgets.

### Usage

- `/ui prof on` — enable collection and draw the overlay panel.
- `/ui prof off` — disable both.
- `/ui prof dump` — print a per-window report (stage ms, re-record frequency, top
  dirty initiators with widget paths) to chat. Copyable from a server.
- `/ui prof reset` — clear collected data.

Collection is gated by `GuiProfiler.Enabled` and has no cost when off. The profiler
measures CPU-side cost only; GPU rasterisation (`DrawPicture`) is not attributed to
individual nodes.

Note: `reset` clears the collected profiler data (re-record window and dirty initiators),
but the stage timings shown in `dump` come from `PerformanceMetrics`, which maintains a
rolling/live measurement and is not cleared by `reset`. Measurement attribution has a
boundary: only dirtying initiated during a window's build/layout/paint pass is credited
to that pass; dirtying from outside the frame loop (e.g., animation ticks or event handler
`SetState` calls before the frame runs) may not be attributed to any window, so initiator
counts can under-report for animation-heavy HUDs.
