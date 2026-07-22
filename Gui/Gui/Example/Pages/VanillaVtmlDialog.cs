using Vintagestory.API.Client;

namespace Gui.Example.Pages;

/// <summary>
///     Vanilla VS reference dialog — renders the same VTML through the legacy
///     <see cref="GuiComposer" /> pipeline (AddRichtext) for side-by-side
///     comparison with the framework's <c>VtmlText</c>.
/// </summary>
internal sealed class VanillaVtmlDialog : GuiDialog
{
    private readonly string _vtml;

    public VanillaVtmlDialog(string vtml, ICoreClientAPI capi) : base(capi)
    {
        _vtml = vtml;
        Compose();
    }

    public override string ToggleKeyCombinationCode => null!;

    private void Compose()
    {
        var font = CairoFont.WhiteSmallText();

        SingleComposer = capi.Gui
            .CreateCompo("vanillavtml", ElementStdBounds.AutosizedMainDialog)
            .AddDialogTitleBar("Vanilla VTML (reference)", () => TryClose())
            .AddDialogBG(ElementBounds.Fill)
            .AddRichtext
            (
                _vtml,
                font,
                ElementBounds.Fixed(0, 40, 520, 340),
                null,
                "rt"
            )
            .Compose();
    }
}
