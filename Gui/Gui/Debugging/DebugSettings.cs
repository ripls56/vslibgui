using Gui.Widgets.Framework;

namespace Gui.Debugging;

/// <summary>
///     Holds debug overlay flags. Extends <see cref="ChangeNotifier" /> so the debug window
///     panel rebuilds reactively when any flag changes.
/// </summary>
public class DebugSettings : ChangeNotifier
{
    private bool _showBounds;
    private bool _showHeatMap;
    private bool _showHud;
    private bool _showPaint;
    private bool _showTree;
    private bool _showViolations;
    private bool _showWindow;

    /// <summary>Renders the widget/element/render-object tree as a scrollable text panel.</summary>
    public bool ShowTree
    {
        get => _showTree;
        set
        {
            if (_showTree == value)
            {
                return;
            }

            _showTree = value;
            NotifyListeners();
        }
    }

    /// <summary>Draws cyan/magenta bounding-box overlays on every RenderObject.</summary>
    public bool ShowBounds
    {
        get => _showBounds;
        set
        {
            if (_showBounds == value)
            {
                return;
            }

            _showBounds = value;
            NotifyListeners();
        }
    }

    /// <summary>Flashes orange when a node repaints; enables the repaint heat map.</summary>
    public bool ShowPaint
    {
        get => _showPaint;
        set
        {
            if (_showPaint == value)
            {
                return;
            }

            _showPaint = value;
            NotifyListeners();
        }
    }

    /// <summary>Enables HUD debug rendering.</summary>
    public bool ShowHud
    {
        get => _showHud;
        set
        {
            if (_showHud == value)
            {
                return;
            }

            _showHud = value;
            NotifyListeners();
        }
    }

    /// <summary>Whether the debug window itself is open.</summary>
    public bool ShowWindow
    {
        get => _showWindow;
        set
        {
            if (_showWindow == value)
            {
                return;
            }

            _showWindow = value;
            NotifyListeners();
        }
    }

    /// <summary>
    ///     When enabled, layout violations (invalid constraints, size overflows, RenderFlex overflow)
    ///     are logged via <c>capi.Logger.Warning</c> and highlighted with a red overlay in the debug
    ///     painter. Toggle with <c>/ui violations</c>.
    /// </summary>
    public bool ShowViolations
    {
        get => _showViolations;
        set
        {
            if (_showViolations == value)
            {
                return;
            }

            _showViolations = value;
            NotifyListeners();
        }
    }

    /// <summary>
    ///     When enabled alongside <see cref="ShowPaint" />, nodes that repaint frequently are tinted
    ///     blue→red proportionally to their repaint frequency over a rolling 60-frame window.
    ///     Toggle with <c>/ui heatmap</c>.
    /// </summary>
    public bool ShowHeatMap
    {
        get => _showHeatMap;
        set
        {
            if (_showHeatMap == value)
            {
                return;
            }

            _showHeatMap = value;
            NotifyListeners();
        }
    }

    /// <summary>
    ///     Duration in milliseconds for the repaint flash overlay to fade out.
    ///     Default 500 ms. Set via <c>/ui flash &lt;ms&gt;</c>.
    /// </summary>
    public double FlashDurationMs
    {
        get;
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            NotifyListeners();
        }
    } = 500.0;

    /// <summary>Returns <see langword="true" /> if any debug flag is active.</summary>
    public bool IsAnyEnabled =>
        _showTree || _showBounds || _showPaint || _showHud || _showWindow || _showViolations ||
        _showHeatMap;

    /// <summary>Turns off all debug flags and fires a single notification.</summary>
    public void DisableAll()
    {
        _showTree = false;
        _showBounds = false;
        _showPaint = false;
        _showHud = false;
        _showWindow = false;
        _showViolations = false;
        _showHeatMap = false;
        NotifyListeners();
    }
}
