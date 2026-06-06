using Gui.Widgets.Animations;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;
using Gui.Widgets.Input;

namespace Gui.Tests.Input;

[TestFixture]
public class SliderTests
{
    [SetUp]
    public void SetUp()
    {
        _buildOwner = new BuildOwner();
        _buildOwner.SetTickerProvider(new MockTickerProvider());
    }

    private BuildOwner _buildOwner = null!;

    private (Element root, EventDispatcher dispatcher) Mount(Widget widget, float width = 200,
        float height = 30)
    {
        var element = widget.CreateElement();
        element.AssignOwner(_buildOwner);
        element.Mount(null);
        _buildOwner.BuildDirtyElements();
        element.RenderObject!.Layout(LayoutConstraints.Tight(width, height));
        return (element, new EventDispatcher());
    }


    [Test]
    public void Slider_DefaultProperties_ShouldHaveCorrectDefaults()
    {
        var slider = new Slider(0.5f, _ => { });

        Assert.That(slider.Value, Is.EqualTo(0.5f));
        Assert.That(slider.Min, Is.EqualTo(0f));
        Assert.That(slider.Max, Is.EqualTo(1f));
        Assert.That(slider.Divisions, Is.Null);
    }

    [Test]
    public void Slider_WithCustomRange_ShouldStoreProperties()
    {
        var slider = new Slider(50, _ => { }, min: 0, max: 100);

        Assert.That(slider.Min, Is.EqualTo(0f));
        Assert.That(slider.Max, Is.EqualTo(100f));
        Assert.That(slider.Value, Is.EqualTo(50f));
    }

    [Test]
    public void Slider_WithDivisions_ShouldStoreProperty()
    {
        var slider = new Slider(0, _ => { }, min: 0, max: 10, divisions: 10);

        Assert.That(slider.Divisions, Is.EqualTo(10));
    }


    [Test]
    public void Slider_ShouldMountAndLayout()
    {
        var slider = new Slider(0.5f, _ => { });
        var (root, _) = Mount(slider);

        Assert.That(root.RenderObject, Is.Not.Null);
        Assert.That(root.RenderObject!.Size.X, Is.EqualTo(200f));
    }


    [Test]
    public void Slider_Click_ShouldUpdateValue()
    {
        float? newValue = null;
        var slider = new Slider(0f, v => newValue = v);
        var (root, dispatcher) = Mount(slider);

        // Click in the middle of the track
        var e = new PointerEvent(100, 15);
        dispatcher.DispatchPointerDown(root, e);
        dispatcher.DispatchPointerUp(root, e);

        Assert.That(newValue, Is.Not.Null);
        Assert.That(newValue!.Value, Is.EqualTo(0.5f).Within(0.15f));
    }

    [Test]
    public void Slider_Drag_ShouldCallOnChanged()
    {
        float lastValue = 0;
        var callCount = 0;
        var slider = new Slider(0f, v =>
        {
            lastValue = v;
            callCount++;
        });
        var (root, dispatcher) = Mount(slider);

        // Press at left, move to right
        dispatcher.DispatchPointerDown(root, new PointerEvent(10, 15));
        dispatcher.DispatchPointerMove(root, new PointerEvent(100, 15));
        dispatcher.DispatchPointerMove(root, new PointerEvent(190, 15));
        dispatcher.DispatchPointerUp(root, new PointerEvent(190, 15));

        Assert.That(callCount, Is.GreaterThanOrEqualTo(2));
        Assert.That(lastValue, Is.GreaterThan(0.5f));
    }

    [Test]
    public void Slider_OnChangeEnd_ShouldFireOnRelease()
    {
        float? endValue = null;
        var slider = new Slider(
            0f,
            _ => { },
            v => endValue = v
        );
        var (root, dispatcher) = Mount(slider);

        dispatcher.DispatchPointerDown(root, new PointerEvent(100, 15));
        dispatcher.DispatchPointerUp(root, new PointerEvent(100, 15));

        Assert.That(endValue, Is.Not.Null);
    }

    [Test]
    public void Slider_WithDivisions_ShouldSnapToSteps()
    {
        float? newValue = null;
        // Range 0-10, 10 divisions = steps of 1.0
        var slider = new Slider(
            0f,
            v => newValue = v,
            min: 0, max: 10, divisions: 10
        );
        var (root, dispatcher) = Mount(slider);

        // Click somewhere that should snap
        dispatcher.DispatchPointerDown(root, new PointerEvent(100, 15));
        dispatcher.DispatchPointerUp(root, new PointerEvent(100, 15));

        Assert.That(newValue, Is.Not.Null);
        // Value should be a whole number (snapped to step)
        var remainder = newValue!.Value % 1.0f;
        Assert.That(remainder, Is.EqualTo(0f).Within(0.01f),
            $"Value {newValue} should be snapped to integer step");
    }

    [Test]
    public void Slider_MouseWheel_ShouldChangeValue()
    {
        float? newValue = null;
        var slider = new Slider(
            0.5f,
            v => newValue = v,
            min: 0, max: 1
        );
        var (root, dispatcher) = Mount(slider);

        // Scroll up
        var wheelEvent = new PointerEvent(100, 15, delta: 1);
        dispatcher.DispatchMouseWheel(root, wheelEvent);

        Assert.That(newValue, Is.Not.Null);
        Assert.That(newValue!.Value, Is.GreaterThan(0.5f));
    }

    [Test]
    public void Slider_MouseWheelWithDivisions_ShouldStepByOne()
    {
        float? newValue = null;
        var slider = new Slider(
            5f,
            v => newValue = v,
            min: 0, max: 10, divisions: 10
        );
        var (root, dispatcher) = Mount(slider);

        var wheelEvent = new PointerEvent(100, 15, delta: 1);
        dispatcher.DispatchMouseWheel(root, wheelEvent);

        Assert.That(newValue, Is.Not.Null);
        Assert.That(newValue!.Value, Is.EqualTo(6f).Within(0.01f));
    }

    [Test]
    public void Slider_Value_ShouldClampToRange()
    {
        float? newValue = null;
        var slider = new Slider(
            0.9f,
            v => newValue = v,
            min: 0, max: 1
        );
        var (root, dispatcher) = Mount(slider);

        // Click at the far right edge of the track
        dispatcher.DispatchPointerDown(root, new PointerEvent(199, 15));
        dispatcher.DispatchPointerUp(root, new PointerEvent(199, 15));

        Assert.That(newValue, Is.Not.Null);
        Assert.That(newValue!.Value, Is.LessThanOrEqualTo(1f));
    }

    [Test]
    public void Slider_WithLabel_ShouldMountSuccessfully()
    {
        var slider = new Slider(
            0.5f,
            _ => { },
            label: "Volume"
        );
        var (root, _) = Mount(slider, 300);

        Assert.That(root.RenderObject, Is.Not.Null);
    }

    private class MockTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(Action<TimeSpan> onTick) => new(onTick);
    }
}
