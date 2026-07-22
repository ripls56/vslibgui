using Gui.Rendering;
using Gui.Settings;
using Gui.Tests.Helpers;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Overlay;
using OpenTK.Mathematics;

namespace Gui.Tests.Widgets;

[TestFixture]
public class CustomizeOverrideFieldTests
{
    [Test]
    public void OverrideFields_UseCompactCard()
    {
        Widget[] fields =
        [
            new ColorOverrideField("Color", null, Vector4.One, _ => { }),
            new FloatOverrideField("Corner radius", null, 0, 40, 1, 8, _ => { }),
            new EnumOverrideField<ButtonVariant>
            (
                "Variant", null, ButtonVariant.Primary, _ => { }
            ),
            new EdgeInsetsOverrideField("Padding", null, EdgeInsets.All(8), _ => { })
        ];

        foreach (var field in fields)
        {
            var root = TestHelpers.Mount(field, new ThemeData());
            Assert.That
            (
                TestHelpers.FindWidget<OverrideFieldCard>(root),
                Is.Not.Null,
                field.GetType().Name
            );
        }
    }

    [Test]
    public void InheritedFloatOverride_DisablesReset()
    {
        var root = TestHelpers.Mount
        (
            new FloatOverrideField("Corner radius", null, 0, 40, 1, 8, _ => { }),
            new ThemeData()
        );
        var button = TestHelpers.FindWidget<IconButton>(root)!;
        var tooltip = TestHelpers.FindWidget<Tooltip>(root)!;

        Assert.Multiple(() =>
        {
            Assert.That(button.Enabled, Is.False);
            var icon = button.Icon;
            Assert.That(icon.Domain, Is.EqualTo("gui"));
            Assert.That(icon.Path, Is.EqualTo("textures/icons/undo-2.svg"));
            Assert.That
            (
                ((Gui.Widgets.Basic.Text)tooltip.Content).Content,
                Is.EqualTo("Reset to inherited value")
            );
        });
    }

    [Test]
    public void ExplicitFloatOverride_EnablesReset()
    {
        var root = TestHelpers.Mount
        (
            new FloatOverrideField("Corner radius", 12, 0, 40, 1, 8, _ => { }),
            new ThemeData()
        );

        Assert.That(TestHelpers.FindWidget<IconButton>(root)!.Enabled, Is.True);
    }

    [Test]
    public void ColorField_UpdatesValueAsTextChanges()
    {
        Vector4? updated = null;
        var root = TestHelpers.Mount
        (
            new ColorField(Vector4.Zero, value => updated = value, "Color"),
            new ThemeData()
        );

        TestHelpers.FindWidget<TextField>(root)!.Controller!.Text = "#FF0000";

        Assert.That(updated, Is.EqualTo(new Vector4(1, 0, 0, 1)));
    }
}
