using Gui.Core.Framework;
using Gui.Tests.Helpers;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;

namespace Gui.Tests.Framework;

[TestFixture]
public class BuildOwnerTests
{
    private class MockElement : Element
    {
        public MockElement(Widget widget) : base(widget)
        {
            var prop = typeof(Element).GetProperty("Depth");
            prop?.SetValue(this, 1);
        }

        public bool Rebuilt { get; private set; }
        public override RenderObject? RenderObject => null;

        public override void VisitChildren(Action<Element> visitor)
        {
        }

        public override void Rebuild()
        {
            Rebuilt = true;
            base.Rebuild();
        }
    }

    private class MockTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(Action<TimeSpan> onTick) => new(onTick);
    }

    [Test]
    public void BuildOwner_ShouldCollectDirtyElements()
    {
        var owner = new BuildOwner();
        owner.SetTickerProvider(new MockTickerProvider());
        var widget = new TestWidget();
        var element = new MockElement(widget);
        element.AssignOwner(owner);

        owner.ScheduleBuildFor(element);
        owner.BuildDirtyElements();

        Assert.That(element.Rebuilt, Is.True);
    }
}
