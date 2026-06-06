using Gui.Widgets.Framework;

namespace Gui.Widgets.Input;

public class FocusNode : ChangeNotifier
{
    private FocusManager? _manager;

    public Element? Owner { get; set; }

    public bool HasFocus { get; private set; }

    internal void SetHasFocus(
        bool value,
        FocusManager? manager
    )
    {
        if (HasFocus == value)
        {
            return;
        }

        HasFocus = value;
        _manager = value
            ? manager
            : null;
        NotifyListeners();
    }

    public void RequestFocus()
    {
        var manager = _manager ?? Owner?.Owner?.FocusManager;
        if (manager != null)
        {
            manager.RequestFocus(this);
        }
        else
        {
            SetHasFocus(
                true,
                null
            );
        }
    }

    public void Unfocus()
    {
        var manager = _manager ?? Owner?.Owner?.FocusManager;
        if (HasFocus && manager != null)
        {
            manager.RequestFocus(null);
        }
        else
        {
            SetHasFocus(
                false,
                null
            );
        }
    }
}

public class FocusManager
{
    public FocusNode? PrimaryFocus { get; private set; }

    public void RequestFocus(
        FocusNode? node
    )
    {
        if (PrimaryFocus == node)
        {
            return;
        }

        var oldFocus = PrimaryFocus;
        PrimaryFocus = node;

        oldFocus?.SetHasFocus(
            false,
            null
        );
        node?.SetHasFocus(
            true,
            this
        );
    }
}
