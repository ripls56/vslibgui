using Gui.Example.Pages;
using Gui.Example.Sidebar;
using Gui.Rendering;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using Gui.Widgets.Scroll;
using Vintagestory.API.Client;

namespace Gui.Example;

internal class ExampleContent : StatefulWidget
{
    public ExampleContent(ICoreClientAPI capi)
    {
        Capi = capi;
    }

    public ICoreClientAPI Capi { get; }

    public override Widgets.Framework.State CreateState() => new State();

    private class State : State<ExampleContent>
    {
        // Collapsed sidebar = 48 px icon; expanded = 164 px (label 116 + icon 48).
        // x=0..116 — transparent (game world shows through).
        // x=116..164 — icon sits on dark background, straddles content edge.
        // x=164..end — scrollable page content.
        private const float IconWidth = 48f;
        private const float ExpandedWidth = 164f;
        private const float TransparentW = ExpandedWidth - IconWidth; // 116

        private int _page;

        private Widget CurrentPage()
        {
            return _page switch
            {
                0 => new LayoutPage(Widget.Capi),
                1 => new InputPage(Widget.Capi),
                2 => new ScrollPage(Widget.Capi),
                3 => new AnimationsPage(Widget.Capi),
                4 => new TypographyPage(Widget.Capi),
                5 => new InventoryPage(Widget.Capi),
                6 => new ThemingPage(Widget.Capi),
                _ => new InventoryPage(Widget.Capi)
            };
        }

        public override Widget Build(BuildContext context)
        {
            var bg = Theme.Of(context).ColorScheme.Background;

            return
                new Stack(
                    [
                        // Dark window background + scrollable content.
                        // Starts at ExpandedWidth so the entire sidebar area (x=0..164) is
                        // transparent — the game world renders behind both the icon and label.
                        // ResizeBorder is placed here so only the content area is resizable,
                        // not the transparent sidebar strip.
                        new Positioned(
                            ExpandedWidth,
                            0,
                            0,
                            0,
                            child: new Container(
                                new BoxStyle { Color = bg },
                                new SingleChildScrollView(
                                    new Padding(
                                        EdgeInsets.All(24),
                                        CurrentPage()
                                    )
                                )
                            )
                        ),

                        // Sidebar — floats at left:0, expands rightward up to ExpandedWidth.
                        // Items are right-aligned so the icon always sits at x=TransparentW..ExpandedWidth.
                        new Positioned(
                            0,
                            0,
                            bottom: 0,
                            child: new ExampleSidebar(
                                _page,
                                idx => SetState(() => _page = idx)
                            )
                        )
                    ]
                );
        }
    }
}
