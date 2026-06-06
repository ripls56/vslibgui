using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Gui.Clipboard;

internal sealed class GameClipboard(ICoreClientAPI capi) : IClipboard
{
    private IXPlatformInterface? XPlatform => capi.Forms;

    public string GetText() => XPlatform?.GetClipboardText() ?? string.Empty;

    public void SetText(string text) => XPlatform?.SetClipboardText(text);
}
