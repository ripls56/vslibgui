using Gui.Core.Painting;

namespace Gui.Debugging;

/// <summary>
///     Explicit per-frame state of a <see cref="RenderRepaintBoundary" />.
///     Replaces the fragile "which timestamp is smaller" inference that caused incorrect
///     red-on-hit visualization.
/// </summary>
internal enum BoundaryRepaintState
{
    /// <summary>Neither hit nor miss recorded this frame (node not visited or canvas is null).</summary>
    Idle,

    /// <summary>The boundary served its subtree from the cached SKPicture — no re-record needed.</summary>
    CacheHit,

    /// <summary>The SKPicture cache was stale; the subtree was re-recorded this frame.</summary>
    CacheMiss
}
