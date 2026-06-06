using System.Reflection;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;

namespace Gui.Tests.Input;

public class TestTickerProvider : ITickerProvider
{
    public Ticker CreateTicker(Action<TimeSpan> onTick) => new(onTick);
}

[TestFixture]
public class NumericFieldTests
{
    [SetUp]
    public void Setup()
    {
        _lastValue = 0;
        _numericField = new NumericField(
            10,
            5,
            v => _lastValue = v
        );

        _buildOwner = new BuildOwner();
        _buildOwner.SetTickerProvider(new TestTickerProvider());

        _element = _numericField.CreateElement();
        _element.AssignOwner(_buildOwner);
        _element.Mount(null);

        _state = (NumericFieldState)((StatefulElement)_element).State;
    }

    [TearDown]
    public void TearDown()
    {
        _element.Unmount();
        _numericField.Dispose();
    }

    private NumericField _numericField;
    private NumericFieldState _state;
    private Element _element;
    private BuildOwner _buildOwner;
    private float _lastValue;

    [Test]
    public void NumericField_ShouldInitializeWithCorrectValue() =>
        Assert.That(_numericField.Value, Is.EqualTo(10));

    [Test]
    public void NumericField_ShouldUpdateValue_WhenIncremented()
    {
        var method =
            typeof(NumericFieldState).GetMethod("Adjust",
                BindingFlags.NonPublic | BindingFlags.Instance);
        method!.Invoke(_state, [5.0f]);

        Assert.That(_lastValue, Is.EqualTo(15));
    }

    [Test]
    public void NumericField_ShouldUpdateValue_WhenDecremented()
    {
        var method =
            typeof(NumericFieldState).GetMethod("Adjust",
                BindingFlags.NonPublic | BindingFlags.Instance);
        method!.Invoke(_state, [-5.0f]);

        Assert.That(_lastValue, Is.EqualTo(5));
    }

    [Test]
    public void NumericField_ShouldParseTextUpdates()
    {
        var controllerField =
            typeof(NumericFieldState).GetField("_controller",
                BindingFlags.NonPublic | BindingFlags.Instance);
        var controller = (TextEditingController)controllerField!.GetValue(_state)!;

        controller.Text = "20";

        Assert.That(_lastValue, Is.EqualTo(20));
    }

    [Test]
    public void NumericField_ShouldIgnoreInvalidText()
    {
        var controllerField =
            typeof(NumericFieldState).GetField("_controller",
                BindingFlags.NonPublic | BindingFlags.Instance);
        var controller = (TextEditingController)controllerField!.GetValue(_state)!;

        controller.Text = "abc";

        Assert.That(_lastValue, Is.EqualTo(0));
    }
}
