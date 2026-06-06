using OpenTK.Graphics.OpenGL4;

namespace Gui.Rendering;

/// <summary>
///     Captures the slice of global GL state that offscreen / Skia rendering
///     mutates (viewport, blend, depth, stencil, scissor, cull, sRGB, masks)
///     so it can be replayed verbatim afterwards. The game leaves much of this
///     state untouched between passes, so any leak persists into later draws —
///     restoring the exact prior values keeps the rest of the frame identical.
/// </summary>
internal struct GlStateSnapshot
{
    private readonly int[] _viewport;
    private readonly bool[] _colorMask;

    private bool _blend;
    private bool _depthTest;
    private bool _scissorTest;
    private bool _stencilTest;
    private bool _cullFace;
    private bool _sampleAlphaToCoverage;
    private bool _framebufferSrgb;
    private bool _depthMask;

    private int _blendEquationRgb;
    private int _blendEquationAlpha;
    private int _blendSrcRgb;
    private int _blendDstRgb;
    private int _blendSrcAlpha;
    private int _blendDstAlpha;
    private int _depthFunc;

    private GlStateSnapshot(int[] viewport, bool[] colorMask)
    {
        _viewport = viewport;
        _colorMask = colorMask;
        _blend = _depthTest = _scissorTest = _stencilTest = false;
        _cullFace = _sampleAlphaToCoverage = _framebufferSrgb = _depthMask = false;
        _blendEquationRgb = _blendEquationAlpha = 0;
        _blendSrcRgb = _blendDstRgb = _blendSrcAlpha = _blendDstAlpha = 0;
        _depthFunc = 0;
    }

    public static GlStateSnapshot Capture()
    {
        var snapshot = new GlStateSnapshot(new int[4], new bool[4])
        {
            _blend = GL.IsEnabled(EnableCap.Blend),
            _depthTest = GL.IsEnabled(EnableCap.DepthTest),
            _scissorTest = GL.IsEnabled(EnableCap.ScissorTest),
            _stencilTest = GL.IsEnabled(EnableCap.StencilTest),
            _cullFace = GL.IsEnabled(EnableCap.CullFace),
            _sampleAlphaToCoverage = GL.IsEnabled(EnableCap.SampleAlphaToCoverage),
            _framebufferSrgb = GL.IsEnabled(EnableCap.FramebufferSrgb)
        };

        GL.GetInteger(
            GetPName.Viewport,
            snapshot._viewport
        );
        GL.GetBoolean(
            GetPName.DepthWritemask,
            out snapshot._depthMask
        );
        GL.GetBoolean(
            GetPName.ColorWritemask,
            snapshot._colorMask
        );

        GL.GetInteger(
            GetPName.BlendEquationRgb,
            out snapshot._blendEquationRgb
        );
        GL.GetInteger(
            GetPName.BlendEquationAlpha,
            out snapshot._blendEquationAlpha
        );
        GL.GetInteger(
            GetPName.BlendSrcRgb,
            out snapshot._blendSrcRgb
        );
        GL.GetInteger(
            GetPName.BlendDstRgb,
            out snapshot._blendDstRgb
        );
        GL.GetInteger(
            GetPName.BlendSrcAlpha,
            out snapshot._blendSrcAlpha
        );
        GL.GetInteger(
            GetPName.BlendDstAlpha,
            out snapshot._blendDstAlpha
        );
        GL.GetInteger(
            GetPName.DepthFunc,
            out snapshot._depthFunc
        );

        return snapshot;
    }

    public void Restore()
    {
        SetCap(
            EnableCap.Blend,
            _blend
        );
        SetCap(
            EnableCap.DepthTest,
            _depthTest
        );
        SetCap(
            EnableCap.ScissorTest,
            _scissorTest
        );
        SetCap(
            EnableCap.StencilTest,
            _stencilTest
        );
        SetCap(
            EnableCap.CullFace,
            _cullFace
        );
        SetCap(
            EnableCap.SampleAlphaToCoverage,
            _sampleAlphaToCoverage
        );
        SetCap(
            EnableCap.FramebufferSrgb,
            _framebufferSrgb
        );

        GL.DepthMask(_depthMask);
        GL.ColorMask(
            _colorMask[0],
            _colorMask[1],
            _colorMask[2],
            _colorMask[3]
        );
        GL.DepthFunc((DepthFunction)_depthFunc);

        GL.BlendEquationSeparate(
            (BlendEquationMode)_blendEquationRgb,
            (BlendEquationMode)_blendEquationAlpha
        );
        GL.BlendFuncSeparate(
            (BlendingFactorSrc)_blendSrcRgb,
            (BlendingFactorDest)_blendDstRgb,
            (BlendingFactorSrc)_blendSrcAlpha,
            (BlendingFactorDest)_blendDstAlpha
        );

        GL.Viewport(
            _viewport[0],
            _viewport[1],
            _viewport[2],
            _viewport[3]
        );
    }

    private static void SetCap(EnableCap cap, bool enabled)
    {
        if (enabled)
        {
            GL.Enable(cap);
        }
        else
        {
            GL.Disable(cap);
        }
    }
}
