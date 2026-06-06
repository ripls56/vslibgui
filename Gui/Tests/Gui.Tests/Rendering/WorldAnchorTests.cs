using Gui.Rendering;
using OpenTK.Mathematics;
using Vintagestory.API.MathTools;

namespace Gui.Tests.Rendering;

public class WorldAnchorTests
{
    /// <summary>
    ///     When the projection produces a negative Z (point behind camera),
    ///     TryProject must return false and not write windowPos.
    /// </summary>
    [Test]
    public void TryProject_BehindCamera_ReturnsFalse()
    {
        // View matrix that places point at negative Z after transform.
        // Identity projection + view that flips Z.
        var projection = IdentityMatrix();
        var view = TranslationMatrix(0, 0, -100); // pushes target far behind camera
        var anchor = new WorldAnchor { WorldPos = new Vec3d(0, 0, 0) };

        var ok = anchor.TryProject(
            projection, view,
            1920, 1080,
            1f,
            new Vector2(200, 100),
            out var windowPos);

        Assert.That(ok, Is.False);
    }

    /// <summary>
    ///     For an identity-ish projection where the world point ends up dead
    ///     center on screen, the resulting windowPos must center the window
    ///     horizontally and apply the configured Align vertically.
    /// </summary>
    [Test]
    public void TryProject_CentersHorizontallyAndAppliesAlign()
    {
        // Skip projection math complexity: instead, test the post-projection
        // arithmetic by feeding matrices that place the projected screen point
        // at frame center with positive depth.
        var projection = IdentityMatrix();
        var view = IdentityMatrix(); // depth z=0 → in front of camera
        var anchor = new WorldAnchor { WorldPos = new Vec3d(0, 0, 0), Align = 0.5 };

        var ok = anchor.TryProject(
            projection, view,
            1920, 1080,
            1f,
            new Vector2(200, 100),
            out var windowPos);

        Assert.That(ok, Is.True);
        // Center horizontally: (frameW/2 - windowW/2) / scale = 960 - 100 = 860
        Assert.That(windowPos.X, Is.EqualTo(860f).Within(1f));
        // Vertical align: (frameH - screenY) / scale - windowH * Align = (1080 - 540) - 50 = 490
        Assert.That(windowPos.Y, Is.EqualTo(490f).Within(1f));
    }

    /// <summary>
    ///     Align=0 places the dialog top-aligned at the projected point;
    ///     Align=1 places it bottom-aligned. The Y delta equals windowHeight.
    /// </summary>
    [Test]
    public void TryProject_AlignAffectsVerticalOffset()
    {
        var projection = IdentityMatrix();
        var view = IdentityMatrix();
        var topAnchor = new WorldAnchor { WorldPos = new Vec3d(0, 0, 0), Align = 0 };
        var bottomAnchor = new WorldAnchor { WorldPos = new Vec3d(0, 0, 0), Align = 1 };

        topAnchor.TryProject(
            IdentityMatrix(), IdentityMatrix(),
            1920, 1080, 1f, new Vector2(200, 100),
            out var topPos);
        bottomAnchor.TryProject(
            IdentityMatrix(), IdentityMatrix(),
            1920, 1080, 1f, new Vector2(200, 100),
            out var bottomPos);

        Assert.That(bottomPos.Y - topPos.Y, Is.EqualTo(-100f).Within(1f));
    }

    private static double[] IdentityMatrix()
    {
        var m = new double[16];
        m[0] = 1;
        m[5] = 1;
        m[10] = 1;
        m[15] = 1;
        return m;
    }

    private static double[] TranslationMatrix(double x, double y, double z)
    {
        var m = IdentityMatrix();
        m[12] = x;
        m[13] = y;
        m[14] = z;
        return m;
    }
}
