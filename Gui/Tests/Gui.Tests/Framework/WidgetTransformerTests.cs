using Gui.Core.Framework;
using Gui.Widgets.Framework;

namespace Gui.Tests.Framework;

[TestFixture]
public class WidgetTransformerTests
{
    [TearDown]
    public void TearDown() => WidgetTransformerRegistry.Clear();

    private class KeyedWidget : Widget
    {
        public KeyedWidget(Key? key = null) : base(key)
        {
        }

        public override Element CreateElement() => new RenderObjectElement(this);
        public override RenderObject CreateRenderObject() => new MockRo();
    }

    private class ReplacedWidget : Widget
    {
        public ReplacedWidget(Key? key = null) : base(key)
        {
        }

        public override Element CreateElement() => new RenderObjectElement(this);
        public override RenderObject CreateRenderObject() => new MockRo();
    }

    private class MockRo : RenderObject
    {
        protected override void PerformLayout()
        {
        }
    }

    private class ReplaceTransformer : IWidgetTransformer
    {
        public Widget Transform(Widget widget) =>
            new ReplacedWidget(widget.Key);
    }

    private class TrackingTransformer : IWidgetTransformer
    {
        private readonly int _id;
        private readonly List<int> _log;

        public TrackingTransformer(List<int> log, int id)
        {
            _log = log;
            _id = id;
        }

        public Widget Transform(Widget widget)
        {
            _log.Add(_id);
            return widget;
        }
    }

    [Test]
    public void Apply_NoTransformer_ReturnsSameInstance()
    {
        var key = new ValueKey<string>("test.key");
        var widget = new KeyedWidget(key);
        var result = WidgetTransformerRegistry.Apply(widget);
        Assert.That(result, Is.SameAs(widget));
    }

    [Test]
    public void Apply_KeylessWidget_ReturnsSameInstance()
    {
        var widget = new KeyedWidget();
        var key = new ValueKey<string>("test.key");
        WidgetTransformerRegistry.Register(key, new ReplaceTransformer());
        var result = WidgetTransformerRegistry.Apply(widget);
        Assert.That(result, Is.SameAs(widget));
    }

    [Test]
    public void Apply_MatchingKey_TransformerRuns()
    {
        var key = new ValueKey<string>("test.key");
        var widget = new KeyedWidget(key);
        WidgetTransformerRegistry.Register(key, new ReplaceTransformer());
        var result = WidgetTransformerRegistry.Apply(widget);
        Assert.That(result, Is.InstanceOf<ReplacedWidget>());
    }

    [Test]
    public void Apply_DifferentKey_TransformerDoesNotRun()
    {
        var registeredKey = new ValueKey<string>("test.key");
        var widgetKey = new ValueKey<string>("other.key");
        var widget = new KeyedWidget(widgetKey);
        WidgetTransformerRegistry.Register(registeredKey, new ReplaceTransformer());
        var result = WidgetTransformerRegistry.Apply(widget);
        Assert.That(result, Is.SameAs(widget));
    }

    [Test]
    public void Register_PriorityOrder_LowerRunsFirst()
    {
        var key = new ValueKey<string>("test.key");
        var log = new List<int>();
        WidgetTransformerRegistry.Register(key, new TrackingTransformer(log, 20), 20);
        WidgetTransformerRegistry.Register(key, new TrackingTransformer(log, 5), 5);
        WidgetTransformerRegistry.Register(key, new TrackingTransformer(log, 10), 10);
        WidgetTransformerRegistry.Apply(new KeyedWidget(key));
        Assert.That(log, Is.EqualTo(new[] { 5, 10, 20 }));
    }

    [Test]
    public void Apply_Chain_EachTransformerReceivesPreviousOutput()
    {
        var key = new ValueKey<string>("test.key");
        Widget? secondInput = null;
        var first = new ReplaceTransformer();
        var second = new LambdaTransformer(w =>
        {
            secondInput = w;
            return w;
        });
        WidgetTransformerRegistry.Register(key, first, 1);
        WidgetTransformerRegistry.Register(key, second, 2);
        WidgetTransformerRegistry.Apply(new KeyedWidget(key));
        Assert.That(secondInput, Is.InstanceOf<ReplacedWidget>());
    }

    [Test]
    public void UpdateChild_TransformerFires_OnMount()
    {
        var key = new ValueKey<string>("test.panel");
        WidgetTransformerRegistry.Register(key, new ReplaceTransformer());

        var parent = new LambdaStatelessWidget(_ => new KeyedWidget(key));
        var owner = new BuildOwner();
        var parentElement = new ComponentElement(parent);
        parentElement.AssignOwner(owner);
        parentElement.Mount(null);

        Element? child = null;
        parentElement.VisitChildren(e => child = e);

        Assert.That(child?.Widget, Is.InstanceOf<ReplacedWidget>());
    }
}

file class LambdaTransformer(Func<Widget, Widget> fn) : IWidgetTransformer
{
    public Widget Transform(Widget widget) => fn(widget);
}

file class LambdaStatelessWidget(Func<BuildContext, Widget> build) : StatelessWidget
{
    public override Widget Build(BuildContext context) => build(context);
}
