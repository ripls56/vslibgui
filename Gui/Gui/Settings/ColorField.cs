using System;
using System.Collections.Generic;
using Gui.Core.Painting;
using Gui.Rendering;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using Gui.Widgets.Overlay;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Settings;

/// <summary>Edits a single RGBA color via a hex text field and a swatch preview.</summary>
public sealed class ColorField : StatefulWidget
{
    internal readonly string _label;
    internal readonly Action<Vector4> _onChanged;
    internal readonly bool _showLabel;
    internal readonly Vector4 _value;

    /// <summary>Creates a color field.</summary>
    public ColorField
    (
        Vector4 value,
        Action<Vector4> onChanged,
        string label,
        bool showLabel = true
    )
    {
        _value = value;
        _onChanged = onChanged;
        _label = label;
        _showLabel = showLabel;
    }

    /// <summary>Parses a hex string (#RGB/#RRGGBB/#RRGGBBAA). Returns false if invalid.</summary>
    public static bool TryParseHex(string text, out Vector4 color)
    {
        color = Vector4.Zero;
        if (string.IsNullOrWhiteSpace(text)) return false;
        try
        {
            color = text.Trim().FromHex();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public override State CreateState() => new ColorFieldState();
}

internal sealed class ColorFieldState : State<ColorField>
{
    private TextEditingController? _hex;
    private readonly LayerLink _link = new();
    private OverlayEntry? _barrierEntry;
    private OverlayEntry? _overlayEntry;
    private bool _isOpen;

    /// <inheritdoc />
    public override void InitState()
    {
        base.InitState();
        _hex = new TextEditingController(Widget._value.ToHex());
    }

    /// <inheritdoc />
    public override void UpdateWidget(ColorField oldWidget)
    {
        base.UpdateWidget(oldWidget);
        if (Widget._value != oldWidget._value)
        {
            _hex!.Text = Widget._value.ToHex();
        }
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        var v = Widget._value;
        var theme = Theme.Of(context);

        var editor = new Row
        (
            spacing: 12,
            crossAxisAlignment: CrossAxisAlignment.Center,
            children:
            [
                new CompositedTransformTarget
                (
                    _link,
                    new GestureDetector
                    (
                        onTap: e => TogglePicker(context),
                        child: new Container
                        (
                            new BoxStyle
                            {
                                Color = v,
                                BorderColor = theme.ColorScheme.OutlineVariant,
                                BorderThickness = 1,
                                CornerRadius = new Vector4(4)
                            },
                            child: new SizedBox(width: 28, height: 28)
                        )
                    )
                ),
                new Expanded
                (
                    new TextField
                    (
                        controller: _hex,
                        onSubmitted: SubmitHex,
                        onChanged: text =>
                        {
                            if (ColorField.TryParseHex(text, out var color))
                            {
                                Commit(color);
                            }
                        }
                    )
                )
            ]
        );

        if (!Widget._showLabel)
        {
            return editor;
        }

        return new Column
        (
            spacing: 8,
            children:
            [
                new Text(Widget._label, theme.TextTheme.Label),
                editor
            ]
        );
    }

    private void SubmitHex(string text)
    {
        if (ColorField.TryParseHex(text, out var color))
        {
            Commit(color);
        }
        else
        {
            _hex!.Text = Widget._value.ToHex();
        }
    }

    private void Commit(Vector4 color)
    {
        Widget._onChanged(color);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        ClosePicker();
        base.Dispose();
    }

    private void TogglePicker(BuildContext context)
    {
        if (_isOpen)
        {
            ClosePicker();
        }
        else
        {
            OpenPicker(context);
        }
    }

    private void OpenPicker(BuildContext context)
    {
        if (_isOpen)
        {
            return;
        }

        var overlay = Overlay.Of(context);
        if (overlay == null)
        {
            return;
        }

        var ro = Element.RenderObject!;
        var size = ro.Size;
        var globalPos = ro.LocalToGlobal(Vector2.Zero) + ro.GetScreenOffset();

        _barrierEntry = new OverlayEntry(BuildBarrier());
        _overlayEntry = new OverlayEntry(
            new AnchoredOverlay(
                globalPos.X,
                globalPos.Y,
                size.Y,
                8f,
                OverlaySide.Bottom,
                HorizontalAnchor.Start,
                link: _link,
                child: BuildPicker(context)
            )
        );
        overlay.Insert(_barrierEntry);
        overlay.Insert(_overlayEntry);
        SetState(() => _isOpen = true);
    }

    private void ClosePicker()
    {
        if (!_isOpen)
        {
            return;
        }

        _overlayEntry?.Remove();
        _overlayEntry = null;
        _barrierEntry?.Remove();
        _barrierEntry = null;
        SetState(() => _isOpen = false);
    }

    private Widget BuildBarrier()
    {
        return new Positioned(
            0,
            0,
            0,
            0,
            child: new GestureDetector(
                onTap: _ => ClosePicker(),
                child: new Container(
                    new BoxStyle
                    {
                        HitTestBehavior = HitTestBehavior.Opaque
                    }
                )
            )
        );
    }

    private Widget BuildPicker(BuildContext context)
    {
        var theme = Theme.Of(context);
        return new Container(
            new BoxStyle
            {
                Color = theme.ColorScheme.Surface,
                BorderColor = theme.ColorScheme.OutlineVariant,
                BorderThickness = 1,
                CornerRadius = new Vector4(8),
                Padding = EdgeInsets.All(8)
            },
            child: new ColorPicker(
                value: Widget._value,
                onChanged: Commit,
                onChangeEnd: Commit
            )
        );
    }
}

/// <summary>
///     A small ghost button rendered with an <c>✕</c> glyph that invokes a reset callback.
///     Shares the theme's button variant styling but uses a tight padding so it fits inside an
///     override-field row.
/// </summary>
internal sealed class OverrideFieldCard : StatelessWidget
{
    private const float LabelWidth = 200f;

    private readonly Widget _editor;
    private readonly bool _hasOverride;
    private readonly string _label;
    private readonly Action _onReset;

    public OverrideFieldCard(string label, Widget editor, bool hasOverride, Action onReset)
    {
        _label = label;
        _editor = editor;
        _hasOverride = hasOverride;
        _onReset = onReset;
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        return new Container
        (
            new BoxStyle
            {
                Color = theme.ColorScheme.SurfaceLow,
                BorderColor = theme.ColorScheme.OutlineVariant,
                BorderThickness = 1f,
                CornerRadius = new Vector4(8f),
                Padding = EdgeInsets.Symmetric(vertical: 8f, horizontal: 12f)
            },
            new Row
            (
                spacing: 12,
                crossAxisAlignment: CrossAxisAlignment.Center,
                children:
                [
                    new SizedBox
                    (
                        width: LabelWidth,
                        child: new Text(_label, theme.TextTheme.Label)
                    ),
                    new Expanded(_editor),
                    new ClearButton(_onReset, _hasOverride)
                ]
            )
        );
    }
}

internal sealed class ClearButton : StatelessWidget
{
    private readonly bool _enabled;
    private readonly Action _onClear;

    /// <summary>Creates a clear button.</summary>
    public ClearButton(Action onClear, bool enabled)
    {
        _onClear = onClear;
        _enabled = enabled;
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        var style = Theme.Of(context).IconButtonStyle with
        {
            Size = 28f
        };
        return new Tooltip
        (
            new IconButton
            (
                new Icon("gui", "textures/icons/undo-2.svg", 12),
                ButtonVariant.Ghost,
                onTap: _ => _onClear(),
                enabled: _enabled,
                style: style
            ),
            new Text("Reset to inherited value")
        );
    }
}

/// <summary>Binds a <see cref="ColorField"/> to a nullable override, with a clear button.</summary>
public sealed class ColorOverrideField : StatelessWidget
{
    private readonly Vector4 _fallback;
    private readonly string _label;
    private readonly Action<Vector4?> _onChanged;
    private readonly Vector4? _value;

    /// <summary>Creates a nullable color override field. <paramref name="fallback"/> is shown when the override is null.</summary>
    public ColorOverrideField
    (
        string label,
        Vector4? value,
        Vector4 fallback,
        Action<Vector4?> onChanged
    )
    {
        _label = label;
        _value = value;
        _onChanged = onChanged;
        _fallback = fallback;
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context) => new OverrideFieldCard
    (
        _label,
        new ColorField
        (
            _value ?? _fallback,
            c => _onChanged(c),
            _label,
            showLabel: false
        ),
        _value.HasValue,
        () => _onChanged(null)
    );

}

/// <summary>Binds a <see cref="NumericField"/> to a nullable float override, with a clear button.</summary>
public sealed class FloatOverrideField : StatelessWidget
{
    private readonly float _fallback;
    private readonly string _label;
    private readonly float _max;
    private readonly float _min;
    private readonly Action<float?> _onChanged;
    private readonly float _step;
    private readonly float? _value;

    /// <summary>Creates a nullable float override field. <paramref name="fallback"/> is shown when the override is null.</summary>
    public FloatOverrideField
    (
        string label,
        float? value,
        float min,
        float max,
        float step,
        float fallback,
        Action<float?> onChanged
    )
    {
        _label = label;
        _value = value;
        _min = min;
        _max = max;
        _step = step;
        _onChanged = onChanged;
        _fallback = fallback;
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context) => new OverrideFieldCard
    (
        _label,
        new NumericField
        (
            initialValue: _value ?? _fallback,
            step: _step,
            onChanged: f => _onChanged(f)
        ),
        _value.HasValue,
        () => _onChanged(null)
    );
}

/// <summary>Binds a <see cref="Dropdown{T}"/> to a nullable enum override, with a clear button.</summary>
public sealed class EnumOverrideField<T> : StatelessWidget where T : struct, Enum
{
    private readonly T _fallback;
    private readonly string _label;
    private readonly Action<T?> _onChanged;
    private readonly T? _value;

    /// <summary>Creates a nullable enum override field. <paramref name="fallback"/> is shown when the override is null.</summary>
    public EnumOverrideField
    (
        string label,
        T? value,
        T fallback,
        Action<T?> onChanged
    )
    {
        _label = label;
        _value = value;
        _onChanged = onChanged;
        _fallback = fallback;
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        var items = new List<DropdownItem<T>>();
        foreach (T value in Enum.GetValues(typeof(T)))
        {
            items.Add
            (
                new DropdownItem<T>
                {
                    Value = value, Label = value.ToString()
                }
            );
        }

        return new OverrideFieldCard
        (
            _label,
            new Dropdown<T>
            (
                value: _value ?? _fallback,
                items: items,
                onChanged: v => _onChanged(v)
            ),
            _value.HasValue,
            () => _onChanged(null)
        );
    }
}

/// <summary>Binds four <see cref="NumericField"/>s to a nullable <see cref="EdgeInsets"/> override, with a clear button.</summary>
public sealed class EdgeInsetsOverrideField : StatelessWidget
{
    private readonly EdgeInsets _fallback;
    private readonly string _label;
    private readonly Action<EdgeInsets?> _onChanged;
    private readonly EdgeInsets? _value;

    /// <summary>Creates a nullable EdgeInsets override field. <paramref name="fallback"/> is shown when the override is null.</summary>
    public EdgeInsetsOverrideField
    (
        string label,
        EdgeInsets? value,
        EdgeInsets fallback,
        Action<EdgeInsets?> onChanged
    )
    {
        _label = label;
        _value = value;
        _onChanged = onChanged;
        _fallback = fallback;
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context) => new OverrideFieldCard
    (
        _label,
        new Row
        (
            spacing: 8,
            children:
            [
                CreateEdgeField
                (
                    "L", _value?.Left ?? _fallback.Left,
                    v => Commit
                    (
                        v, _value?.Top ?? _fallback.Top, _value?.Right ?? _fallback.Right,
                        _value?.Bottom ?? _fallback.Bottom
                    )
                ),
                CreateEdgeField
                (
                    "T", _value?.Top ?? _fallback.Top,
                    v => Commit
                    (
                        _value?.Left ?? _fallback.Left, v, _value?.Right ?? _fallback.Right,
                        _value?.Bottom ?? _fallback.Bottom
                    )
                ),
                CreateEdgeField
                (
                    "R", _value?.Right ?? _fallback.Right,
                    v => Commit
                    (
                        _value?.Left ?? _fallback.Left, _value?.Top ?? _fallback.Top, v,
                        _value?.Bottom ?? _fallback.Bottom
                    )
                ),
                CreateEdgeField
                (
                    "B", _value?.Bottom ?? _fallback.Bottom,
                    v => Commit
                    (
                        _value?.Left ?? _fallback.Left, _value?.Top ?? _fallback.Top,
                        _value?.Right ?? _fallback.Right, v
                    )
                )
            ]
        ),
        _value.HasValue,
        () => _onChanged(null)
    );

    private Widget CreateEdgeField
    (
        string label,
        float value,
        Action<float> onChanged
    )
    {
        return new Expanded
        (
            new Row
            (
                spacing: 4,
                crossAxisAlignment: CrossAxisAlignment.Center,
                children:
                [
                    new Text(label),
                    new Expanded
                    (
                        new NumericField
                        (
                            initialValue: value,
                            step: 1,
                            onChanged: onChanged,
                            style: new BoxStyle
                            {
                                Height = 32
                            }
                        )
                    )
                ]
            )
        );
    }

    private void Commit
    (
        float left,
        float top,
        float right,
        float bottom
    )
    {
        _onChanged(new EdgeInsets(left, top, right, bottom));
    }
}
