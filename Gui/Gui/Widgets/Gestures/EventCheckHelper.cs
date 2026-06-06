using System;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Gestures;

/// <summary>
///     Shared logic for checking whether an object handles pointer or
///     focus events. Used by <see cref="EventDispatcher" /> (hit-testing)
///     and <see cref="Element" /> (interactivity checks).
/// </summary>
public static class EventCheckHelper
{
    private static readonly Type[] PointerHandlerTypes =
    [
        typeof(IPointerDownHandler),
        typeof(IPointerClickHandler),
        typeof(IPointerEnterHandler),
        typeof(IPointerWheelHandler),
        typeof(IPointerDragEnterHandler)
    ];

    /// <summary>
    ///     Returns true if <paramref name="source" /> implements any pointer
    ///     handler interface and, when also
    ///     <see cref="ISelectiveEventHandler" />, reports handling at least
    ///     one event type.
    /// </summary>
    public static bool HandlesAnyPointerEvent(
        object source
    )
    {
        var declaresAny = source is IPointerDownHandler ||
                          source is IPointerClickHandler ||
                          source is IPointerEnterHandler ||
                          source is IPointerWheelHandler ||
                          source is IPointerDragEnterHandler;
        if (!declaresAny)
        {
            return false;
        }

        if (source is ISelectiveEventHandler sel)
        {
            foreach (var t in PointerHandlerTypes)
            {
                if (sel.HandlesEvent(t))
                {
                    return true;
                }
            }

            return false;
        }

        return true;
    }

    /// <summary>
    ///     Returns true if <paramref name="source" /> handles any pointer
    ///     event or implements <see cref="IFocusable" />. Used for hit-test
    ///     interactivity checks that include keyboard focus.
    /// </summary>
    public static bool IsInteractive(
        object source
    )
    {
        if (source is IFocusable)
        {
            return true;
        }

        return HandlesAnyPointerEvent(source);
    }
}
