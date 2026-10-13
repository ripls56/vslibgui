using Gui.Example.Shared;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Vintagestory.API.Client;

namespace Gui.Example.Pages;

/// <summary>
///     Demo page showing Unicode / mixed-script text rendering via HarfBuzz font shaping.
/// </summary>
internal class TypographyPage : StatelessWidget
{
    public TypographyPage(ICoreClientAPI capi)
    {
        Capi = capi;
    }

    public ICoreClientAPI Capi { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var colors = theme.ColorScheme;
        var headerStyle = theme.TextTheme.Headline;
        headerStyle.Color = colors.Primary;

        var longText =
            "The quick brown fox jumps over the lazy dog. Pack my box with five dozen liquor jugs.";

        var vtml =
            """
            Hello <strong>bold</strong> and <i>italic</i><br>
            <font color="#ff0000" size="20">Red big text</font><br>
            <a href="handbook://page">Click me</a><br>
            <icon name="dice"></icon> inline icon<br>
            <itemstack type="item" code="game:stick" floattype="left"></itemstack>
            Text wraps around the floated item stack.
            """;

        return new Column
        (
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 16,
            children:
            [
                new Text("Typography", headerStyle),

                new DemoCard
                (
                    "Overflow — Ellipsis",
                    description: "MaxLines = 1 with Overflow.Ellipsis truncates text and appends …",
                    demo: new Text
                    (
                        longText,
                        new TextStyle
                        {
                            FontSize = 14,
                            Color = colors.OnSurface,
                            MaxLines = 1,
                            Overflow = TextOverflow.Ellipsis
                        }
                    ),
                    code: """
                          new TextStyle { MaxLines = 1, Overflow = TextOverflow.Ellipsis }
                          """,
                    capi: Capi
                ),

                new DemoCard
                (
                    "Overflow — Clip",
                    description:
                    "MaxLines = 1 with Overflow.Clip hard-clips without any indicator.",
                    demo: new Text
                    (
                        longText,
                        new TextStyle
                        {
                            FontSize = 14,
                            Color = colors.OnSurface,
                            MaxLines = 1,
                            Overflow = TextOverflow.Clip
                        }
                    ),
                    code: """
                          new TextStyle { MaxLines = 1, Overflow = TextOverflow.Clip }
                          """,
                    capi: Capi
                ),

                new DemoCard
                (
                    "MaxLines = 3",
                    description: "Wraps normally but stops at 3 lines, then clips.",
                    demo: new Text
                    (
                        longText + " " + longText,
                        new TextStyle
                        {
                            FontSize = 14,
                            Color = colors.OnSurface,
                            MaxLines = 3,
                            Overflow = TextOverflow.Ellipsis
                        }
                    ),
                    code: """
                          new TextStyle { MaxLines = 3, Overflow = TextOverflow.Ellipsis }
                          """,
                    capi: Capi
                ),

                new DemoCard
                (
                    "SoftWrap = false",
                    description: "Disables line wrapping — text overflows in a single run.",
                    demo: new Text
                    (
                        longText,
                        new TextStyle
                        {
                            FontSize = 14,
                            Color = colors.OnSurface,
                            SoftWrap = false,
                            Overflow = TextOverflow.Clip
                        }
                    ),
                    code: """
                          new TextStyle { SoftWrap = false, Overflow = TextOverflow.Clip }
                          """,
                    capi: Capi
                ),

                new DemoCard
                (
                    "Latin",
                    description: "Standard ASCII text shaped with primary font.",
                    demo: new Text
                    (
                        "The quick brown fox jumps over the lazy dog.",
                        new TextStyle
                        {
                            FontSize = 14, Color = colors.OnSurface
                        }
                    ),
                    code: """new Text("The quick brown fox jumps over the lazy dog.")""",
                    capi: Capi
                ),

                new DemoCard
                (
                    "CJK — Chinese",
                    description: "Chinese ideographs shaped via system fallback font.",
                    demo: new Text
                    (
                        "你好，世界！天气很好。",
                        new TextStyle
                        {
                            FontSize = 14, Color = colors.OnSurface
                        }
                    ),
                    code: """new Text("你好，世界！天气很好。")""",
                    capi: Capi
                ),

                new DemoCard
                (
                    "CJK — Japanese",
                    description: "Hiragana, Katakana and kanji via fallback.",
                    demo: new Text
                    (
                        "こんにちは世界。テスト文字列。",
                        new TextStyle
                        {
                            FontSize = 14, Color = colors.OnSurface
                        }
                    ),
                    code: """new Text("こんにちは世界。テスト文字列。")""",
                    capi: Capi
                ),

                new DemoCard
                (
                    "Mixed Latin + CJK",
                    description:
                    "Latin and CJK in one run — FontRunSplitter segments by typeface, HarfBuzz shapes each.",
                    demo: new Text
                    (
                        "Hello 世界! Inventory (物品栏) level 42.",
                        new TextStyle
                        {
                            FontSize = 14, Color = colors.OnSurface
                        }
                    ),
                    code: """new Text("Hello 世界! Inventory (物品栏) level 42.")""",
                    capi: Capi
                ),

                new DemoCard
                (
                    "Korean (Hangul)",
                    description: "Hangul syllable blocks via system fallback.",
                    demo: new Text
                    (
                        "안녕하세요 세계!",
                        new TextStyle
                        {
                            FontSize = 14, Color = colors.OnSurface
                        }
                    ),
                    code: """new Text("안녕하세요 세계!")""",
                    capi: Capi
                ),

                new DemoCard
                (
                    "VTML Rich Text",
                    description:
                    "VtmlText parses markup — <strong>, <i>, <font>, <a>, <icon>, <itemstack>.",
                    demo: new VtmlText
                    (
                        vtml,
                        new TextStyle
                        {
                            FontSize = 14, Color = colors.OnSurface
                        }
                    ),
                    code: """
                          new VtmlText(vtml, baseStyle, onLinkClick: href => ...)
                          """,
                    capi: Capi
                ),

                new DemoCard
                (
                    "Vanilla Reference",
                    description:
                    "Same VTML in a vanilla GuiDialog via GuiComposer.AddRichtext — for comparison.",
                    demo: new Button
                    (
                        new Text
                        (
                            "Open Vanilla VTML",
                            new TextStyle
                            {
                                Color = colors.OnPrimary, FontSize = 14
                            }
                        ),
                        onTap: _ => new VanillaVtmlDialog(vtml, Capi).TryOpen()
                    ),
                    code: """
                          new VanillaVtmlDialog(vtml, capi).TryOpen()
                          """,
                    capi: Capi
                )
            ]
        );
    }
}
