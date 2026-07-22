using System;
using Gui.Theming;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;

namespace Gui.Settings.CategoryEditors;

/// <summary>Editor for misc component styles (ProgressBar, Focus).</summary>
public sealed class MiscCategoryEditor : StatelessWidget
{
    private readonly ThemeData _baseTheme;
    private readonly ThemeOverrides _draft;
    private readonly Action _onChanged;

    /// <summary>Creates the misc editor.</summary>
    public MiscCategoryEditor
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
        return new Column
        (
            spacing: 12,
            children:
            [
                ProgressBarSection(),
                FocusSection()
            ]
        );
    }

    private Widget ProgressBarSection()
    {
        _draft.ProgressBar ??= new ProgressBarOverrides();
        var o = _draft.ProgressBar;
        var s = _baseTheme.ProgressBarStyle;
        return new Column
        (
            spacing: 4,
            children:
            [
                new Text("ProgressBar", _baseTheme.TextTheme.Title),
                new FloatOverrideField
                (
                    "Height", o.Height, 0, 40, 1, s.Height, v =>
                    {
                        o.Height = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Corner Radius", o.CornerRadius, 0, 40, 1, s.CornerRadius, v =>
                    {
                        o.CornerRadius = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Border Thickness", o.BorderThickness, 0, 10, 1, s.BorderThickness, v =>
                    {
                        o.BorderThickness = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Fill Padding", o.FillPadding, 0, 10, 1, s.FillPadding, v =>
                    {
                        o.FillPadding = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    "Fill Color", o.FillColor, s.FillColor, v =>
                    {
                        o.FillColor = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    "Track Color", o.TrackColor, s.TrackColor, v =>
                    {
                        o.TrackColor = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    "Border Color", o.BorderColor, s.BorderColor, v =>
                    {
                        o.BorderColor = v;
                        _onChanged();
                    }
                )
            ]
        );
    }

    private Widget FocusSection()
    {
        _draft.Focus ??= new FocusOverrides();
        var o = _draft.Focus;
        return new Column
        (
            spacing: 4,
            children:
            [
                new Text("Focus", _baseTheme.TextTheme.Title),
                new ColorOverrideField
                (
                    "Outline Color", o.OutlineColor, _baseTheme.FocusOutlineColor, v =>
                    {
                        o.OutlineColor = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Outline Width", o.OutlineWidth, 0, 10, 1, _baseTheme.FocusOutlineWidth, v =>
                    {
                        o.OutlineWidth = v;
                        _onChanged();
                    }
                )
            ]
        );
    }
}
