using Gui.Widgets.Animations;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;

namespace Gui.Tests.Animations;

#region Test doubles

internal class MockTickerProvider : ITickerProvider
{
    public List<Ticker> Tickers = [];

    public Ticker CreateTicker(Action<TimeSpan> onTick)
    {
        var t = new Ticker(onTick);
        Tickers.Add(t);
        return t;
    }

    public void Advance(TimeSpan elapsed)
    {
        foreach (var t in Tickers)
        {
            t.Tick(elapsed);
        }
    }
}

internal class TestAnimatedWidget : ImplicitlyAnimatedWidget
{
    public TestAnimatedWidget(
        float targetValue,
        TimeSpan duration,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null)
        : base(duration, curve, onEnd, key)
    {
        TargetValue = targetValue;
    }

    public float TargetValue { get; }

    public override State CreateState() => new TestAnimatedState();
}

internal class TestAnimatedState : ImplicitlyAnimatedWidgetState<TestAnimatedWidget>
{
    private FloatTween? _valueTween;
    public float CurrentValue => _valueTween?.Evaluate(Animation) ?? 0f;
    public int BuildCount { get; private set; }

    protected override void ForEachTween(TweenVisitor visitor)
    {
        _valueTween = (FloatTween)visitor.Visit(
            _valueTween,
            Widget.TargetValue,
            v => new FloatTween(v, v));
    }

    public override Widget Build(BuildContext context)
    {
        BuildCount++;
        return new SizedBox(CurrentValue, CurrentValue);
    }
}

#endregion

[TestFixture]
public class ImplicitlyAnimatedWidgetTests
{
    [SetUp]
    public void SetUp()
    {
        _vsync = new MockTickerProvider();
        _owner = new BuildOwner();
        _owner.SetTickerProvider(_vsync);
    }

    private MockTickerProvider _vsync = null!;
    private BuildOwner _owner = null!;

    private (StatefulElement element, TestAnimatedState state) Mount(
        TestAnimatedWidget widget)
    {
        var element = (StatefulElement)widget.CreateElement();
        element.AssignOwner(_owner);
        element.Mount(null);
        _owner.BuildDirtyElements();
        var state = (TestAnimatedState)element.State;
        return (element, state);
    }

    [Test]
    public void InitState_CreatesControllerAndCallsForEachTween()
    {
        var widget = new TestAnimatedWidget(100f, TimeSpan.FromMilliseconds(300));
        var (_, state) = Mount(widget);

        Assert.That(state.CurrentValue, Is.EqualTo(100f));
    }

    [Test]
    public void UpdateWidget_AnimatesOnPropertyChange()
    {
        var widget = new TestAnimatedWidget(100f, TimeSpan.FromMilliseconds(300));
        var (element, state) = Mount(widget);

        var newWidget = new TestAnimatedWidget(200f, TimeSpan.FromMilliseconds(300));
        element.Update(newWidget);
        _owner.BuildDirtyElements();

        // Advance halfway
        _vsync.Advance(TimeSpan.FromMilliseconds(150));
        _owner.BuildDirtyElements();
        Assert.That(state.CurrentValue, Is.InRange(145f, 155f));

        // Complete
        _vsync.Advance(TimeSpan.FromMilliseconds(200));
        _owner.BuildDirtyElements();
        Assert.That(state.CurrentValue, Is.EqualTo(200f));
    }

    [Test]
    public void UpdateWidget_NoAnimation_WhenTargetUnchanged()
    {
        var widget = new TestAnimatedWidget(100f, TimeSpan.FromMilliseconds(300));
        var (element, state) = Mount(widget);

        var buildsAfterMount = state.BuildCount;

        var newWidget = new TestAnimatedWidget(100f, TimeSpan.FromMilliseconds(300));
        element.Update(newWidget);
        _owner.BuildDirtyElements();

        var buildsAfterUpdate = state.BuildCount;

        // Advance time — should not trigger extra builds from animation
        _vsync.Advance(TimeSpan.FromMilliseconds(150));
        _owner.BuildDirtyElements();

        // Only the rebuild from Update itself, no animation-driven rebuilds
        Assert.That(state.BuildCount, Is.EqualTo(buildsAfterUpdate));
    }

    [Test]
    public void OnEnd_CalledWhenAnimationCompletes()
    {
        var onEndCalled = false;
        var widget = new TestAnimatedWidget(
            100f, TimeSpan.FromMilliseconds(300), onEnd: () => onEndCalled = true);
        var (element, _) = Mount(widget);

        var newWidget = new TestAnimatedWidget(
            200f, TimeSpan.FromMilliseconds(300), onEnd: () => onEndCalled = true);
        element.Update(newWidget);
        _owner.BuildDirtyElements();

        Assert.That(onEndCalled, Is.False);

        _vsync.Advance(TimeSpan.FromMilliseconds(400));
        _owner.BuildDirtyElements();

        Assert.That(onEndCalled, Is.True);
    }

    [Test]
    public void CurveIsApplied()
    {
        var widget = new TestAnimatedWidget(
            0f, TimeSpan.FromMilliseconds(100), Curves.EaseIn);
        var (element, state) = Mount(widget);

        var newWidget = new TestAnimatedWidget(
            100f, TimeSpan.FromMilliseconds(100), Curves.EaseIn);
        element.Update(newWidget);
        _owner.BuildDirtyElements();

        // Advance to 50% linear time
        _vsync.Advance(TimeSpan.FromMilliseconds(50));
        _owner.BuildDirtyElements();

        // EaseIn = t^2, so at t=0.5 → 0.25 → value ~25
        Assert.That(state.CurrentValue, Is.InRange(20f, 30f));
    }
}
