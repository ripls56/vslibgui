using Gui.Core.Framework;

namespace Gui.Debugging;

/// <summary>
///     A render object that initiated dirtying (paint or layout) together with the
///     number of distinct frames on which it did so within the retained window.
/// </summary>
/// <param name="Node">The render object that called MarkNeedsPaint/MarkNeedsLayout.</param>
/// <param name="InitiatedFrames">Distinct frames the node initiated dirtying.</param>
public readonly record struct DirtyHotspot
(
    RenderObject Node,
    int InitiatedFrames
);
