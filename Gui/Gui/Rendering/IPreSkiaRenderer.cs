namespace Gui.Rendering;

/// <summary>
///     Contract for GL renderers that must execute before
///     <see cref="SkiaRenderer.Begin" /> takes over the GL context.
///     Register implementations with <see cref="PreSkiaPipeline" />.
/// </summary>
public interface IPreSkiaRenderer
{
    /// <summary>Whether any work is queued for this frame.</summary>
    bool HasPendingRequests { get; }

    /// <summary>
    ///     Processes all queued GL work. Called once per frame by
    ///     <see cref="PreSkiaPipeline" /> before Skia acquires the context.
    /// </summary>
    void ProcessQueue();
}
