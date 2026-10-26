using Gui.Core.Windowing;
using OpenTK.Mathematics;

namespace Gui.Tests.Windowing;

[TestFixture]
public class WindowCollapseStateTests
{
    private const float BarHeight = 28f;
    private static readonly Vector2 FullSize = new(300, 400);

    [Test]
    public void Collapse_FreezesWidthAndLocksHeight()
    {
        var state = new WindowCollapseState();
        var owner = new object();

        var changed = state.SetCollapsed(owner, true, BarHeight, FullSize);

        Assert.That(changed, Is.True);
        Assert.That(state.IsCollapsed, Is.True);
        Assert.That(state.CollapsedHeight, Is.EqualTo(BarHeight));
        Assert.That(state.ExpandedSize, Is.EqualTo(FullSize));
        Assert.That(state.TryGetCollapsedSize(out var size), Is.True);
        Assert.That(size, Is.EqualTo(new Vector2(300, BarHeight)));
        Assert.That(state.Apply(new Vector2(350, 500)), Is.EqualTo(new Vector2(350, BarHeight)));
    }

    [Test]
    public void Expanded_ApplyAndTryGetCollapsedSize_PassThrough()
    {
        var state = new WindowCollapseState();

        Assert.That(state.TryGetCollapsedSize(out _), Is.False);
        Assert.That(state.Apply(FullSize), Is.EqualTo(FullSize));
        Assert.That(state.FilterEdges(ResizeEdge.Top | ResizeEdge.Left),
            Is.EqualTo(ResizeEdge.Top | ResizeEdge.Left));
        Assert.That(state.EffectiveMinHeight(50f), Is.EqualTo(50f));
    }

    [Test]
    public void Collapse_SecondOwnerIsIgnored()
    {
        var state = new WindowCollapseState();
        var first = new object();
        var second = new object();
        state.SetCollapsed(first, true, BarHeight, FullSize);

        var changed = state.SetCollapsed(second, true, 40f, new Vector2(500, 600));

        Assert.That(changed, Is.False);
        Assert.That(state.CollapsedHeight, Is.EqualTo(BarHeight));
        Assert.That(state.ExpandedSize, Is.EqualTo(FullSize));
    }

    [Test]
    public void Expand_OnlyOwnerCanExpand()
    {
        var state = new WindowCollapseState();
        var owner = new object();
        var stranger = new object();
        state.SetCollapsed(owner, true, BarHeight, FullSize);

        var strangerChanged = state.SetCollapsed(stranger, false, 0f, FullSize);
        Assert.That(strangerChanged, Is.False);
        Assert.That(state.IsCollapsed, Is.True);

        var ownerChanged = state.SetCollapsed(owner, false, 0f, FullSize);
        Assert.That(ownerChanged, Is.True);
        Assert.That(state.IsCollapsed, Is.False);

        // Owner is cleared: a different title bar may now collapse the window.
        Assert.That(state.SetCollapsed(stranger, true, BarHeight, FullSize), Is.True);
    }

    [Test]
    public void Expand_WhenNotCollapsed_ReturnsFalse()
    {
        var state = new WindowCollapseState();

        Assert.That(state.SetCollapsed(new object(), false, 0f, FullSize), Is.False);
    }

    [Test]
    public void Collapse_SameOwnerTwice_ReturnsFalse()
    {
        var state = new WindowCollapseState();
        var owner = new object();
        state.SetCollapsed(owner, true, BarHeight, FullSize);

        Assert.That(state.SetCollapsed(owner, true, BarHeight, FullSize), Is.False);
    }

    [Test]
    public void FilterEdges_KeepsLeftRight_DropsTopBottom()
    {
        var state = new WindowCollapseState();
        state.SetCollapsed(new object(), true, BarHeight, FullSize);

        Assert.That(state.FilterEdges(ResizeEdge.Left), Is.EqualTo(ResizeEdge.Left));
        Assert.That(state.FilterEdges(ResizeEdge.Right), Is.EqualTo(ResizeEdge.Right));
        Assert.That(state.FilterEdges(ResizeEdge.Top), Is.EqualTo(ResizeEdge.None));
        Assert.That(state.FilterEdges(ResizeEdge.Bottom), Is.EqualTo(ResizeEdge.None));
        Assert.That(state.FilterEdges(ResizeEdge.Bottom | ResizeEdge.Right),
            Is.EqualTo(ResizeEdge.Right));
        Assert.That(state.FilterEdges(ResizeEdge.Top | ResizeEdge.Left),
            Is.EqualTo(ResizeEdge.Left));
    }

    [Test]
    public void EffectiveMinHeight_MapsConfigMinToBarHeight()
    {
        var state = new WindowCollapseState();
        state.SetCollapsed(new object(), true, BarHeight, FullSize);

        Assert.That(state.EffectiveMinHeight(50f), Is.EqualTo(28f));
        Assert.That(state.EffectiveMinHeight(10f), Is.EqualTo(10f));
    }

    [Test]
    public void UpdateFrozenWidth_WhileCollapsed_ChangesCollapsedSize()
    {
        var state = new WindowCollapseState();
        state.SetCollapsed(new object(), true, BarHeight, FullSize);

        state.UpdateFrozenWidth(420f);

        Assert.That(state.TryGetCollapsedSize(out var size), Is.True);
        Assert.That(size, Is.EqualTo(new Vector2(420, BarHeight)));
    }

    [Test]
    public void UpdateFrozenWidth_WhileExpanded_IsIgnored()
    {
        var state = new WindowCollapseState();

        state.UpdateFrozenWidth(420f);

        Assert.That(state.TryGetCollapsedSize(out var size), Is.False);
        Assert.That(size, Is.EqualTo(Vector2.Zero));
    }
}
