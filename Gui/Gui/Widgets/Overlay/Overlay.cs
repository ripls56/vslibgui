using System;
using System.Collections.Generic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;

namespace Gui.Widgets.Overlay;

public class OverlayEntry : IDisposable
{
    private bool _isMounted;
    internal OverlayState? State;

    public OverlayEntry(
        Widget widget
    )
    {
        Widget = widget;
    }

    public Widget Widget { get; set; }

    public void Dispose() => Remove();

    public void MarkNeedsBuild() => State?.RequestRebuild();

    public void Remove() => State?.RemoveEntry(this);
}

public class Overlay : StatefulWidget
{
    internal readonly List<OverlayEntry> InitialEntries;

    public Overlay(
        IEnumerable<OverlayEntry>? initialEntries = null
    )
    {
        InitialEntries = new List<OverlayEntry>(initialEntries ?? []);
    }

    public override State CreateState() => new OverlayState();

    public static OverlayState? Of(
        BuildContext context
    )
    {
        // Simple search for Overlay in parent tree
        var curr = context.Element;
        while (curr != null)
        {
            if (curr is StatefulElement se && se.State is OverlayState os)
            {
                return os;
            }

            curr = curr.Parent;
        }

        return null;
    }
}

public class OverlayState : State<Overlay>
{
    private readonly List<OverlayEntry> _entries = [];

    internal void RequestRebuild() => SetState(() => { });

    public override void InitState()
    {
        base.InitState();
        foreach (var entry in Widget.InitialEntries)
        {
            entry.State = this;
            _entries.Add(entry);
        }
    }

    public void Insert(
        OverlayEntry entry
    )
    {
        entry.State = this;
        SetState(() => { _entries.Add(entry); }
        );
    }

    public void RemoveEntry(
        OverlayEntry entry
    )
    {
        SetState(() =>
            {
                _entries.Remove(entry);
                entry.State = null;
            }
        );
    }

    public override Widget Build(
        BuildContext context
    ) =>
        new Stack(_entries.ConvertAll(e => e.Widget));
}
