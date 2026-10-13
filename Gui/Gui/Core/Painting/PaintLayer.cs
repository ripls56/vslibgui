using System;
using SkiaSharp;

namespace Gui.Core.Painting;

/// <summary>
///     A composited paint layer: a stable <see cref="SKDrawable" /> handle
///     (<see cref="Host" />) whose recorded content can be replaced independently of
///     any parent recording that references it.
///     <para>
///         This is what makes layered composition possible. Skia snapshots nested
///         drawables when a recording is finished with <c>EndRecording()</c>
///         (<see cref="SKPicture" />), but keeps them live when it is finished with
///         <c>EndRecordingAsDrawable()</c>. So every layer — including the window root —
///         records into a drawable, and a parent layer only stores a reference to the
///         child's <see cref="Host" />. Re-recording a child layer is then visible on the
///         next playback of the parent without re-recording the parent.
///     </para>
/// </summary>
internal sealed class PaintLayer : IDisposable
{
    private readonly HostDrawable _host;
    private SKDrawable? _content;
    private SKPictureRecorder? _recorder;

    internal PaintLayer() => _host = new HostDrawable(this);

    /// <summary>
    ///     Stable handle to draw into a parent layer. Never replaced for the lifetime
    ///     of this layer, so parent recordings stay valid across re-records.
    /// </summary>
    internal SKDrawable Host => _host;

    /// <summary><c>true</c> once content has been recorded at least once.</summary>
    internal bool HasContent => _content != null;

    /// <summary>Bounds passed to the last <see cref="BeginRecording" /> call.</summary>
    internal SKRect Bounds { get; private set; }

    public void Dispose()
    {
        _recorder?.Dispose();
        _recorder = null;
        _content?.Dispose();
        _content = null;
        _host.Dispose();
    }

    /// <summary>
    ///     Starts recording new content. Must be paired with <see cref="EndRecording" />.
    /// </summary>
    internal SKCanvas BeginRecording
    (
        SKRect bounds
    )
    {
        Bounds = bounds;
        _recorder?.Dispose();
        _recorder = new SKPictureRecorder();
        return _recorder.BeginRecording(bounds);
    }

    /// <summary>
    ///     Finishes recording and swaps in the new content, disposing the previous one.
    ///     The old content is only ever referenced by <see cref="Host" />, so parent
    ///     recordings are unaffected.
    /// </summary>
    internal void EndRecording()
    {
        if (_recorder == null)
        {
            return;
        }

        var next = _recorder.EndRecordingAsDrawable();
        _recorder.Dispose();
        _recorder = null;

        var previous = _content;
        _content = next;
        previous?.Dispose();
        _host.NotifyDrawingChanged();
    }

    private sealed class HostDrawable : SKDrawable
    {
        private readonly PaintLayer _owner;

        internal HostDrawable
        (
            PaintLayer owner
        ) =>
            _owner = owner;

        protected override void OnDraw
        (
            SKCanvas canvas
        )
        {
            var content = _owner._content;
            if (content != null)
            {
                canvas.DrawDrawable
                (
                    content,
                    0,
                    0
                );
            }
        }

        protected override SKRect OnGetBounds() => _owner.Bounds;
    }
}
