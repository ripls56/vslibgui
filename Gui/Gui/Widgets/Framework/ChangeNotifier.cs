using System;
using System.Collections.Generic;

namespace Gui.Widgets.Framework;

public interface IListenable
{
    void AddListener
    (
        Action listener
    );

    void RemoveListener
    (
        Action listener
    );
}

public abstract class ChangeNotifier : IListenable, IDisposable
{
    private readonly List<Action> _listeners = [];

    public virtual void Dispose() => _listeners.Clear();

    public void AddListener
    (
        Action listener
    ) =>
        _listeners.Add(listener);

    public void RemoveListener
    (
        Action listener
    ) =>
        _listeners.Remove(listener);

    protected void NotifyListeners()
    {
        // Iterate backwards by index so a listener removing itself (or a
        // later listener) during the callback doesn't skip or repeat entries,
        // avoiding a per-notify array allocation.
        for (var i = _listeners.Count - 1; i >= 0; i--)
        {
            if (i < _listeners.Count)
            {
                _listeners[i]();
            }
        }
    }
}

public class ValueNotifier<T> : ChangeNotifier
{
    private T _value;

    public ValueNotifier
    (
        T value
    )
    {
        _value = value;
    }

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals
                (
                    _value,
                    value
                ))
            {
                return;
            }

            _value = value;
            NotifyListeners();
        }
    }
}
