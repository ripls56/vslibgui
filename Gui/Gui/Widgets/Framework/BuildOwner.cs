using System;
using System.Collections.Generic;
using Gui.Clipboard;
using Gui.Sound;
using Gui.Widgets.Animations;
using Gui.Widgets.Input;

namespace Gui.Widgets.Framework;

/// <summary>
///     Manages the widget rebuild pass. Dirty elements are accumulated in a
///     <c>HashSet</c> via <see cref="ScheduleBuildFor" />; <see cref="BuildDirtyElements" />
///     rebuilds them in depth-first order (parents before children) so that parent rebuilds
///     can clear descendant dirty flags and prevent redundant work.
/// </summary>
public class BuildOwner
{
    private readonly List<Element> _buildBuffer = [];
    private readonly HashSet<Element> _dirtyElements = [];
    private readonly Dictionary<GlobalKey, Element> _globalKeys = new();
    private IClipboard? _clipboard;
    private ISoundPlayer? _soundPlayer;
    private ITickerProvider? _tickerProvider;
    public FocusManager? FocusManager { get; set; }

    /// <summary><c>true</c> if at least one element is scheduled for rebuild.</summary>
    public bool HasDirtyElements => _dirtyElements.Count > 0;

    /// <summary>
    ///     Called when an error occurs during an element's rebuild.
    /// </summary>
    public Action<Element, Exception>? OnError { get; set; }

    /// <summary>
    ///     Called when an element is mounted into the tree.
    /// </summary>
    public Action? OnElementMounted { get; set; }

    /// <summary>
    ///     Called when an element is unmounted from the tree.
    /// </summary>
    public Action? OnElementUnmounted { get; set; }

    public void SetTickerProvider(
        ITickerProvider provider
    ) =>
        _tickerProvider = provider;

    public ITickerProvider GetTickerProvider()
    {
        if (_tickerProvider == null)
        {
            throw new InvalidOperationException(
                "TickerProvider not set in BuildOwner. UI must be opened via GuiBase."
            );
        }

        return _tickerProvider;
    }

    public void SetClipboard(
        IClipboard clipboard
    ) =>
        _clipboard = clipboard;

    /// <summary>Returns the <see cref="IClipboard" /> registered for this build owner.</summary>
    public IClipboard GetClipboard()
    {
        if (_clipboard == null)
        {
            throw new InvalidOperationException(
                "Clipboard not set in BuildOwner. UI must be opened via GuiBase."
            );
        }

        return _clipboard;
    }

    public void SetSoundPlayer(
        ISoundPlayer player
    ) =>
        _soundPlayer = player;

    public ISoundPlayer GetSoundPlayer()
    {
        if (_soundPlayer == null)
        {
            throw new InvalidOperationException(
                "SoundPlayer not set in BuildOwner. UI must be opened via GuiBase."
            );
        }

        return _soundPlayer;
    }

    internal void RegisterGlobalKey(
        GlobalKey key,
        Element element
    )
    {
        _globalKeys[key] = element;
        key.CurrentElement = element;
    }

    internal void UnregisterGlobalKey(
        GlobalKey key
    )
    {
        _globalKeys.Remove(key);
        key.CurrentElement = null;
    }

    /// <summary>
    ///     Adds <paramref name="element" /> to the dirty set so it will be rebuilt on the next
    ///     <see cref="BuildDirtyElements" /> call. Called automatically by
    ///     <see cref="Element.MarkNeedsBuild" />.
    /// </summary>
    public void ScheduleBuildFor(
        Element element
    ) =>
        _dirtyElements.Add(element);

    /// <summary>
    ///     Rebuilds all dirty elements. Elements are sorted by depth (shallow first) so that
    ///     parent rebuilds run before children — preventing a child from being rebuilt
    ///     redundantly after its parent already rebuilt it. Loops until no new dirty elements
    ///     remain, handling cascaded rebuilds from animation controllers or state changes
    ///     triggered inside <c>Build()</c>.
    /// </summary>
    public void BuildDirtyElements()
    {
        while (_dirtyElements.Count > 0)
        {
            _buildBuffer.Clear();
            _buildBuffer.AddRange(_dirtyElements);
            _dirtyElements.Clear();

            // Sort by depth: parents (lower depth) before children (higher depth).
            // Skip sort for 0-1 elements since it's a no-op.
            if (_buildBuffer.Count > 1)
            {
                _buildBuffer.Sort((
                        a,
                        b
                    ) => a.Depth.CompareTo(b.Depth)
                );
            }

            foreach (var element in _buildBuffer)
            {
                // Skip if a parent rebuild already rebuilt this element
                // (its dirty flag was cleared by base.Rebuild() during the parent's pass),
                // or if the element was unmounted (e.g. dialog closed between frames).
                if (!element.IsDirty || element.Owner == null)
                {
                    continue;
                }

                if (element is IRebuildable rebuildable)
                {
                    try
                    {
                        rebuildable.Rebuild();
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke(
                            element,
                            ex
                        );
                    }
                }
            }
        }
    }
}

public interface IRebuildable
{
    void Rebuild();
}
