using System;

namespace Gui.Widgets.Framework;

/// <summary>
///     A platonic widget that calls a builder function to create its child.
///     Useful for obtaining a <see cref="BuildContext" /> in the middle of a build method.
/// </summary>
public class Builder : StatelessWidget
{
    public Builder(
        Func<BuildContext, Widget> builder,
        Key? key = null
    ) : base(key)
    {
        BuilderFunc = builder;
    }

    public Func<BuildContext, Widget> BuilderFunc { get; }

    public override Widget Build(
        BuildContext context
    ) =>
        BuilderFunc(context);
}
