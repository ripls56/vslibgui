using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Vintagestory.API.Client;

namespace Gui.Tests.Input;

[TestFixture]
public class TextFieldTests
{
    [SetUp]
    public void Setup()
    {
        _controller = new TextEditingController();
        _focusNode = new FocusNode();
        _textField = new TextField(_controller, _focusNode);

        _buildOwner = new BuildOwner();
        _buildOwner.SetTickerProvider(new TestTickerProvider());
        _element = _textField.CreateElement();
        _element.AssignOwner(_buildOwner);
        _element.Mount(null);

        _state = (TextFieldState)((StatefulElement)_element).State;
    }

    [TearDown]
    public void TearDown()
    {
        _element.Unmount();
        _controller.Dispose();
        _focusNode.Dispose();
        _textField.Dispose();
    }

    private TextField _textField;
    private TextEditingController _controller;
    private FocusNode _focusNode;
    private TextFieldState _state;
    private Element _element;
    private BuildOwner _buildOwner;

    [Test]
    public void TextField_ShouldInsertCharacter_WhenFocusedAndKeyTyped()
    {
        _focusNode.RequestFocus();

        var keyEvent = new KeyboardEvent(KeyEventType.KeyChar, 0, 'a');
        _state.OnKeyChar(keyEvent);

        Assert.That(_controller.Text, Is.EqualTo("a"));
    }

    [Test]
    public void TextField_ShouldNotInsertCharacter_WhenNotFocused()
    {
        var keyEvent = new KeyboardEvent(KeyEventType.KeyChar, 0, 'a');
        _state.OnKeyChar(keyEvent);

        Assert.That(_controller.Text, Is.Empty);
    }

    [Test]
    public void TextField_ShouldHandleBackspace()
    {
        _focusNode.RequestFocus();
        _controller.Text = "Hello";
        _controller.Selection = TextSelection.Collapsed(5);

        var key = (int)GlKeys.BackSpace;
        Console.WriteLine($"Testing BackSpace with code: {key}");
        var keyEvent = new KeyboardEvent(KeyEventType.KeyDown, key);
        _state.OnKeyDown(keyEvent);

        Assert.That(_controller.Text, Is.EqualTo("Hell"));
    }

    [Test]
    public void TextField_ShouldHandleArrowKeys()
    {
        _focusNode.RequestFocus();
        _controller.Text = "Hello";
        _controller.Selection = TextSelection.Collapsed(5);

        var left = (int)GlKeys.Left;
        var right = (int)GlKeys.Right;
        Console.WriteLine($"Testing Arrows with codes: Left={left}, Right={right}");

        _state.OnKeyDown(new KeyboardEvent(KeyEventType.KeyDown, left));
        Assert.That(_controller.Selection.BaseOffset, Is.EqualTo(4));

        _state.OnKeyDown(new KeyboardEvent(KeyEventType.KeyDown, right));
        Assert.That(_controller.Selection.BaseOffset, Is.EqualTo(5));
    }
}
