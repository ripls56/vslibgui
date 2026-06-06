using System.Collections.Generic;
using System.Linq;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Debugging;

/// <summary>
///     Root content widget of the debug window. Assembles a window selector, toggle rows,
///     performance metrics, and an optional widget-tree panel based on the current
///     <see cref="DebugSettings" />.
/// </summary>
public class DebugPanel : StatefulWidget
{
    private static readonly TextStyle SectionLabelStyle = new()
    {
        FontSize = 12, Color = new Vector4(0.75f, 0.85f, 1f, 1f)
    };

    /// <summary>Initializes a new <see cref="DebugPanel" />.</summary>
    public DebugPanel(DebugSettings settings)
    {
        Settings = settings;
    }

    /// <summary>Debug settings driving the panel toggles.</summary>
    public DebugSettings Settings { get; }

    /// <inheritdoc />
    public override State CreateState() => new DebugPanelState();

    private class DebugPanelState : State<DebugPanel>
    {
        private int _selectedIndex;

        public override Widget Build(BuildContext context)
        {
            var settings = Widget.Settings;

            var windows = GuiModSystem.Instance?.OpenWindows
                .Where(w => w is not DebugWindow)
                .ToList() ?? new List<GuiBase>();

            if (_selectedIndex >= windows.Count)
            {
                _selectedIndex = windows.Count > 0 ? windows.Count - 1 : 0;
            }

            var selected = windows.Count > 0 ? windows[_selectedIndex] : null;
            var metrics = selected?.PerformanceMetrics;
            var root = selected?.RootElement;

            var children = new List<Widget>();

            if (windows.Count > 0)
            {
                var items = windows.Select((w, i) => new DropdownItem<int>
                {
                    Value = i, Label = w.GetType().Name
                }).ToList();

                children.Add(new Text("Window", SectionLabelStyle));
                children.Add(new Dropdown<int>(
                    _selectedIndex,
                    items,
                    i => SetState(() => _selectedIndex = i)
                ));
                children.Add(new SizedBox(height: 4));
            }

            children.Add(new DebugToggleRow("Show Bounds", settings.ShowBounds,
                v => settings.ShowBounds = v));
            children.Add(new DebugToggleRow("Track Repaints", settings.ShowPaint,
                v => settings.ShowPaint = v));
            children.Add(new DebugToggleRow("Heat Map", settings.ShowHeatMap,
                v => settings.ShowHeatMap = v));
            children.Add(new DebugToggleRow("Violations", settings.ShowViolations,
                v => settings.ShowViolations = v));
            children.Add(
                new DebugToggleRow("Show HUD", settings.ShowHud, v => settings.ShowHud = v));
            children.Add(new DebugToggleRow("Show Tree", settings.ShowTree,
                v => settings.ShowTree = v));
            children.Add(new SizedBox(height: 8));
            children.Add(new Text("Performance", SectionLabelStyle));
            children.Add(new DebugPerformancePanel(metrics));

            if (settings.ShowTree)
            {
                children.Add(new SizedBox(height: 8));
                children.Add(new Text("Widget Tree", SectionLabelStyle));
                children.Add(new DebugTreePanel(root));
            }

            return new Padding(
                EdgeInsets.All(12),
                new Column(
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    spacing: 4,
                    children: children
                )
            );
        }
    }
}
