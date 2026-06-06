namespace Gui.Widgets.Framework;

/// <summary>
///     A widget whose UI is fully determined by its constructor arguments. The Library calls
///     <see cref="Build" /> every time the parent rebuilds and passes a new instance of this
///     widget. There is no internal mutable state — use <see cref="StatefulWidget" /> if you
///     need to track changing values over time.
/// </summary>
public abstract class StatelessWidget : Widget
{
    protected StatelessWidget(
        Key? key = null
    ) : base(key)
    {
    }

    /// <summary>
    ///     Describes the UI for this widget. Called by the Library whenever this widget's
    ///     element needs to rebuild. Must be a pure function of this widget's fields and
    ///     <paramref name="context" /> — do not read or write external mutable state here.
    /// </summary>
    public abstract Widget Build(
        BuildContext context
    );

    public override Element CreateElement() => new ComponentElement(this);
}
