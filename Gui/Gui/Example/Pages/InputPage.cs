using System.Collections.Generic;
using Gui.Example.Shared;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using Gui.Widgets.Overlay;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using Vintagestory.API.Client;

namespace Gui.Example.Pages;

internal class InputPage : StatefulWidget
{
    public InputPage(ICoreClientAPI capi)
    {
        Capi = capi;
    }

    public ICoreClientAPI Capi { get; }

    public override Widgets.Framework.State CreateState() => new State();

    private class State : State<InputPage>
    {
        private readonly TextEditingController _textController = new("Hello, World!");
        private bool _checked;
        private int _dropdown = 1;
        private float _numeric = 25f;
        private int _radio = 1;
        private float _slider = 0.5f;

        public override Widget Build(BuildContext context)
        {
            var colors = Theme.Of(context).ColorScheme;

            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 16,
                children:
                [
                    new Text("Input",
                        new TextStyle
                        {
                            FontSize = 22, Weight = FontWeight.Bold, Color = colors.Primary
                        }),

                    new DemoCard(
                        "Button",
                        description: "Theme-aware button. Variant controls visual style.",
                        demo: new Row(
                            12,
                            children:
                            [
                                new Button(
                                    new Text("Primary", new TextStyle { Color = colors.OnPrimary })
                                ),
                                new Button(
                                    new Text("Ghost", new TextStyle { Color = colors.Primary }),
                                    ButtonVariant.Ghost
                                )
                            ]
                        ),
                        code: """
                              new Button(
                                new Text("Primary"),
                                variant: ButtonVariant.Primary,
                                onTap:   _ => { /* handle */ }
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "Checkbox",
                        description: "Toggleable checkbox with optional label.",
                        demo: new Checkbox(
                            _checked,
                            label: "Enable feature",
                            onChanged: v => SetState(() => _checked = v)
                        ),
                        code: """
                              new Checkbox(
                                value:     _checked,
                                label:     "Enable feature",
                                onChanged: v => SetState(() => _checked = v)
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "RadioButton",
                        description:
                        "Single-select group. A button is selected when its value equals groupValue.",
                        demo: new Column(
                            crossAxisAlignment: CrossAxisAlignment.Start,
                            spacing: 8,
                            children:
                            [
                                new RadioButton<int>(
                                    0,
                                    _radio,
                                    v => SetState(() => _radio = v),
                                    "Option A"
                                ),
                                new RadioButton<int>(
                                    1,
                                    _radio,
                                    v => SetState(() => _radio = v),
                                    "Option B"
                                ),
                                new RadioButton<int>(
                                    2,
                                    _radio,
                                    v => SetState(() => _radio = v),
                                    "Option C"
                                )
                            ]
                        ),
                        code: """
                              new RadioButton<int>(
                                value:      0,
                                groupValue: _selected,
                                onChanged:  v => SetState(() => _selected = v),
                                label:      "Option A"
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "Dropdown",
                        description:
                        "Opens a floating overlay menu. Shares the Overlay infrastructure with Tooltip.",
                        demo: new SizedBox(
                            220,
                            child: new Dropdown<int>(
                                _dropdown,
                                new List<DropdownItem<int>>
                                {
                                    new() { Value = 1, Label = "Survival" },
                                    new() { Value = 2, Label = "Creative" },
                                    new() { Value = 3, Label = "Spectator" }
                                },
                                v => SetState(() => _dropdown = v)
                            )
                        ),
                        code: """
                              new Dropdown<int>(
                                _selectedValue,
                                new List<DropdownItem<int>>
                                {
                                  new DropdownItem<int> { Value = 1, Label = "Option A" },
                                  new DropdownItem<int> { Value = 2, Label = "Option B" },
                                },
                                v => SetState(() => _selectedValue = v)
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "Slider",
                        description: "Continuous value selector. Use divisions for discrete steps.",
                        demo: new SizedBox(
                            320,
                            child: new Slider(
                                _slider,
                                v => SetState(() => _slider = v)
                            )
                        ),
                        code: """
                              new Slider(
                                value:     _sliderValue,
                                onChanged: v => SetState(() => _sliderValue = v)
                              )

                              // Discrete steps:
                              new Slider(
                                value:     _volume,
                                min:       0,
                                max:       10,
                                divisions: 10,
                                onChanged: v => SetState(() => _volume = v)
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "NumericField",
                        description:
                        "A − / + button pair around a text field. Validates input as a float.",
                        demo: new SizedBox(
                            200,
                            child: new NumericField(
                                _numeric,
                                5f,
                                v => SetState(() => _numeric = v)
                            )
                        ),
                        code: """
                              new NumericField(
                                initialValue: 25f,
                                step:         5f,
                                onChanged:    v => SetState(() => _value = v)
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "TextField",
                        description:
                        "Single-line text input with cursor, caret scrolling, and keyboard handling.",
                        demo: new SizedBox(
                            320,
                            child: new TextField(
                                _textController,
                                style: new BoxStyle
                                {
                                    Height = 35,
                                    Color = new Vector4(0f, 0f, 0f, 0.3f),
                                    BorderThickness = 1f,
                                    BorderColor = colors.Border,
                                    CornerRadius = Vector4.One * 4
                                }
                            )
                        ),
                        code: """
                              // Allocate once (field initializer or InitState):
                              TextEditingController _ctrl = new TextEditingController("initial");

                              new TextField(
                                controller: _ctrl,
                                style: new BoxStyle { Height = 35, BorderThickness = 1 }
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "ProgressBar",
                        description:
                        "Displays a 0–1 progress value. Move the Slider above to change it.",
                        demo: new Column(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            spacing: 8,
                            children:
                            [
                                new ProgressBar(_slider),
                                new ProgressBar(
                                    _slider,
                                    new ProgressBarStyle
                                    {
                                        Height = 14,
                                        CornerRadius = 7,
                                        FillColor = colors.Primary
                                    }
                                )
                            ]
                        ),
                        code: """
                              new ProgressBar(value: _progress)

                              // Custom style:
                              new ProgressBar(
                                value: _progress,
                                style: new ProgressBarStyle { Height = 14, CornerRadius = 7 }
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "Tooltip",
                        description:
                        "Shows a floating bubble above the child after a 500 ms hover delay.",
                        demo: new Tooltip(
                            content: new Text("This is a tooltip!",
                                new TextStyle { FontSize = 12, Color = colors.OnSurface }),
                            child: new Button(
                                new Text("Hover me", new TextStyle { Color = colors.OnPrimary })
                            )
                        ),
                        code: """
                              new Tooltip(
                                content: new Text("Tooltip text"),
                                child:   myWidget,
                                // Optional overrides:
                                waitDuration: TimeSpan.FromMilliseconds(300),
                                fadeDuration: TimeSpan.FromMilliseconds(150),
                                verticalGap:  12f
                              )
                              """,
                        capi: Widget.Capi
                    )
                ]
            );
        }
    }
}
