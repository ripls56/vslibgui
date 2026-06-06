using System;
using System.Collections.Generic;
using Gui.Core.Painting;
using Gui.Rendering;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Core.Framework;

public abstract class ParentData
{
}

public enum FlexFit
{
    /// <summary>Force the child to fill the allocated flex space (like Expanded).</summary>
    Tight,

    /// <summary>Allow the child to be smaller than the allocated flex space.</summary>
    Loose
}

public class FlexParentData : ParentData
{
    public int Flex { get; set; }
    public FlexFit Fit { get; set; } = FlexFit.Tight;
}

public class StackParentData : ParentData
{
    public float? Top { get; set; }
    public float? Left { get; set; }
    public float? Right { get; set; }
    public float? Bottom { get; set; }
    public float? Width { get; set; }
    public float? Height { get; set; }

    public bool IsPositioned => Top.HasValue || Left.HasValue || Right.HasValue ||
                                Bottom.HasValue || Width.HasValue ||
                                Height.HasValue;
}

/// <summary>
///     Controls how a render object clips its content to its bounds.
/// </summary>
public enum ClipBehavior
{
    /// <summary>No clipping. Content may paint outside the render object's bounds.</summary>
    None,

    /// <summary>
    ///     Clips content to bounds using an axis-aligned rectangle. Fast — no anti-aliasing
    ///     on the clip edge. Use when pixel-perfect clipping is acceptable.
    /// </summary>
    HardEdge,

    /// <summary>
    ///     Clips content to bounds with anti-aliased edges. Slightly more expensive than
    ///     <see cref="HardEdge" /> but produces smoother results at rounded corners or
    ///     diagonal clip shapes.
    /// </summary>
    AntiAlias
}

/// <summary>
///     The base class of the render tree. A <c>RenderObject</c> owns layout (size + position),
///     paint (Skia draw calls), and dirty-flag propagation. It does <b>not</b> hold widget
///     configuration or element state — those live in the widget and element trees.
///     <para>
///         Subclasses must implement <see cref="PerformLayout" /> to compute their own
///         <see cref="Size" /> and position children, and may override <see cref="PaintInternal" />
///         to issue Skia draw calls via <see cref="Rendering.PaintingContext" />.
///     </para>
///     <para>
///         Property setters that affect layout must call <see cref="MarkNeedsLayout" />.
///         Property setters that only affect appearance must call <see cref="MarkNeedsPaint" />.
///     </para>
/// </summary>
public abstract class RenderObject : IDisposable
{
    private readonly List<RenderObject> _children = [];
    private Vector4? _debugColor;

    private int _lastPaintedFrameId = -1;

    /// <summary>
    ///     Debug repaint tracking state. Uses frame IDs for unambiguous hit/miss detection.
    ///     Read by <see cref="Gui.Debugging.DebugPainter" /> during the debug overlay pass.
    ///     Explicitly initialized so that frame-ID sentinels (-1) are set rather than zero-initialized.
    /// </summary>
    internal RepaintRecord RepaintRecord = new();

    public RenderObject? Parent { get; private set; }
    public IReadOnlyList<RenderObject> Children => _children;

    /// <summary>The constraints passed by the parent in the last <see cref="Layout" /> call.</summary>
    public LayoutConstraints Constraints { get; private set; }

    /// <summary>
    ///     The size computed by the last <see cref="PerformLayout" /> call, clamped to
    ///     <see cref="Constraints" />. Set this inside <c>PerformLayout</c>.
    /// </summary>
    public Vector2 Size { get; set; }

    /// <summary>
    ///     Controls whether this render object clips its content to its bounds during
    ///     paint. The clip is applied AFTER <see cref="PaintInternal" /> in subclasses
    ///     that override <see cref="Paint" /> to draw pre-clip artwork (e.g. outer
    ///     box shadows in <see cref="RenderBox" />); otherwise it wraps the entire
    ///     paint pass. Changing this value triggers a repaint.
    /// </summary>
    public virtual ClipBehavior ClipBehavior
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    } = ClipBehavior.None;

    public float X
    {
        get;
        // X/Y are always set by the parent during PerformLayout.
        // We only need to mark the parent for repaint (not re-layout) since
        // the position change itself is already part of the current layout pass.
        set
        {
            if (Math.Abs(field - value) > 0.01)
            {
                field = value;
                Parent?.MarkNeedsPaint();
            }
        }
    }

    public float Y
    {
        get;
        set
        {
            if (Math.Abs(field - value) > 0.01)
            {
                field = value;
                Parent?.MarkNeedsPaint();
            }
        }
    }

    public ParentData? ParentData { get; set; }

    public bool NeedsLayout { get; internal set; } = true;
    public bool NeedsPaint { get; internal set; } = true;
    public bool ChildNeedsLayout { get; internal set; } = true;
    public bool ChildNeedsPaint { get; internal set; } = true;

    /// <summary>
    ///     Monotonically incrementing frame counter. Incremented by GuiBase at the start of each frame.
    /// </summary>
    public static int CurrentFrameId { get; private set; }

    /// <summary>
    ///     True if this RenderObject was painted during the current frame.
    ///     Implemented with frame IDs to avoid a full tree walk for reset each frame.
    ///     Use <see cref="MarkPainted" /> to record a paint event.
    /// </summary>
    public bool WasPaintedThisFrame => _lastPaintedFrameId == CurrentFrameId;

    public Vector4 DebugColor => _debugColor ??= GenerateDebugColor();

    public static Action<RenderObject>? OnAnyPaint { get; set; }

    /// <summary>
    ///     When set, called during layout whenever a constraint violation is detected.
    ///     The arguments are the offending <see cref="RenderObject" /> and a human-readable message.
    ///     Set this in <c>GuiBase</c> when layout debugging is enabled; clear it otherwise.
    /// </summary>
    public static Action<RenderObject, string>? OnLayoutViolation { get; set; }

    /// <summary>
    ///     True when the most recent layout pass detected a violation (invalid constraints,
    ///     size exceeding constraints, or overflow). Cleared at the start of each layout pass.
    ///     Used by <c>DebugPainter</c> to draw a visual overlay.
    /// </summary>
    public bool HasLayoutViolation { get; private set; }

    public bool IsRepaintBoundary { get; protected set; } = false;


    /// <summary>
    ///     Screen-space offset of the window that owns this render tree.
    ///     Set by <see cref="GuiBase" /> on the root each frame.
    ///     Use <see cref="GetScreenOffset" /> to retrieve it from any node.
    /// </summary>
    internal Vector2 ScreenOffset { get; set; }

    public virtual bool IsHitTestTarget => false;

    public virtual void Dispose()
    {
        // DO NOT dispose children recursively. 
        // We might be just replacing a wrapper RO, and children might be reused.
        // We rely on GC for truly orphaned ROs.
        Parent?.RemoveChild(this);
        _children.Clear();
    }

    public static void AdvanceFrame() => CurrentFrameId++;

    internal void MarkPainted() => _lastPaintedFrameId = CurrentFrameId;

    private Vector4 GenerateDebugColor()
    {
        var rnd = new Random(GetHashCode());
        var r = (float)rnd.NextDouble();
        var g = (float)rnd.NextDouble();
        var b = (float)rnd.NextDouble();

        var max = Math.Max(
            r,
            Math.Max(
                g,
                b
            )
        );
        if (max < 0.7f && max > 0.001f)
        {
            var scale = 0.7f / max;
            r = Math.Min(
                1.0f,
                r * scale
            );
            g = Math.Min(
                1.0f,
                g * scale
            );
            b = Math.Min(
                1.0f,
                b * scale
            );
        }
        else if (max <= 0.001f)
        {
            r = 0.7f;
            g = 0.7f;
            b = 0.7f;
        }

        return new Vector4(
            r,
            g,
            b,
            1.0f
        );
    }

    public virtual void AddChild(
        RenderObject child
    )
    {
        if (child.Parent == this)
        {
            return;
        }

        child.Parent?.RemoveChild(child);
        child.Parent = this;
        _children.Add(child);
        MarkNeedsLayout();
    }

    public virtual void RemoveChild(
        RenderObject child
    )
    {
        if (!_children.Remove(child))
        {
            return;
        }

        child.Parent = null;
        MarkNeedsLayout();
    }

    /// <summary>
    ///     Reorders existing children to match <paramref name="newOrder" />.
    ///     Every element in <paramref name="newOrder" /> must already be a child.
    /// </summary>
    internal void ReorderChildren(
        List<RenderObject> newOrder
    )
    {
        if (newOrder.Count != _children.Count)
        {
            return;
        }

        var same = true;
        for (var i = 0; i < _children.Count; i++)
        {
            if (!ReferenceEquals(
                    _children[i],
                    newOrder[i]
                ))
            {
                same = false;
                break;
            }
        }

        if (same)
        {
            return;
        }

        _children.Clear();
        _children.AddRange(newOrder);
        MarkNeedsLayout();
    }

    /// <summary>
    ///     Marks this render object as needing relayout and propagates
    ///     <c>ChildNeedsLayout</c> up the ancestor chain so the frame loop knows
    ///     to re-run the layout pass. Also calls <see cref="MarkNeedsPaint" />.
    ///     Call this from any property setter that affects size or child positioning.
    /// </summary>
    public void MarkNeedsLayout()
    {
        if (NeedsLayout)
        {
            return;
        }

        NeedsLayout = true;
        MarkNeedsPaint();

        var curr = Parent;
        while (curr != null && !curr.ChildNeedsLayout)
        {
            curr.ChildNeedsLayout = true;
            curr.ChildNeedsPaint = true;
            curr = curr.Parent;
        }
    }

    /// <summary>
    ///     Marks this render object as needing repaint and propagates
    ///     <c>ChildNeedsPaint</c> up the ancestor chain. Call this from any property
    ///     setter that affects appearance but not size (e.g. color, border color).
    /// </summary>
    public void MarkNeedsPaint()
    {
        if (NeedsPaint)
        {
            return;
        }

        NeedsPaint = true;
        OnMarkNeedsPaint();

        var curr = Parent;
        while (curr != null && !curr.ChildNeedsPaint)
        {
            curr.ChildNeedsPaint = true;
            curr = curr.Parent;
        }
    }

    protected virtual void OnMarkNeedsPaint()
    {
    }

    /// <summary>
    ///     Sets a backing field if changed, marking the render object as
    ///     needing repaint and/or relayout. Returns true if the value
    ///     actually changed.
    /// </summary>
    protected bool SetProperty<T>(
        ref T field,
        T value,
        bool repaint = false,
        bool relayout = false
    )
    {
        if (EqualityComparer<T>.Default.Equals(
                field,
                value
            ))
        {
            return false;
        }

        field = value;
        if (relayout)
        {
            MarkNeedsLayout();
        }
        else if (repaint)
        {
            MarkNeedsPaint();
        }

        return true;
    }

    /// <summary>
    ///     Reports a layout violation from within a <see cref="PerformLayout" /> override.
    ///     Sets <see cref="HasLayoutViolation" /> and invokes <see cref="OnLayoutViolation" />
    ///     if a handler is registered (i.e. violation logging is enabled).
    /// </summary>
    protected void ReportLayoutViolation(
        string message
    )
    {
        HasLayoutViolation = true;
        OnLayoutViolation?.Invoke(
            this,
            message
        );
    }

    /// <summary>
    ///     Entry point for the layout pass. Skips relayout if constraints are unchanged and
    ///     no dirty flags are set. Otherwise stores <paramref name="constraints" />, calls
    ///     <see cref="PerformLayout" />, and clamps <see cref="Size" /> to the constraints.
    /// </summary>
    public virtual void Layout(
        LayoutConstraints constraints
    )
    {
        if (!NeedsLayout && !ChildNeedsLayout && Constraints.Equals(constraints))
        {
            return;
        }

        HasLayoutViolation = false;

        if (OnLayoutViolation != null)
        {
            ValidateIncomingConstraints(constraints);
        }

        Constraints = constraints;
        PerformLayout();

        if (OnLayoutViolation != null)
        {
            ValidateOutputSize();
        }

        Size = Constraints.Constrain(Size);

        NeedsLayout = false;
        ChildNeedsLayout = false;
    }

    private void ValidateIncomingConstraints(
        LayoutConstraints c
    )
    {
        string? error = null;
        if (float.IsNaN(c.MinWidth) || float.IsNaN(c.MaxWidth) ||
            float.IsNaN(c.MinHeight) || float.IsNaN(c.MaxHeight))
        {
            error = $"constraints contain NaN ({c})";
        }
        else if (c.MinWidth > c.MaxWidth + 0.001f)
        {
            error = $"MinWidth ({c.MinWidth:F1}) > MaxWidth ({c.MaxWidth:F1})";
        }
        else if (c.MinHeight > c.MaxHeight + 0.001f)
        {
            error = $"MinHeight ({c.MinHeight:F1}) > MaxHeight ({c.MaxHeight:F1})";
        }
        else if (c.MinWidth < -0.001f)
        {
            error = $"MinWidth ({c.MinWidth:F1}) is negative";
        }
        else if (c.MinHeight < -0.001f)
        {
            error = $"MinHeight ({c.MinHeight:F1}) is negative";
        }

        if (error != null)
        {
            HasLayoutViolation = true;
            OnLayoutViolation!.Invoke(
                this,
                $"{GetType().Name} received invalid constraints: {error}"
            );
        }
    }

    private void ValidateOutputSize()
    {
        const float epsilon = 0.001f;
        var widthViolation = Size.X > Constraints.MaxWidth + epsilon ||
                             Size.X < Constraints.MinWidth - epsilon;
        var heightViolation = Size.Y > Constraints.MaxHeight + epsilon ||
                              Size.Y < Constraints.MinHeight - epsilon;

        if (widthViolation || heightViolation)
        {
            HasLayoutViolation = true;
            var axis = widthViolation && heightViolation ? "both axes"
                : widthViolation ? "width" : "height";
            OnLayoutViolation!.Invoke(
                this,
                $"{GetType().Name} computed size ({Size.X:F1}, {Size.Y:F1}) violates " +
                $"constraints [{Constraints}] on {axis}. Size will be clamped."
            );
        }
    }

    /// <summary>
    ///     Compute and assign <see cref="Size" />. Must also call <c>child.Layout(childConstraints)</c>
    ///     on every child and set <c>child.X</c> / <c>child.Y</c> to position them.
    /// </summary>
    protected abstract void PerformLayout();

    /// <summary>Returns the minimum width this object needs to display without clipping, given a height.</summary>
    public virtual float GetMinIntrinsicWidth(
        float height
    ) =>
        0f;

    /// <summary>Returns the maximum useful width (single-line width for text), given a height.</summary>
    public virtual float GetMaxIntrinsicWidth(
        float height
    ) =>
        0f;

    /// <summary>Returns the minimum height this object needs to display without clipping, given a width.</summary>
    public virtual float GetMinIntrinsicHeight(
        float width
    ) =>
        0f;

    /// <summary>Returns the maximum useful height, given a width.</summary>
    public virtual float GetMaxIntrinsicHeight(
        float width
    ) =>
        0f;

    /// <summary>
    ///     Updates the rolling repaint frequency counter used for heat-map visualization.
    ///     Decays <see cref="RepaintRecord.HotFrameCount" /> by half for every elapsed 60-frame
    ///     window since the last update, then increments for the current repaint event.
    /// </summary>
    protected void UpdateHeatWindow()
    {
        const int windowSize = 60;
        var framesSinceLast = CurrentFrameId - RepaintRecord.HotWindowLastFrameId;
        var windowsElapsed = framesSinceLast / windowSize;
        if (windowsElapsed > 0)
        {
            // Right-shift by windowsElapsed to halve once per elapsed window.
            // Clamp to 31 shifts to avoid undefined bit-shift behavior on int.
            RepaintRecord.HotFrameCount = windowsElapsed >= 31
                ? 0
                : RepaintRecord.HotFrameCount >> windowsElapsed;
            RepaintRecord.HotWindowLastFrameId += windowsElapsed * windowSize;
        }

        RepaintRecord.HotFrameCount++;
    }

    /// <summary>
    ///     Public paint entry point invoked recursively by the framework. Manages the
    ///     per-frame paint scaffolding so subclasses only have to describe their visual
    ///     content in <see cref="PaintInternal" />:
    ///     <list type="number">
    ///         <item>marks this object as painted this frame (<see cref="MarkPainted" />);</item>
    ///         <item>
    ///             records repaint heat / fires <see cref="OnAnyPaint" /> via
    ///             <see cref="RecordPaintEvent" />;
    ///         </item>
    ///         <item>opens a canvas save scope and applies <see cref="ClipBehavior" /> if set;</item>
    ///         <item>calls <see cref="PaintInternal" /> for the subclass's own draw calls;</item>
    ///         <item>clears <see cref="NeedsPaint" /> (unless this is a repaint boundary);</item>
    ///         <item>
    ///             iterates children via <see cref="PaintChildren" />, translating into each
    ///             child's local origin;
    ///         </item>
    ///         <item>restores the canvas and clears <see cref="ChildNeedsPaint" />.</item>
    ///     </list>
    ///     <para>
    ///         Override this method only when a subclass needs to change the ORDER of these
    ///         steps relative to its own drawing — e.g. <c>RenderBox</c> overrides
    ///         <see cref="Paint" /> to draw outer box shadows BEFORE the clip is applied so
    ///         shadows can extend outside the box bounds. Subclasses that simply want to draw
    ///         additional content inside the existing pipeline should override
    ///         <see cref="PaintInternal" /> instead.
    ///     </para>
    ///     <para>
    ///         Overrides should reuse <see cref="RecordPaintEvent" /> and
    ///         <see cref="PaintChildren" /> rather than duplicating the bookkeeping and
    ///         child-traversal logic.
    ///     </para>
    /// </summary>
    public virtual void Paint(
        PaintingContext context
    )
    {
        if (context.Canvas == null)
        {
            return;
        }

        MarkPainted();
        RecordPaintEvent(context);

        using (context.Canvas.SaveScope())
        {
            if (ClipBehavior != ClipBehavior.None)
            {
                var rect = Size.ToSkRect(Vector2.Zero);
                context.Canvas.ClipRect(
                    rect,
                    SKClipOperation.Intersect,
                    ClipBehavior == ClipBehavior.AntiAlias
                );
            }

            PaintInternal(context);

            if (!IsRepaintBoundary)
            {
                NeedsPaint = false;
            }

            PaintChildren(context);
        }

        ChildNeedsPaint = false;
    }

    /// <summary>
    ///     Updates repaint heat tracking and invokes <see cref="OnAnyPaint" /> if this
    ///     render object actually needed to repaint this frame. Subclasses overriding
    ///     <see cref="Paint" /> should call this once per paint instead of duplicating
    ///     the bookkeeping inline.
    /// </summary>
    protected void RecordPaintEvent(
        PaintingContext context
    )
    {
        if (!NeedsPaint && !ChildNeedsPaint)
        {
            return;
        }

        RepaintRecord.DirtyPaintedFrameId = CurrentFrameId;
        RepaintRecord.DirtyPaintCount++;
        RepaintRecord.LastEventTimestampMs = context.CurrentTime;
        UpdateHeatWindow();
        OnAnyPaint?.Invoke(this);
    }

    /// <summary>
    ///     Paints every child in order with its local-space translation applied.
    ///     Subclasses overriding <see cref="Paint" /> can reuse this to delegate child
    ///     traversal back to the base implementation.
    /// </summary>
    protected void PaintChildren(
        PaintingContext context
    )
    {
        if (context.Canvas == null)
        {
            return;
        }

        foreach (var child in _children)
        {
            using (context.Canvas.SaveScope())
            {
                context.Canvas.Translate(
                    child.X,
                    child.Y
                );
                child.Paint(context);
            }
        }
    }

    /// <summary>
    ///     Subclass-supplied draw calls. Invoked by <see cref="Paint" /> exactly once per
    ///     frame, at the render object's local origin <c>(0, 0)</c>, inside the canvas
    ///     save scope and AFTER any <see cref="ClipBehavior" /> clip has been applied. The
    ///     base implementation is empty — leaf-style render objects override this to draw
    ///     their fill, border, text, icons, or other content.
    ///     <para>
    ///         Do NOT call <c>canvas.Translate</c> for child positioning here — children are
    ///         painted automatically by <see cref="Paint" /> after this method returns. Do
    ///         not paint outside the bounds defined by <see cref="Size" /> if a parent might
    ///         apply a clip; for content that legitimately escapes the clip (e.g. outer
    ///         shadows), override <see cref="Paint" /> instead so the artwork can be drawn
    ///         BEFORE the clip is applied.
    ///     </para>
    /// </summary>
    protected virtual void PaintInternal(
        PaintingContext context
    )
    {
    }

    /// <summary>
    ///     Walks up to the root and returns its <see cref="ScreenOffset" />.
    /// </summary>
    internal Vector2 GetScreenOffset()
    {
        var cur = this;
        while (cur.Parent != null)
        {
            cur = cur.Parent;
        }

        return cur.ScreenOffset;
    }

    public virtual Vector2 LocalToGlobal(
        Vector2 localPoint
    )
    {
        var point = localPoint + new Vector2(
            X,
            Y
        );
        return Parent?.LocalToGlobal(point) ?? point;
    }

    public virtual Vector2 GlobalToLocal(
        Vector2 globalPoint
    )
    {
        var parentLocal = Parent?.GlobalToLocal(globalPoint) ?? globalPoint;
        return parentLocal - new Vector2(
            X,
            Y
        );
    }

    public virtual void VisitChildren(
        Action<RenderObject> visitor
    )
    {
        foreach (var child in _children)
        {
            visitor(child);
        }
    }

    public virtual void DebugDump(
        Action<string> printer,
        int indent = 0
    )
    {
        var spaces = new string(
            ' ',
            indent * 2
        );
        printer($"{spaces}{GetType().Name} ({X},{Y}) {Size}");
        foreach (var child in _children)
        {
            child.DebugDump(
                printer,
                indent + 1
            );
        }
    }

    /// <summary>
    ///     Transforms <paramref name="position" /> from this object's coordinate space
    ///     to <paramref name="child" />'s coordinate space.
    ///     By default, only subtracts the child's X and Y.
    /// </summary>
    public virtual Vector2 GlobalToChild(
        RenderObject child,
        Vector2 position
    )
    {
        if (child == this)
        {
            return position;
        }

        return position - new Vector2(
            child.X,
            child.Y
        );
    }

    internal void ResetDirtyFlags()
    {
        NeedsLayout = false;
        NeedsPaint = false;
        ChildNeedsLayout = false;
        ChildNeedsPaint = false;
        // WasPaintedThisFrame is now computed via frame-id, no reset needed.

        foreach (var child in _children)
        {
            child.ResetDirtyFlags();
        }
    }

    /// <summary>
    ///     Returns true if <paramref name="position" /> (in local space) falls within this
    ///     object's bounds AND <see cref="IsHitTestTarget" /> is true. Child recursion is
    ///     handled by <c>Element.HitTest</c>, which maintains the RenderObject-to-Element
    ///     mapping required for event dispatch. This method only decides whether THIS node
    ///     itself counts as a hit.
    /// </summary>
    public virtual bool HitTest(
        HitTestResult result,
        Vector2 position,
        Element element
    )
    {
        if (position.X >= 0 && position.X <= Size.X &&
            position.Y >= 0 && position.Y <= Size.Y)
        {
            return IsHitTestTarget;
        }

        return false;
    }
}
