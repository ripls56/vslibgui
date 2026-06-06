using System.Collections.Generic;
using Gui.Widgets.Framework;

namespace Gui.Core.Framework;

public readonly struct HitEntry
{
    public Element Element { get; }

    public HitEntry(
        Element element
    )
    {
        Element = element;
    }
}

public class HitTestResult
{
    private readonly List<HitEntry> _path = [];
    public IReadOnlyList<HitEntry> Path => _path;

    public void Add(
        Element element
    ) =>
        _path.Add(new HitEntry(element));

    public void Clear() => _path.Clear();
}
