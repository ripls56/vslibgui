using System;
using OpenTK.Mathematics;

namespace Gui;

/// <summary>
///     Describes the window behavior for a <see cref="GuiBase" /> instance:
///     position, size, drag/resize.
///     <para>
///         For dialog type (Dialog vs HUD), override <see cref="GuiBase.DialogType" /> directly.
///     </para>
/// </summary>
public class WindowConfig
{
    /// <summary>
    ///     Initial position in logical (UI-scaled) pixels. Null = centered on screen.
    /// </summary>
    public Vector2? Position { get; set; }

    /// <summary>
    ///     Fixed window size in logical pixels. Null = shrink-wrap to content.
    /// </summary>
    public Vector2? Size { get; set; }

    /// <summary>Minimum window size for resize clamping.</summary>
    public Vector2 MinSize { get; set; } = new(
        100,
        50
    );

    /// <summary>Maximum window size for resize clamping.</summary>
    public Vector2 MaxSize { get; set; } = new(
        float.PositiveInfinity,
        float.PositiveInfinity
    );

    /// <summary>Whether the user can drag the window by the top handle area.</summary>
    public bool Draggable { get; set; } = true;

    /// <summary>Whether the user can resize the window by dragging edges/corners.</summary>
    public bool Resizable { get; set; } = true;

    /// <summary>Height of the top drag handle zone in logical pixels.</summary>
    public float DragHandleHeight { get; set; } = 24f;

    /// <summary>Width of the edge resize grip zone in logical pixels.</summary>
    public float ResizeHandleSize { get; set; } = 8f;

    public bool IsShrinkWrap => Size == null;

    /// <summary>
    ///     When true (default), content is clipped to the window bounds. Set to false to let
    ///     content (e.g. an oversized image) overflow and paint beyond the window rectangle.
    /// </summary>
    public bool Clip { get; set; } = true;

    /// <summary>
    ///     When true (default), any mouse-down inside the window bounds consumes the event even if
    ///     no widget or drag zone handled it. This prevents clicks from falling through to windows
    ///     behind this one and ensures this window always gains focus on click.
    ///     Set to false for transparent/overlay windows that should pass clicks through.
    /// </summary>
    public bool OpaqueHitTest { get; set; } = true;

    /// <summary>
    ///     Called when the user clicks outside the window bounds.
    ///     Use this to close or unfocus the dialog.
    ///     <para>Note: <see cref="GuiBase.FocusManager" /> focus is always cleared automatically.</para>
    /// </summary>
    public Action? OnPointerDownOutside { get; set; }
}

[Flags]
public enum ResizeEdge
{
    None = 0,
    Left = 1,
    Right = 2,
    Top = 4,
    Bottom = 8
}
