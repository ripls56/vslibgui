using System;
using System.Collections.Generic;

namespace Gui.Widgets.Framework;

public interface IListenable
{
    void AddListener(
        Action listener
    );

    void RemoveListener(
        Action listener
    );
}

public abstract class ChangeNotifier : IListenable, IDisposable
{
    private readonly List<Action> _listeners = [];

    public virtual void Dispose() => _listeners.Clear();

    public void AddListener(
        Action listener
    ) =>
        _listeners.Add(listener);

    public void RemoveListener(
        Action listener
    ) =>
        _listeners.Remove(listener);

    protected void NotifyListeners()
    {
        var snapshot = _listeners.ToArray();
        foreach (var listener in snapshot)
        {
            listener();
        }
    }
}

public class ValueNotifier<T> : ChangeNotifier
{
    private T _value;

    public ValueNotifier(
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
            if (EqualityComparer<T>.Default.Equals(
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
