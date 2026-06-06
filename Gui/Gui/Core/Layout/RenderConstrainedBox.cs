using Gui.Core.Framework;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Core.Layout;

/// <summary>
///     A render object that imposes additional constraints on its child.
/// </summary>
public class RenderConstrainedBox : RenderBox
{
    private LayoutConstraints _additionalConstraints;

    public RenderConstrainedBox()
    {
        _additionalConstraints = new LayoutConstraints();
    }

    public RenderConstrainedBox(
        LayoutConstraints additionalConstraints
    )
    {
        _additionalConstraints = additionalConstraints;
    }

    public LayoutConstraints AdditionalConstraints
    {
        get => _additionalConstraints;
        set
        {
            if (!_additionalConstraints.Equals(value))
            {
                _additionalConstraints = value;
                MarkNeedsLayout();
            }
        }
    }

    // Properties for compatibility with existing code (SizedBox, Container)
    public float? MinWidth
    {
        get => _additionalConstraints.MinWidth;
        set
        {
            if (_additionalConstraints.MinWidth != (value ?? 0))
            {
                _additionalConstraints.MinWidth = value ?? 0;
                MarkNeedsLayout();
            }
        }
    }

    public float? MaxWidth
    {
        get => _additionalConstraints.MaxWidth;
        set
        {
            if (_additionalConstraints.MaxWidth != (value ?? float.PositiveInfinity))
            {
                _additionalConstraints.MaxWidth = value ?? float.PositiveInfinity;
                MarkNeedsLayout();
            }
        }
    }

    public float? MinHeight
    {
        get => _additionalConstraints.MinHeight;
        set
        {
            if (_additionalConstraints.MinHeight != (value ?? 0))
            {
                _additionalConstraints.MinHeight = value ?? 0;
                MarkNeedsLayout();
            }
        }
    }

    public float? MaxHeight
    {
        get => _additionalConstraints.MaxHeight;
        set
        {
            if (_additionalConstraints.MaxHeight != (value ?? float.PositiveInfinity))
            {
                _additionalConstraints.MaxHeight = value ?? float.PositiveInfinity;
                MarkNeedsLayout();
            }
        }
    }

    protected override void PerformLayout()
    {
        var innerConstraints = _additionalConstraints.Enforce(Constraints);

        if (Children.Count > 0)
        {
            var child = Children[0];
            child.X = 0;
            child.Y = 0;
            child.Layout(innerConstraints);
            Size = Constraints.Constrain(child.Size);
        }
        else
        {
            // When there are no children:
            // - If explicit constraints are tight (Min == Max), use that value
            // - Otherwise use 0
            // This handles SizedBox(width, height) with explicit values properly
            var width = _additionalConstraints.MinWidth == _additionalConstraints.MaxWidth
                ? _additionalConstraints.MaxWidth
                : 0;
            var height = _additionalConstraints.MinHeight == _additionalConstraints.MaxHeight
                ? _additionalConstraints.MaxHeight
                : 0;
            Size = Constraints.Constrain(
                new Vector2(
                    width,
                    height
                )
            );
        }
    }
}
