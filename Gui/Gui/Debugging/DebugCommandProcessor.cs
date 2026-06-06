namespace Gui.Debugging;

public class DebugCommandProcessor
{
    private readonly DebugSettings _settings;

    public DebugCommandProcessor(
        DebugSettings settings
    )
    {
        _settings = settings;
    }

    /// <summary>
    ///     Processes a debug subcommand. The optional <paramref name="arg" /> is used by commands
    ///     that accept a parameter (e.g. <c>flash</c> accepts a duration in milliseconds).
    /// </summary>
    public string Process(
        string subCommand,
        string? arg = null
    )
    {
        switch (subCommand?.ToLower())
        {
            case "tree":
                _settings.ShowTree = !_settings.ShowTree;
                return $"ShowTree: {_settings.ShowTree}";
            case "bounds":
                _settings.ShowBounds = !_settings.ShowBounds;
                return $"ShowBounds: {_settings.ShowBounds}";
            case "paint":
                _settings.ShowPaint = !_settings.ShowPaint;
                return $"ShowPaint: {_settings.ShowPaint}";
            case "hud":
                _settings.ShowHud = !_settings.ShowHud;
                return $"ShowHud: {_settings.ShowHud}";
            case "window":
                _settings.ShowWindow = !_settings.ShowWindow;
                return $"ShowWindow: {_settings.ShowWindow}";
            case "violations":
                _settings.ShowViolations = !_settings.ShowViolations;
                return $"ShowViolations: {_settings.ShowViolations}";
            case "heatmap":
                _settings.ShowHeatMap = !_settings.ShowHeatMap;
                return $"ShowHeatMap: {_settings.ShowHeatMap}";
            case "flash":
                if (arg != null && double.TryParse(
                        arg,
                        out var ms
                    ) && ms > 0)
                {
                    _settings.FlashDurationMs = ms;
                    return $"FlashDurationMs set to {ms}ms";
                }

                return $"FlashDurationMs: {_settings.FlashDurationMs}ms (usage: /ui flash 500)";
            case "debugall":
                if (_settings.IsAnyEnabled)
                {
                    _settings.DisableAll();
                }
                else
                {
                    _settings.ShowTree = true;
                    _settings.ShowBounds = true;
                    _settings.ShowPaint = true;
                    _settings.ShowHud = true;
                    _settings.ShowViolations = true;
                    _settings.ShowHeatMap = true;
                }

                return $"AnyEnabled: {_settings.IsAnyEnabled}";
            default:
                return
                    "Unknown subcommand. Available: tree, bounds, paint, hud, violations, heatmap, flash, debugall";
        }
    }
}
