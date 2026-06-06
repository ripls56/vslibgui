using System;
using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace Gui.Shared;

/// <summary>
///     Helpers for removing vanilla <see cref="GuiDialog" /> instances from the
///     engine before installing framework-native replacements. Mirrors what
///     <c>ClientMain</c> does internally on dialog unregistration.
/// </summary>
public static class VanillaDialogCleanup
{
    /// <summary>
    ///     Finds and removes the first dialog matching <paramref name="match" />
    ///     from the engine's <c>LoadedGuis</c> list, then disposes it. No-op when
    ///     no matching dialog exists.
    /// </summary>
    public static void RemoveDialog(ICoreClientAPI capi, Predicate<GuiDialog> match)
    {
        if (capi.World is not ClientMain game)
        {
            return;
        }

        var loadedGuisField = AccessTools.Field(game.GetType(), "LoadedGuis");
        var loaded = loadedGuisField?.GetValue(game) as List<GuiDialog>;
        var guis = loaded?.FindAll(match);

        if (guis == null)
        {
            return;
        }

        foreach (var dialog in guis)
        {
            game.UnregisterDialog(dialog);
            dialog.Dispose();
        }
    }
}
