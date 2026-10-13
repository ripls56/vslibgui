using System.Collections.Generic;
using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Tests.Gestures;

/// <summary>
///     Hit-testing through a scaling/panning viewport, i.e. the pattern a zoomable
///     canvas has to use: the render object takes the whole viewport as its layout
///     <see cref="RenderObject.Size" /> and maps pointers into content space in
///     <see cref="RenderObject.GlobalToChild" />.
///     <para>
///         A paint-only <c>Transform</c> cannot do this job: its layout box stays the
///         child's unscaled size, and the coarse bounds check in <c>Element.HitTest</c>
///         then rejects every pointer over content painted outside that box — which is
///         all content once zoom &gt; 1 or the content is panned.
///     </para>
/// </summary>
[TestFixture]
public class ScaledViewportHitTestTests
{
    private const float ViewportW = 400f;
    private const float ViewportH = 300f;
    private const float ContentW = 400f;
    private const float ContentH = 300f;
    private const float NodeSize = 44f;

    // Offsets are chosen so the node in question is actually inside the viewport —
    // content scrolled out of view is legitimately not hit-testable.
    [TestCase(1.0f, 0f, 0f, 200f, 150f)]
    [TestCase(1.0f, 0f, 0f, 60f, 40f)]
    [TestCase(1.5f, -100f, -75f, 200f, 150f)]
    [TestCase(1.5f, -200f, -150f, 340f, 260f)]
    [TestCase(2.5f, -300f, -225f, 200f, 150f)]
    [TestCase(2.5f, -40f, -20f, 60f, 40f)]
    [TestCase(2.5f, -700f, -500f, 340f, 260f)]
    public void ScaledViewport_HitTestFollowsPaintedPosition(
        float scale,
        float offsetX,
        float offsetY,
        float nodeCx,
        float nodeCy
    )
    {
        var node = new MockNode(NodeSize);
        var viewport = new ScaledViewport(
            new Vector2(ContentW, ContentH),
            scale,
            new Vector2(offsetX, offsetY),
            new Stack([
                new Positioned(
                    left: nodeCx - NodeSize / 2f,
                    top: nodeCy - NodeSize / 2f,
                    width: NodeSize,
                    height: NodeSize,
                    child: node
                )
            ])
        );

        var owner = new BuildOwner();
        var rootElement = viewport.CreateElement();
        rootElement.AssignOwner(owner);
        rootElement.Mount(null);
        owner.BuildDirtyElements();

        var rootRo = rootElement.RenderObject!;
        rootRo.Layout(LayoutConstraints.Tight(ViewportW, ViewportH));

        Assert.That(rootRo.Size, Is.EqualTo(new Vector2(ViewportW, ViewportH)),
            "viewport must claim the full box so its hit bounds cover everything it paints");

        var nodeRo = FindRenderObject(rootElement, node);
        Assert.That(nodeRo, Is.Not.Null);

        // Where the node actually lands on screen: content space → scale → offset.
        var content = ContentCentre(rootRo, nodeRo!);
        var painted = content * scale + new Vector2(offsetX, offsetY);

        var result = new HitTestResult();
        var hit = rootElement.HitTest(result, painted);
        var where =
            $"scale={scale} offset=({offsetX},{offsetY}) node=({nodeCx},{nodeCy}) painted=({painted.X:F1},{painted.Y:F1})";

        Assert.Multiple(() =>
        {
            Assert.That(hit, Is.True, $"{where} — nothing hit");
            Assert.That(result.Path.Any(e => e.Element.Widget == node), Is.True,
                $"{where} — node not in hit path");
        });
    }

    [Test]
    public void ScaledViewport_GlobalToLocalRoundTripsThroughScale()
    {
        var node = new MockNode(NodeSize);
        var viewport = new ScaledViewport(
            new Vector2(ContentW, ContentH),
            2f,
            new Vector2(-50f, -30f),
            node
        );

        var owner = new BuildOwner();
        var rootElement = viewport.CreateElement();
        rootElement.AssignOwner(owner);
        rootElement.Mount(null);
        owner.BuildDirtyElements();

        var ro = rootElement.RenderObject!;
        ro.Layout(LayoutConstraints.Tight(ViewportW, ViewportH));

        // Content point (120, 90) paints at (120*2-50, 90*2-30) = (190, 150).
        var local = ro.GlobalToLocal(new Vector2(190f, 150f));

        Assert.That(local.X, Is.EqualTo(120f).Within(0.01f));
        Assert.That(local.Y, Is.EqualTo(90f).Within(0.01f));
    }

    /// <summary>Centre of <paramref name="target" /> in content space (before scale/offset).</summary>
    private static Vector2 ContentCentre(
        RenderObject viewport,
        RenderObject target
    )
    {
        var offset = Vector2.Zero;
        for (var ro = target; ro != null && ro != viewport; ro = ro.Parent)
        {
            offset += new Vector2(ro.X, ro.Y);
        }

        return offset + target.Size / 2f;
    }

    private static RenderObject? FindRenderObject(
        Element root,
        Widget widget
    )
    {
        RenderObject? found = null;

        void Visit(
            Element el
        )
        {
            if (found != null)
            {
                return;
            }

            if (ReferenceEquals(el.Widget, widget))
            {
                found = el.RenderObject;
                return;
            }

            el.VisitChildren(Visit);
        }

        Visit(root);
        return found;
    }

    private class MockNode : RenderObjectWidget, IPointerClickHandler
    {
        private readonly float _size;

        public MockNode(
            float size
        ) =>
            _size = size;

        public void OnPointerClick(
            PointerEvent args
        )
        {
        }

        public override RenderObject CreateRenderObject() =>
            new RenderBox { PreferredSize = new Vector2(_size, _size) };
    }

    /// <summary>Fills its constraints, lays the child out at a fixed content size, and scales it.</summary>
    private class ScaledViewport : SingleChildWidget
    {
        public ScaledViewport(
            Vector2 contentSize,
            float scale,
            Vector2 offset,
            Widget child
        ) : base(child)
        {
            ContentSize = contentSize;
            Scale = scale;
            Offset = offset;
        }

        public Vector2 ContentSize { get; }
        public float Scale { get; }
        public Vector2 Offset { get; }

        public override RenderObject CreateRenderObject() =>
            new RenderScaledViewport
            {
                ContentSize = ContentSize, Scale = Scale, Offset = Offset
            };

        public override void UpdateRenderObject(
            RenderObject renderObject
        )
        {
            base.UpdateRenderObject(renderObject);
            var ro = (RenderScaledViewport)renderObject;
            ro.ContentSize = ContentSize;
            ro.Scale = Scale;
            ro.Offset = Offset;
        }
    }

    private class RenderScaledViewport : RenderObject
    {
        public Vector2 ContentSize { get; set; } = Vector2.One;
        public float Scale { get; set; } = 1f;
        public Vector2 Offset { get; set; } = Vector2.Zero;

        public override bool IsHitTestTarget => false;

        protected override void PerformLayout()
        {
            Size = Constraints.Biggest;
            foreach (var child in Children)
            {
                child.X = 0;
                child.Y = 0;
                child.Layout(LayoutConstraints.Tight(ContentSize));
            }
        }

        public override void Paint(
            PaintingContext context
        )
        {
            if (context.Canvas == null)
            {
                base.Paint(context);
                return;
            }

            using (context.Canvas.SaveScope())
            {
                var m = SKMatrix.CreateScale(Scale, Scale)
                    .PostConcat(SKMatrix.CreateTranslation(Offset.X, Offset.Y));
                context.Canvas.Concat(in m);
                base.Paint(context);
            }
        }

        public override Vector2 GlobalToChild(
            RenderObject child,
            Vector2 position
        ) =>
            ToContent(position) - new Vector2(child.X, child.Y);

        public override Vector2 LocalToGlobal(
            Vector2 localPoint
        )
        {
            var point = localPoint * Scale + Offset + new Vector2(X, Y);
            return Parent?.LocalToGlobal(point) ?? point;
        }

        public override Vector2 GlobalToLocal(
            Vector2 globalPoint
        )
        {
            var parentLocal = Parent?.GlobalToLocal(globalPoint) ?? globalPoint;
            return ToContent(parentLocal - new Vector2(X, Y));
        }

        private Vector2 ToContent(
            Vector2 position
        ) =>
            (position - Offset) / Scale;
    }
}
