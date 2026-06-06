using Gui.Core.Basic;
using Gui.Core.Framework;
using Gui.Tests.Helpers;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Vintagestory.API.Client;

namespace Gui.Tests.Widgets;

[TestFixture]
public class HotkeyChipTests
{
    private static Element MountTree(Widget widget)
    {
        var owner = TestHelpers.NewBuildOwner();
        var element = widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);
        return element;
    }

    private static bool HasRenderObject<T>(Element root) where T : RenderObject
    {
        var found = false;

        void Visit(Element el)
        {
            if (el is RenderObjectElement roe && roe.RenderObject is T)
            {
                found = true;
            }

            el.VisitChildren(Visit);
        }

        Visit(root);
        return found;
    }

    private static List<string> CollectTexts(Element root)
    {
        var texts = new List<string>();

        void Visit(Element el)
        {
            if (el is RenderObjectElement roe && roe.RenderObject is RenderText rt)
            {
                texts.Add(rt.Text);
            }

            el.VisitChildren(Visit);
        }

        Visit(root);
        return texts;
    }

    [Test]
    public void NullMapping_RendersFallbackText()
    {
        var chip = new HotkeyChip(null, "[sprint]", 14f);
        var root = MountTree(chip);

        Assert.That(CollectTexts(root), Does.Contain("[sprint]"));
    }

    [Test]
    public void MouseButton_RendersIconInsteadOfText()
    {
        var mapping = new KeyCombination { KeyCode = KeyCombination.MouseStart + 2 };
        var chip = new HotkeyChip(mapping, "[place]", 14f);
        var root = MountTree(chip);

        Assert.That(HasRenderObject<RenderIcon>(root), Is.True,
            "Mouse button should render as an icon");
    }

    [Test]
    public void ModifierPlusKey_RendersSeparateBadgesJoinedByPlus()
    {
        var mapping = new KeyCombination { KeyCode = (int)GlKeys.E, Shift = true };
        var chip = new HotkeyChip(mapping, "[interact]", 14f);
        var root = MountTree(chip);

        var texts = CollectTexts(root);
        Assert.That(texts, Does.Contain("Shift"));
        Assert.That(texts, Does.Contain("+"));
        Assert.That(texts, Does.Contain("E"));
    }

    [Test]
    public void SingleKey_RendersWithoutSeparator()
    {
        var mapping = new KeyCombination { KeyCode = (int)GlKeys.E };
        var chip = new HotkeyChip(mapping, "[interact]", 14f);
        var root = MountTree(chip);

        Assert.That(CollectTexts(root), Does.Not.Contain("+"));
    }
}
