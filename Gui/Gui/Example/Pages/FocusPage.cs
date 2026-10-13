using System;
using Gui.Example.Shared;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;
using Vintagestory.API.Client;

namespace Gui.Example.Pages;

internal class FocusPage : StatefulWidget
{
    public FocusPage(ICoreClientAPI capi)
    {
        Capi = capi;
    }

    public ICoreClientAPI Capi { get; }

    public override Widgets.Framework.State CreateState() => new State();

    private class State : State<FocusPage>
    {
        // Card 1 — plain document (DFS) traversal order. Tab walks A → B → C.
        private readonly FocusNode _a = new();
        private readonly FocusNode _b = new();
        private readonly FocusNode _c = new();
        private readonly FocusNode _disabled = new()
        {
            CanRequestFocus = false
        };

        // Card 4 — FocusScope boundaries. Each group is a scope; Tab never leaves it.
        private readonly FocusNode _g1x = new();
        private readonly FocusNode _g1y = new();
        private readonly FocusNode _g2w = new();
        private readonly FocusNode _g2z = new();

        // Card 3 — exclusion flags. SkipTraversal skips Tab but stays clickable;
        // CanRequestFocus=false removes the node from focus entirely.
        private readonly FocusNode _normal = new();

        // Card 2 — TraversalOrder override. Visually A, B, C, but Tab walks C → A → B.
        private readonly FocusNode _oA = new()
        {
            TraversalOrder = 2f
        };
        private readonly FocusNode _oB = new()
        {
            TraversalOrder = 3f
        };
        private readonly FocusNode _oC = new()
        {
            TraversalOrder = 1f
        };
        private readonly FocusNode _skipped = new()
        {
            SkipTraversal = true
        };

        public override Widget Build(BuildContext context)
        {
            var theme = Theme.Of(context);
            var colors = theme.ColorScheme;
            var headerStyle = theme.TextTheme.Headline;
            headerStyle.Color = colors.Primary;

            return new Column
            (
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 16,
                children:
                [
                    new Text("Focus & Traversal", headerStyle),

                    new Text
                    (
                        "Press Tab / Shift+Tab. The dot marks the node that holds focus.",
                        new TextStyle
                        {
                            FontSize = 13, Color = colors.OnSurfaceVariant
                        }
                    ),

                    new DemoCard
                    (
                        "1. Tab order = document order (DFS)",
                        description:
                        "Traversal follows the widget tree top-to-bottom, NOT the visual position. " +
                        "Each TextField owns a FocusNode; Tab calls FocusManager.FocusNext over that order. " +
                        "The Button below is not IFocusable yet, so Tab skips straight past it.",
                        demo: new Column
                        (
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            spacing: 8,
                            children:
                            [
                                new FocusRow("Field A", _a, colors),
                                new FocusRow("Field B", _b, colors),
                                new FocusRow("Field C", _c, colors),
                                new Button
                                (
                                    new Text
                                    (
                                        "Button (Tab skips it)",
                                        new TextStyle
                                        {
                                            Color = colors.Primary
                                        }
                                    ),
                                    ButtonVariant.Ghost
                                )
                            ]
                        ),
                        code: """
                              FocusNode a = new();
                              FocusNode b = new();

                              new TextField(focusNode: a);
                              new TextField(focusNode: b);
                              // Tab → a, then b (tree order). Shift+Tab reverses.
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard
                    (
                        "2. TraversalOrder overrides the order",
                        description:
                        "Fields sit in order A, B, C — but TraversalOrder pins C first. " +
                        "This proves the order is a property of the node, not the layout.",
                        demo: new Column
                        (
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            spacing: 8,
                            children:
                            [
                                new FocusRow("A (order 2)", _oA, colors),
                                new FocusRow("B (order 3)", _oB, colors),
                                new FocusRow("C (order 1)", _oC, colors)
                            ]
                        ),
                        code: """
                              // Lower TraversalOrder is visited first.
                              new FocusNode { TraversalOrder = 1f }; // visited first
                              new FocusNode { TraversalOrder = 3f }; // visited last
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard
                    (
                        "3. SkipTraversal & CanRequestFocus",
                        description:
                        "SkipTraversal: Tab jumps past it, but a click still focuses it. " +
                        "CanRequestFocus=false: focus is refused entirely, even by click.",
                        demo: new Column
                        (
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            spacing: 8,
                            children:
                            [
                                new FocusRow("Normal", _normal, colors),
                                new FocusRow("SkipTraversal (click me)", _skipped, colors),
                                new FocusRow("CanRequestFocus=false", _disabled, colors)
                            ]
                        ),
                        code: """
                              new FocusNode { SkipTraversal = true };    // Tab skips, click works
                              new FocusNode { CanRequestFocus = false }; // never focuses
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard
                    (
                        "4. FocusScope traps traversal",
                        description:
                        "Each group lives in its own FocusScope. Click a field in Group 1, " +
                        "then Tab — focus bounces X ↔ Y and never crosses into Group 2.",
                        demo: new Row
                        (
                            24,
                            crossAxisAlignment: CrossAxisAlignment.Start,
                            children:
                            [
                                new Expanded
                                (
                                    new FocusScope
                                    {
                                        Child = ScopeGroup("Group 1", _g1x, _g1y, colors)
                                    }
                                ),
                                new Expanded
                                (
                                    new FocusScope
                                    {
                                        Child = ScopeGroup("Group 2", _g2z, _g2w, colors)
                                    }
                                )
                            ]
                        ),
                        code: """
                              new FocusScope
                              {
                                  Child = new Column(children: [ fieldX, fieldY ])
                              };
                              // Focus never leaves this scope under Tab.
                              """,
                        capi: Widget.Capi
                    )
                ]
            );
        }

        private static Column ScopeGroup
        (
            string label,
            FocusNode first,
            FocusNode second,
            ColorScheme colors
        ) =>
            new
            (
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children:
                [
                    new Text
                    (
                        label,
                        new TextStyle
                        {
                            FontSize = 12, Weight = FontWeight.Bold, Color = colors.OnSurfaceVariant
                        }
                    ),
                    new FocusRow("X", first, colors),
                    new FocusRow("Y", second, colors)
                ]
            );
    }
}

/// <summary>
///     A labelled <see cref="TextField" /> bound to a <see cref="FocusNode" />. Shows a "focus" badge
///     and a highlighted border when its node holds primary focus, so traversal is visible.
/// </summary>
internal class FocusRow : StatefulWidget
{
    public FocusRow
    (
        string label,
        FocusNode node,
        ColorScheme colors
    )
    {
        Label = label;
        Node = node;
        Colors = colors;
    }

    public string Label { get; }
    public FocusNode Node { get; }
    public ColorScheme Colors { get; }

    public override State CreateState() => new FocusRowState();
}

internal class FocusRowState : State<FocusRow>
{
    private readonly TextEditingController _controller = new();
    private Action? _listener;

    public override void InitState()
    {
        base.InitState();
        _listener = () => SetState(() => { });
        Widget.Node.AddListener(_listener);
    }

    public override void Dispose()
    {
        if (_listener != null)
        {
            Widget.Node.RemoveListener(_listener);
        }

        _controller.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        var c = Widget.Colors;
        var focused = Widget.Node.HasFocus;

        var labelColor = focused
            ? c.Primary
            : new Vector4(c.OnSurface.X, c.OnSurface.Y, c.OnSurface.Z, 0.6f);

        return new Row
        (
            12,
            crossAxisAlignment: CrossAxisAlignment.Center,
            children:
            [
                new SizedBox
                (
                    170,
                    child: new Text
                    (
                        Widget.Label,
                        new TextStyle
                        {
                            FontSize = 13,
                            Weight = focused ? FontWeight.Bold : FontWeight.Normal,
                            Color = labelColor
                        }
                    )
                ),
                new Expanded
                (
                    new TextField
                    (
                        _controller,
                        Widget.Node,
                        TextFieldStyle.Default(c, TextTheme.Default()) with
                        {
                            Height = 32,
                            FillColor = new Vector4(0f, 0f, 0f, 0.3f),
                            BorderThickness = focused ? 2f : 1f,
                            BorderColor = focused ? c.Primary : c.Border
                        }
                    )
                ),
                new SizedBox
                (
                    72,
                    child: focused
                        ? new Text
                        (
                            "● focus",
                            new TextStyle
                            {
                                FontSize = 12, Color = c.Primary
                            }
                        )
                        : new SizedBox(0)
                )
            ]
        );
    }
}
