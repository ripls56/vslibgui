using System.Collections.Generic;
using Gui.Rendering.Text;
using Gui.Vtml;

namespace Gui.Widgets.Spans;

/// <summary>
///     An <see cref="InlineSpan" /> that clears active floats, advancing the
///     cursor past all floated elements. Analogous to CSS <c>clear: both</c>.
/// </summary>
public sealed class ClearSpan : InlineSpan
{
    /// <inheritdoc />
    internal override void CollectRuns(
        TextStyle inherited,
        List<PlacedRun> runs
    )
    {
        runs.Add(
            new PlacedRun
            {
                Text = "",
                Style = inherited,
                Source = this,
                Type = PlacedRunType.Clear,
                WidgetChildIndex = -1,
                Float = VtmlFloat.None
            }
        );
    }

    /// <inheritdoc />
    internal override bool HasAnyRecognizer() => false;
}
