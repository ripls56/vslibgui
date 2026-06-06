using System;

namespace Gui.Widgets.Events;

/// <summary>
///     Mouse button identifiers. Values match
///     <c>Vintagestory.API.Common.EnumMouseButton</c> so that a direct
///     cast from <c>MouseEvent.Button</c> is valid.
/// </summary>
public enum PointerButton
{
    Left = 0,
    Middle = 1,
    Right = 2
}

public class PointerEvent
{
    public PointerEvent(
        float x,
        float y,
        PointerButton button = PointerButton.Left,
        float delta = 0
    )
    {
        X = x;
        Y = y;
        Button = button;
        Delta = delta;
    }

    public float X { get; }
    public float Y { get; }
    public float Delta { get; }
    public PointerButton Button { get; }
    public bool Handled { get; set; }
}

public interface IPointerDownHandler
{
    void OnPointerDown(
        PointerEvent e
    );
}

public interface IPointerUpHandler
{
    void OnPointerUp(
        PointerEvent e
    );
}

public interface IPointerClickHandler
{
    void OnPointerClick(
        PointerEvent e
    );
}

public interface IPointerEnterHandler
{
    void OnPointerEnter(
        PointerEvent e
    );
}

public interface IPointerExitHandler
{
    void OnPointerExit(
        PointerEvent e
    );
}

public interface IPointerMoveHandler
{
    void OnPointerMove(
        PointerEvent e
    );
}

public interface IPointerWheelHandler
{
    void OnMouseWheel(
        PointerEvent e
    );
}

/// <summary>
///     Fired when the pointer enters an element while a mouse button is held
///     down (i.e. during a drag operation). Unlike <see cref="IPointerEnterHandler" />,
///     this is dispatched even when pointer capture is active on another element.
/// </summary>
public interface IPointerDragEnterHandler
{
    void OnPointerDragEnter(
        PointerEvent e
    );
}

/// <summary>
///     Fired when the pointer leaves an element during a drag operation.
///     Counterpart of <see cref="IPointerDragEnterHandler" />.
/// </summary>
public interface IPointerDragExitHandler
{
    void OnPointerDragExit(
        PointerEvent e
    );
}

/// <summary>
///     Optional interface for widgets that declare all pointer-handler interfaces at the
///     class level but only actively handle a subset based on which callbacks are set.
///     When implemented, hit-testing and event dispatch use <see cref="HandlesEvent" />
///     as a secondary gate before considering the widget interactive.
/// </summary>
public interface ISelectiveEventHandler
{
    bool HandlesEvent(
        Type eventInterface
    );
}
