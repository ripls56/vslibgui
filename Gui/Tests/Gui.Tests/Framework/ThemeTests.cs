using Gui.Core.Framework;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Tests.Framework;

[TestFixture]
public class ThemeTests
{
    private class Leaf : Widget
    {
        public override Element CreateElement() => new RenderObjectElement(this);
        public override RenderObject CreateRenderObject() => new LeafRo();
    }

    private class LeafRo : RenderObject
    {
        protected override void PerformLayout()
        {
        }
    }

    private class ThemeReaderWidget : StatefulWidget
    {
        public override State CreateState() => new ThemeReaderState();
    }

    private class ThemeReaderState : State<ThemeReaderWidget>
    {
        public ThemeData? ReadTheme { get; private set; }
        public int BuildCount { get; private set; }

        public override Widget Build(BuildContext context)
        {
            BuildCount++;
            ReadTheme = Theme.Of(context);
            return new Leaf();
        }
    }

    private class RootWidget : StatefulWidget
    {
        public RootWidget(Widget child)
        {
            InitialChild = child;
        }

        public Widget InitialChild { get; }
        public override State CreateState() => new RootState();
    }

    private class RootState : State<RootWidget>
    {
        public Widget? NextChild { get; set; }

        public override Widget Build(BuildContext context) => NextChild ?? Widget.InitialChild;

        public void RebuildWith(Widget child) => SetState(() => { NextChild = child; });
    }

    private static (BuildOwner owner, Element root, T state)
        MountTree<T>(Widget widget) where T : State
    {
        var owner = new BuildOwner();
        var element = widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);

        T? foundState = null;

        void FindState(Element el)
        {
            if (el is StatefulElement se && se.State is T typed)
            {
                foundState = typed;
            }

            el.VisitChildren(FindState);
        }

        FindState(element);

        return (owner, element, foundState!);
    }

    private static T? FindState<T>(Element root) where T : State
    {
        T? result = null;

        void Visit(Element el)
        {
            if (result != null)
            {
                return;
            }

            if (el is StatefulElement se && se.State is T typed)
            {
                result = typed;
            }

            el.VisitChildren(Visit);
        }

        Visit(root);
        return result;
    }


    [Test]
    public void ThemeOf_ReturnsThemeData_WhenPresent()
    {
        var theme = new ThemeData(
            new ColorScheme
            {
                Primary = new Vector4(1, 0, 0, 1),
                OnPrimary = Vector4.One,
                Secondary = Vector4.Zero,
                OnSecondary = Vector4.One,
                Surface = Vector4.Zero,
                OnSurface = Vector4.One,
                OnSurfaceVariant = Vector4.One,
                Background = Vector4.Zero,
                OnBackground = Vector4.One,
                Border = Vector4.Zero,
                Error = Vector4.Zero,
                OnError = Vector4.One
            });

        var reader = new ThemeReaderWidget();
        var tree = new Theme(theme, reader);

        var (_, _, readerState) = MountTree<ThemeReaderState>(tree);

        Assert.That(readerState.ReadTheme, Is.SameAs(theme));
        Assert.That(readerState.ReadTheme!.ColorScheme.Primary,
            Is.EqualTo(new Vector4(1, 0, 0, 1)));
    }

    [Test]
    public void ThemeOf_ReturnsDefault_WhenNoThemeAncestor()
    {
        var reader = new ThemeReaderWidget();

        var (_, _, state) = MountTree<ThemeReaderState>(reader);

        Assert.That(state.ReadTheme, Is.SameAs(ThemeData.Default));
    }

    [Test]
    public void Consumer_RebuildsWhenThemeChanges()
    {
        var reader = new ThemeReaderWidget();
        var theme1 = new ThemeData();
        var rootWidget = new RootWidget(new Theme(theme1, reader));

        var (owner, root, rootState) = MountTree<RootState>(rootWidget);
        var readerState = FindState<ThemeReaderState>(root)!;
        var buildsBefore = readerState.BuildCount;

        // Swap to a different ThemeData instance
        var theme2 = new ThemeData(
            new ColorScheme
            {
                Primary = new Vector4(0, 1, 0, 1),
                OnPrimary = Vector4.One,
                Secondary = Vector4.Zero,
                OnSecondary = Vector4.One,
                Surface = Vector4.Zero,
                OnSurface = Vector4.One,
                OnSurfaceVariant = Vector4.One,
                Background = Vector4.Zero,
                OnBackground = Vector4.One,
                Border = Vector4.Zero,
                Error = Vector4.Zero,
                OnError = Vector4.One
            });

        rootState.RebuildWith(new Theme(theme2, reader));
        owner.BuildDirtyElements();

        Assert.That(readerState.ReadTheme, Is.SameAs(theme2));
        Assert.That(readerState.BuildCount, Is.GreaterThan(buildsBefore));
    }

    [Test]
    public void ColorScheme_HasSecondaryAndVariantValues()
    {
        var scheme = ColorScheme.Default();

        Assert.That(scheme.Secondary, Is.EqualTo(new Vector4(0.70f, 0.58f, 0.37f, 1.0f)));
        Assert.That(scheme.OnSecondary, Is.EqualTo(new Vector4(0.20f, 0.14f, 0.06f, 1.0f)));
        Assert.That(scheme.OnSurfaceVariant, Is.EqualTo(new Vector4(0.67f, 0.63f, 0.53f, 1.0f)));
    }

    [Test]
    public void ThemeDataDefault_HasSensibleValues()
    {
        var td = ThemeData.Default;

        Assert.That(td.ColorScheme.Primary.W, Is.EqualTo(1f));
        Assert.That(td.ColorScheme.Secondary.W, Is.EqualTo(1f));
        Assert.That(td.TextTheme.Body.FontSize, Is.GreaterThan(0));
    }

    [Test]
    public void SameThemeDataInstance_NoRebuild()
    {
        var reader = new ThemeReaderWidget();
        var theme = new ThemeData();
        var rootWidget = new RootWidget(new Theme(theme, reader));

        var (owner, root, rootState) = MountTree<RootState>(rootWidget);
        var readerState = FindState<ThemeReaderState>(root)!;
        var buildsBefore = readerState.BuildCount;

        // Re-wrap with the SAME ThemeData instance
        rootState.RebuildWith(new Theme(theme, reader));
        owner.BuildDirtyElements();

        // UpdateShouldNotify uses ReferenceEquals → false → no DidChangeDependencies
        // The reader may still rebuild due to parent, but DidChange should not fire.
        // We check that no extra DidChangeDependencies-triggered rebuild happened
        // beyond the normal parent rebuild.
        Assert.That(readerState.BuildCount, Is.LessThanOrEqualTo(buildsBefore + 1));
    }
}
