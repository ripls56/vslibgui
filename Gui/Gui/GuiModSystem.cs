using System.Collections.Generic;
using System.IO;
using Gui.Debugging;
using Gui.Example;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Framework;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Gui;

public class GuiModSystem : ModSystem
{
    private readonly List<GuiBase> _openWindows = new();
    private FileSystemWatcher? _configWatcher;
    private DebugCommandProcessor? _debugCommandProcessor;
    private DebugWindow? _debugWindow;
    private ExampleGui? _exampleGui;
    private GuiGlobalOverlay? _globalOverlay;
    private PostSkiaPipeline? _postSkiaPipeline;

    public static GuiModSystem? Instance { get; private set; }

    /// <summary>All currently open Library windows.</summary>
    public IReadOnlyList<GuiBase> OpenWindows => _openWindows;

    /// <summary>The most recently interacted window (used by debug tooling).</summary>
    public GuiBase? ActiveWindow { get; set; }

    /// <summary>The client API instance, available after StartClientSide.</summary>
    public ICoreClientAPI? Capi { get; private set; }

    public SkiaRenderer? SkiaRenderer { get; private set; }
    public SkiaAssetLoader? SkiaAssetLoader { get; private set; }
    public ItemStackRenderer? ItemStackRenderer { get; private set; }
    public VsIconTextureCache? VsIconTextureCache { get; private set; }

    /// <summary>
    ///     Schedules all <see cref="IPreSkiaRenderer" /> instances once per frame
    ///     before Skia acquires the GL context. Register new pre-Skia GL renderers
    ///     here instead of adding calls to <c>GuiBase.OnRenderGUI</c>.
    /// </summary>
    public PreSkiaPipeline? PreSkiaPipeline { get; private set; }

    public DebugSettings DebugSettings { get; } = new();

    /// <summary>The active theme. Reflects the current <see cref="ThemeData.Default" />.</summary>
    public ThemeData ActiveTheme => ThemeData.Default;

    public PerformanceMetrics? PerformanceMetrics => ActiveWindow?.PerformanceMetrics;

    public void RegisterWindow(
        GuiBase window
    )
    {
        if (!_openWindows.Contains(window))
        {
            _openWindows.Add(window);
        }

        ActiveWindow ??= window;
    }

    public void UnregisterWindow(
        GuiBase window
    )
    {
        _openWindows.Remove(window);
        if (ActiveWindow == window)
        {
            ActiveWindow = _openWindows.Count > 0
                ? _openWindows[^1]
                : null;
        }
    }

    public override void StartClientSide(
        ICoreClientAPI api
    )
    {
        NativeLibraryLoader.Register();
        Instance = this;
        Capi = api;
        SkiaRenderer = new SkiaRenderer();
        SkiaAssetLoader = new SkiaAssetLoader(api);
        ItemStackRenderer = new ItemStackRenderer(api);
        VsIconTextureCache = new VsIconTextureCache(api);
        PreSkiaPipeline = new PreSkiaPipeline(SkiaRenderer, api);
        PreSkiaPipeline.Register(ItemStackRenderer);
        _postSkiaPipeline = new PostSkiaPipeline(SkiaRenderer, PreSkiaPipeline);
        api.Event.RegisterRenderer(PreSkiaPipeline, EnumRenderStage.Ortho);
        api.Event.RegisterRenderer(_postSkiaPipeline, EnumRenderStage.Ortho);
        _debugCommandProcessor = new DebugCommandProcessor(DebugSettings);
        _debugWindow = new DebugWindow(api, DebugSettings);

        api.Assets.AddModOrigin(
            "gui",
            "fonts"
        );
        api.Assets.Reload(
            new AssetLocation(
                "gui",
                "fonts"
            )
        );
        LoadFonts(SkiaAssetLoader);
        FontRegistry.RegisterFontAlias("sans-serif", GuiStyle.StandardFontName);
        FontRegistry.RegisterFontAlias("serif", GuiStyle.DecorativeFontName);
        RegisterDebugCommands(api);
        LoadThemeConfig();
        SetupConfigWatcher();
        _globalOverlay = new GuiGlobalOverlay(api);
        _globalOverlay.TryOpen(false);
    }

    private static void LoadFonts(SkiaAssetLoader loader)
    {
        var fonts = new (string path, string family, FontWeight weight)[]
        {
            ("fonts/CormorantUnicase-Regular.ttf", "Cormorant Unicase", FontWeight.Normal),
            ("fonts/CormorantUnicase-SemiBold.ttf", "Cormorant Unicase", FontWeight.SemiBold),
            ("fonts/CormorantUnicase-Bold.ttf", "Cormorant Unicase", FontWeight.Bold),
            ("fonts/JetBrainsMono-Regular.ttf", "JetBrains Mono", FontWeight.Normal),
            ("fonts/JetBrainsMono-SemiBold.ttf", "JetBrains Mono", FontWeight.SemiBold),
            ("fonts/JetBrainsMono-Bold.ttf", "JetBrains Mono", FontWeight.Bold),
            ("fonts/JetBrainsMono-Italic.ttf", "JetBrains Mono", FontWeight.Italic),
            ("fonts/PlayfairDisplay-Regular.ttf", "Playfair Display", FontWeight.Normal),
            ("fonts/PlayfairDisplay-SemiBold.ttf", "Playfair Display", FontWeight.SemiBold),
            ("fonts/PlayfairDisplay-Bold.ttf", "Playfair Display", FontWeight.Bold),
            ("fonts/PlayfairDisplay-Italic.ttf", "Playfair Display", FontWeight.Italic)
        };

        foreach (var (path, family, weight) in fonts)
        {
            var typeface = loader.LoadFont("gui", path);
            if (typeface != null)
            {
                FontRegistry.RegisterCustomFont(family, weight, typeface);
            }
        }
    }

    private void RegisterDebugCommands(
        ICoreClientAPI capi
    )
    {
        var cmd = capi.ChatCommands.Create("ui")
            .WithDescription("UI Debugging")
            .HandleWith(args =>
                TextCommandResult.Success(
                    "Try /ui tree, bounds, paint, hud, redraw, recreate, debugall"));

        // Simple toggle subcommands
        foreach (var sub in new[]
                 {
                     "tree", "bounds", "paint", "hud", "window", "violations", "heatmap",
                     "debugall"
                 })
        {
            cmd.BeginSubCommand(sub)
                .HandleWith(args =>
                    {
                        var result = _debugCommandProcessor?.Process(sub) ?? "Error";
                        SyncDebugWindow();
                        return TextCommandResult.Success(result);
                    }
                )
                .EndSubCommand();
        }

        // redraw — force-rebuild all open window widget trees
        cmd.BeginSubCommand("redraw")
            .HandleWith(args =>
                {
                    var count = 0;
                    foreach (var window in _openWindows)
                    {
                        window.ForceRebuild();
                        count++;
                    }

                    return TextCommandResult.Success($"Rebuilt {count} window(s).");
                }
            )
            .EndSubCommand();

        // recreate — fully dispose and reopen all windows
        cmd.BeginSubCommand("recreate")
            .HandleWith(args =>
                {
                    var snapshot = new List<GuiBase>(_openWindows);
                    foreach (var window in snapshot)
                    {
                        window.TryClose();
                    }

                    var count = 0;
                    foreach (var window in snapshot)
                    {
                        window.TryOpen();
                        count++;
                    }

                    return TextCommandResult.Success(
                        $"Recreated {count} window(s)."
                    );
                }
            )
            .EndSubCommand();

        // showcase — interactive widget documentation
        cmd.BeginSubCommand("showcase")
            .HandleWith(args =>
                {
                    if (_exampleGui != null && _exampleGui.IsOpened())
                    {
                        _exampleGui.TryClose();
                        _exampleGui = null;
                    }
                    else
                    {
                        _exampleGui = new ExampleGui(capi);
                        _exampleGui.TryOpen();
                    }

                    return TextCommandResult.Success("Showcase toggled.");
                }
            )
            .EndSubCommand();

        // flash <ms> — configures the repaint flash duration
        cmd.BeginSubCommand("flash")
            .WithArgs(
                new StringArgParser(
                    "ms",
                    false
                )
            )
            .HandleWith(args =>
                {
                    var msArg = args.Parsers?.Count > 0
                        ? args.Parsers[0].GetValue()?.ToString()
                        : null;
                    var result = _debugCommandProcessor?.Process(
                        "flash",
                        msArg
                    ) ?? "Error";
                    return TextCommandResult.Success(result);
                }
            )
            .EndSubCommand();
    }

    private void LoadThemeConfig()
    {
        GuiConfig? config;
        try
        {
            config = Capi!.LoadModConfig<GuiConfig>("libgui.json");
        }
        catch (IOException)
        {
            return;
        }

        if (config == null)
        {
            Capi.StoreModConfig(
                new GuiConfig { Theme = ThemeSection.FromColorScheme(ColorScheme.Default()) },
                "libgui.json"
            );
            ThemeData.Default = new ThemeData();
            return;
        }

        ThemeData.Default = config.Theme != null
            ? new ThemeData(config.Theme.ToColorScheme(ColorScheme.Default()))
            : new ThemeData();
    }

    private void SetupConfigWatcher()
    {
        var configDir = Path.Combine(Capi!.DataBasePath, "ModConfig");
        if (!Directory.Exists(configDir))
        {
            return;
        }

        _configWatcher = new FileSystemWatcher(configDir, "libgui.json")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };
        _configWatcher.Changed += OnConfigFileChanged;
        _configWatcher.Created += OnConfigFileChanged;
        _configWatcher.Renamed += (_, e) =>
        {
            if (e.Name == "libgui.json")
            {
                OnConfigChanged();
            }
        };
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e) => OnConfigChanged();

    private void OnConfigChanged() =>
        Capi!.Event.EnqueueMainThreadTask(LoadThemeConfig, "libgui-theme-reload");

    private void SyncDebugWindow()
    {
        if (_debugWindow == null)
        {
            return;
        }

        if (DebugSettings.ShowWindow && !_debugWindow.IsOpened())
        {
            _debugWindow.TryOpen();
        }
        else if (!DebugSettings.ShowWindow && _debugWindow.IsOpened())
        {
            _debugWindow.TryClose();
        }
    }

    public override void Dispose()
    {
        _configWatcher?.Dispose();
        _configWatcher = null;
        _globalOverlay?.TryClose();
        _globalOverlay?.Dispose();
        _globalOverlay = null;
        SvgPictureCache.Clear();
        if (PreSkiaPipeline != null && Capi != null)
        {
            Capi.Event.UnregisterRenderer(PreSkiaPipeline, EnumRenderStage.Ortho);
        }

        if (_postSkiaPipeline != null && Capi != null)
        {
            Capi.Event.UnregisterRenderer(_postSkiaPipeline, EnumRenderStage.Ortho);
        }

        PreSkiaPipeline?.Dispose();
        _postSkiaPipeline?.Dispose();
        ItemStackRenderer?.Dispose();
        VsIconTextureCache?.Dispose();
        SkiaRenderer?.Dispose();
        base.Dispose();
    }
}
