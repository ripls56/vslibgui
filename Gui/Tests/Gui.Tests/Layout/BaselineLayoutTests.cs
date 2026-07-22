using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Tests.Helpers;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Tests.Layout;

[TestFixture]
public class BaselineLayoutTests
{
    private static Element MountAndBuild(Widget widget, BuildOwner owner)
    {
        var element = widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);
        owner.BuildDirtyElements();
        return element;
    }

    [Test]
    public void Row_Baseline_AlignsByFontMetrics()
    {
        var owner = TestHelpers.NewBuildOwner();

        var tallRo = new BaselineStubRenderObject(width: 50, height: 40, baseline: 30);
        var shortRo = new BaselineStubRenderObject(width: 50, height: 20, baseline: 10);

        var flex = new RenderFlex
        {
            Direction = FlexDirection.Horizontal,
            CrossAxisAlignment = CrossAxisAlignment.Baseline,
            MainAxisSize = MainAxisSize.Min
        };
        flex.AddChild(tallRo);
        flex.AddChild(shortRo);

        flex.Layout(LayoutConstraints.Loose(500, 500));

        var maxBaseline = 30f;
        Assert.That(tallRo.Y, Is.EqualTo(maxBaseline - 30).Within(0.01f));
        Assert.That(shortRo.Y, Is.EqualTo(maxBaseline - 10).Within(0.01f));

        var expectedHeight = maxBaseline + Math.Max(40 - 30, 20 - 10);
        Assert.That(flex.Size.Y, Is.EqualTo(expectedHeight).Within(0.01f));
    }

    [Test]
    public void Row_Baseline_NullFallbackUsesHeight()
    {
        var owner = TestHelpers.NewBuildOwner();

        var withBaseline = new BaselineStubRenderObject(width: 50, height: 30, baseline: 20);
        var noBaseline = new NoBaselineStubRenderObject(width: 50, height: 40);

        var flex = new RenderFlex
        {
            Direction = FlexDirection.Horizontal,
            CrossAxisAlignment = CrossAxisAlignment.Baseline,
            MainAxisSize = MainAxisSize.Min
        };
        flex.AddChild(withBaseline);
        flex.AddChild(noBaseline);

        flex.Layout(LayoutConstraints.Loose(500, 500));

        var maxBaseline = Math.Max(20f, 40f);
        Assert.That(withBaseline.Y, Is.EqualTo(maxBaseline - 20).Within(0.01f));
        Assert.That(noBaseline.Y, Is.EqualTo(maxBaseline - 40).Within(0.01f));
    }

    [Test]
    public void LayoutBuilder_RebuildOnConstraintChange()
    {
        var owner = TestHelpers.NewBuildOwner();

        var capturedWidth = 0f;

        var widget = new LayoutBuilder((ctx, constraints) =>
        {
            capturedWidth = constraints.MaxWidth;
            return new SizedBox(width: constraints.MaxWidth, height: 10);
        });

        var element = MountAndBuild(widget, owner);
        var ro = element.RenderObject!;

        ro.Layout(LayoutConstraints.Tight(200, 100));
        owner.BuildDirtyElements();
        ro.Layout(LayoutConstraints.Tight(200, 100));

        Assert.That(capturedWidth, Is.EqualTo(200).Within(0.01f));

        ro.Layout(LayoutConstraints.Tight(400, 100));
        owner.BuildDirtyElements();
        ro.Layout(LayoutConstraints.Tight(400, 100));

        Assert.That(capturedWidth, Is.EqualTo(400).Within(0.01f));
    }

    [Test]
    public void LayoutBuilder_NoRebuildOnSameConstraints()
    {
        var owner = TestHelpers.NewBuildOwner();

        var buildCount = 0;

        var widget = new LayoutBuilder((ctx, constraints) =>
        {
            buildCount++;
            return new SizedBox(width: 10, height: 10);
        });

        var element = MountAndBuild(widget, owner);
        var ro = element.RenderObject!;

        ro.Layout(LayoutConstraints.Tight(200, 100));
        owner.BuildDirtyElements();

        var countAfterFirst = buildCount;

        ro.Layout(LayoutConstraints.Tight(200, 100));
        owner.BuildDirtyElements();

        Assert.That(buildCount, Is.EqualTo(countAfterFirst),
            "Builder should not be called again when constraints are unchanged.");
    }

    private sealed class BaselineStubRenderObject : RenderBox
    {
        private readonly float _width;
        private readonly float _height;
        private readonly float _baseline;

        public BaselineStubRenderObject(float width, float height, float baseline)
        {
            _width = width;
            _height = height;
            _baseline = baseline;
            Size = new Vector2(width, height);
        }

        protected override void PerformLayout()
        {
            Size = new Vector2(_width, _height);
        }

        public override float? GetDistanceToBaseline(
            Gui.Widgets.Layout.TextBaseline baseline
        ) =>
            _baseline;
    }

    private sealed class NoBaselineStubRenderObject : RenderBox
    {
        private readonly float _width;
        private readonly float _height;

        public NoBaselineStubRenderObject(float width, float height)
        {
            _width = width;
            _height = height;
            Size = new Vector2(width, height);
        }

        protected override void PerformLayout()
        {
            Size = new Vector2(_width, _height);
        }

        public override float? GetDistanceToBaseline(
            Gui.Widgets.Layout.TextBaseline baseline
        ) =>
            null;
    }
}
