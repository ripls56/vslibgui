using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Buffer = HarfBuzzSharp.Buffer;

namespace Gui;

/// <summary>
///     Registers a <see cref="NativeLibrary" /> resolver that loads HarfBuzz and SkiaSharp
///     native binaries from the mod's <c>native/{rid}/native/</c> subdirectory, falling back
///     to the default OS search path if the file is not found there.
/// </summary>
internal static class NativeLibraryLoader
{
    private static bool _registered;

    internal static void Register()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;

        var modDir = Path.GetDirectoryName(typeof(NativeLibraryLoader).Assembly.Location) ?? ".";
        var rid = GetRid();
        var nativeDir = Path.Combine(modDir, "native", rid, "native");

        RegisterResolver(typeof(Buffer).Assembly, nativeDir);
    }

    private static void RegisterResolver(Assembly target, string nativeDir)
    {
        NativeLibrary.SetDllImportResolver(target, (name, _, _) =>
        {
            var prefix = name.StartsWith("lib", StringComparison.Ordinal) ? "" : "lib";
            var fileName = OperatingSystem.IsWindows() ? name + ".dll"
                : OperatingSystem.IsMacOS() ? prefix + name + ".dylib"
                : prefix + name + ".so";

            var fullPath = Path.Combine(nativeDir, fileName);
            if (NativeLibrary.TryLoad(fullPath, out var handle))
            {
                return handle;
            }

            NativeLibrary.TryLoad(name, out handle);
            return handle;
        });
    }

    private static string GetRid()
    {
        if (OperatingSystem.IsWindows())
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.Arm64 => "win-arm64",
                Architecture.X86 => "win-x86",
                _ => "win-x64"
            };
        }

        if (OperatingSystem.IsMacOS())
        {
            return "osx";
        }

        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm => "linux-arm",
            Architecture.Arm64 => "linux-arm64",
            Architecture.X86 => "linux-x86",
            _ => "linux-x64"
        };
    }
}
