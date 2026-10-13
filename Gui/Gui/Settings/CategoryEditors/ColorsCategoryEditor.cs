using System;
using Gui.Theming;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Settings.CategoryEditors;

/// <summary>Editor for the 17 ColorScheme colors.</summary>
public sealed class ColorsCategoryEditor : StatelessWidget
{
    private readonly ThemeData _baseTheme;
    private readonly ThemeOverrides _draft;
    private readonly Action _onChanged;

    /// <summary>Creates the colors editor.</summary>
    public ColorsCategoryEditor
    (
        ThemeOverrides draft,
        ThemeData baseTheme,
        Action onChanged
    )
    {
        _draft = draft;
        _baseTheme = baseTheme;
        _onChanged = onChanged;
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        var c = _baseTheme.ColorScheme;
        _draft.Colors ??= new ColorOverrides();
        var o = _draft.Colors;
        return new Column
        (
            12,
            children:
            [
                Field
                (
                    "Primary", o.Primary, c.Primary, v =>
                    {
                        o.Primary = v;
                        _onChanged();
                    }
                ),
                Field
                (
                    "On Primary", o.OnPrimary, c.OnPrimary, v =>
                    {
                        o.OnPrimary = v;
                        _onChanged();
                    }
                ),
                Field
                (
                    "Secondary", o.Secondary, c.Secondary, v =>
                    {
                        o.Secondary = v;
                        _onChanged();
                    }
                ),
                Field
                (
                    "On Secondary", o.OnSecondary, c.OnSecondary, v =>
                    {
                        o.OnSecondary = v;
                        _onChanged();
                    }
                ),
                Field
                (
                    "Surface", o.Surface, c.Surface, v =>
                    {
                        o.Surface = v;
                        _onChanged();
                    }
                ),
                Field
                (
                    "On Surface", o.OnSurface, c.OnSurface, v =>
                    {
                        o.OnSurface = v;
                        _onChanged();
                    }
                ),
                Field
                (
                    "On Surface Variant", o.OnSurfaceVariant, c.OnSurfaceVariant, v =>
                    {
                        o.OnSurfaceVariant = v;
                        _onChanged();
                    }
                ),
                Field
                (
                    "Background", o.Background, c.Background, v =>
                    {
                        o.Background = v;
                        _onChanged();
                    }
                ),
                Field
                (
                    "On Background", o.OnBackground, c.OnBackground, v =>
                    {
                        o.OnBackground = v;
                        _onChanged();
                    }
                ),
                Field
                (
                    "Border", o.Border, c.Border, v =>
                    {
                        o.Border = v;
                        _onChanged();
                    }
                ),
                Field
                (
                    "Error", o.Error, c.Error, v =>
                    {
                        o.Error = v;
                        _onChanged();
                    }
                ),
                Field
                (
                    "On Error", o.OnError, c.OnError, v =>
                    {
                        o.OnError = v;
                        _onChanged();
                    }
                ),
                Field
                (
                    "Surface Low", o.SurfaceLow, c.SurfaceLow, v =>
                    {
                        o.SurfaceLow = v;
                        _onChanged();
                    }
                ),
                Field
                (
                    "Surface High", o.SurfaceHigh, c.SurfaceHigh, v =>
                    {
                        o.SurfaceHigh = v;
                        _onChanged();
                    }
                ),
                Field
                (
                    "State Hover", o.StateHover, c.StateHover, v =>
                    {
                        o.StateHover = v;
                        _onChanged();
                    }
                ),
                Field
                (
                    "State Selected", o.StateSelected, c.StateSelected, v =>
                    {
                        o.StateSelected = v;
                        _onChanged();
                    }
                ),
                Field
                (
                    "Outline Variant", o.OutlineVariant, c.OutlineVariant, v =>
                    {
                        o.OutlineVariant = v;
                        _onChanged();
                    }
                )
            ]
        );
    }

    private Widget Field
    (
        string label,
        Vector4? value,
        Vector4 fallback,
        Action<Vector4?> onChanged
    )
        => new ColorOverrideField(label, value, fallback, onChanged);
}
