using System;
using Gui.Widgets.Basic;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Widgets.Framework;

/// <summary>
///     A widget rendered in place of a subtree that threw an exception during build.
///     The static <see cref="Builder" /> delegate can be replaced to customize the
///     error presentation across the entire application.
/// </summary>
public class ErrorWidget : StatelessWidget
{
    static ErrorWidget()
    {
        Builder = ex => new ErrorWidget(ex);
    }

    /// <summary>Initializes a new <see cref="ErrorWidget" /> for the given exception.</summary>
    public ErrorWidget(
        Exception exception
    )
    {
        _exception = exception;
    }

    private readonly Exception _exception;

    /// <summary>
    ///     Factory used by the framework to build an error placeholder for a caught exception.
    ///     Replace this delegate to render a custom error widget application-wide.
    ///     Defaults to returning a red <see cref="Container" />.
    /// </summary>
    public static Func<Exception, Widget> Builder { get; set; }

    /// <inheritdoc />
    public override Widget Build(
        BuildContext context
    ) =>
        new Container(
            new BoxStyle { Color = new Vector4(1f, 0f, 0f, 0.8f) },
            new Text(_exception.GetType().Name + ": " + _exception.Message)
        );
}
