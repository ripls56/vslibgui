using OpenTK.Mathematics;

namespace Gui.Widgets.Layout;

public class Alignment
{
    public static readonly Alignment TopLeft = new(
        -1,
        -1
    );

    public static readonly Alignment TopCenter = new(
        0,
        -1
    );

    public static readonly Alignment TopRight = new(
        1,
        -1
    );

    public static readonly Alignment CenterLeft = new(
        -1,
        0
    );

    public static readonly Alignment Center = new(
        0,
        0
    );

    public static readonly Alignment CenterRight = new(
        1,
        0
    );

    public static readonly Alignment BottomLeft = new(
        -1,
        1
    );

    public static readonly Alignment BottomCenter = new(
        0,
        1
    );

    public static readonly Alignment BottomRight = new(
        1,
        1
    );

    public Alignment(
        float x,
        float y
    )
    {
        X = x;
        Y = y;
    }

    public float X { get; }
    public float Y { get; }

    public Vector2 CalculateOffset(
        Vector2 parentSize,
        Vector2 childSize
    )
    {
        var centerX = (parentSize.X - childSize.X) / 2f;
        var centerY = (parentSize.Y - childSize.Y) / 2f;

        return new Vector2(
            centerX + X * centerX,
            centerY + Y * centerY
        );
    }
}

public enum MainAxisAlignment
{
    Start,
    Center,
    End,
    SpaceBetween,
    SpaceAround,
    SpaceEvenly
}

public enum CrossAxisAlignment
{
    Start,
    Center,
    End,
    Stretch
}
