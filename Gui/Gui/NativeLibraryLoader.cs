using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Vintagestory.API.Common;
using Buffer = HarfBuzzSharp.Buffer;

namespace Gui;

/// <summary>
///     Registers a <see cref="NativeLibrary" /> resolver that loads HarfBuzz and SkiaSharp
///     native binaries from the mod's <c>native/{rid}/native/</c> subdirectory, falling back
///     to the default OS search path if the file is not found there.
///     <para>
///         On Linux the bundled <c>libHarfBuzzSharp.so</c> statically links harfbuzz, but
///         routes 59 of its own <c>hb_*</c> calls through the PLT with default visibility.
///         Under normal ELF scope rules those bind to the distribution's
///         <c>libharfbuzz.so.0</c> whenever it is already in the process (pulled in through
///         pango/cairo on Arch-family distros), so a single object gets allocated by one
///         harfbuzz and freed by another — <c>free(): invalid pointer</c>, abort. Loading with
///         <c>RTLD_DEEPBIND</c> makes those lookups prefer our own symbols and removes the
///         dependency on the system harfbuzz version.
///     </para>
/// </summary>
internal static class NativeLibraryLoader
{
    private static bool _registered;
    private static bool _diagnosticsLogged;
    private static ILogger? _logger;

    internal static void Register
    (
        ILogger? logger = null
    )
    {
        _logger = logger;

        if (_registered)
        {
            return;
        }

        _registered = true;

        var modDir = Path.GetDirectoryName(typeof(NativeLibraryLoader).Assembly.Location) ?? ".";
        var rid = GetRid();
        var nativeDir = Path.Combine(modDir, "native", rid, "native");

        try
        {
            RegisterResolver(typeof(Buffer).Assembly, nativeDir);
        }
        catch (InvalidOperationException e)
        {
            // Only one resolver per assembly is allowed. Mods that hit the harfbuzz
            // interposition crash before 4.0.0 shipped their own, usually with an aggressive
            // ExecuteOrder so it ran first. Keep theirs instead of aborting client startup.
            _logger?.Warning
            (
                "[GUI] Another mod already registered a native resolver for HarfBuzzSharp; "
                + "keeping it. If it does not load the bundled library with RTLD_DEEPBIND, "
                + "the system libharfbuzz can still interpose our symbols. {0}",
                e.Message
            );
        }
    }

    private static void RegisterResolver
    (
        Assembly target,
        string nativeDir
    )
    {
        NativeLibrary.SetDllImportResolver
        (
            target,
            (
                name,
                _,
                _
            ) =>
            {
                var prefix = name.StartsWith("lib", StringComparison.Ordinal) ? "" : "lib";
                var fileName = OperatingSystem.IsWindows() ? name + ".dll"
                    : OperatingSystem.IsMacOS() ? prefix + name + ".dylib"
                    : prefix + name + ".so";

                var fullPath = Path.Combine(nativeDir, fileName);
                var handle = LoadBundled(fullPath);
                if (handle != IntPtr.Zero)
                {
                    LogDiagnosticsOnce(name, fullPath, handle);
                    return handle;
                }

                NativeLibrary.TryLoad(name, out handle);
                LogDiagnosticsOnce(name, "<OS search path>", handle);
                return handle;
            }
        );
    }

    /// <summary>
    ///     Loads the mod-local native library. On Linux this goes through <c>dlopen</c> with
    ///     <c>RTLD_DEEPBIND</c> so that the library's internal <c>hb_*</c> PLT entries resolve
    ///     against itself rather than an already-loaded system harfbuzz.
    /// </summary>
    private static IntPtr LoadBundled
    (
        string fullPath
    )
    {
        if (!OperatingSystem.IsLinux() || !File.Exists(fullPath))
        {
            return NativeLibrary.TryLoad(fullPath, out var loaded) ? loaded : IntPtr.Zero;
        }

        var handle = Dl.Open(fullPath, Dl.RtldNow | Dl.RtldDeepbind);
        if (handle != IntPtr.Zero)
        {
            return handle;
        }

        // Read the reason before the next dlopen call can overwrite it.
        var reason = Dl.LastError() ?? "unknown error";

        // RTLD_DEEPBIND can fail when the allocator is interposed (jemalloc/tcmalloc via
        // LD_PRELOAD). Fall back to a plain load — same behaviour as before this fix.
        handle = Dl.Open(fullPath, Dl.RtldNow);
        if (handle != IntPtr.Zero)
        {
            _logger?.Warning
            (
                "[GUI] RTLD_DEEPBIND load of {0} failed ({1}); loaded without it. "
                + "A system libharfbuzz may interpose our symbols and crash the process.",
                fullPath,
                reason
            );
            return handle;
        }

        return NativeLibrary.TryLoad(fullPath, out var fallback) ? fallback : IntPtr.Zero;
    }

    /// <summary>
    ///     Logs which binary satisfied the first native resolution, its harfbuzz version, and
    ///     whether a system harfbuzz is already resident. Without this, Linux crash reports
    ///     carry no usable information because the process aborts inside libc.
    /// </summary>
    private static void LogDiagnosticsOnce
    (
        string name,
        string path,
        IntPtr handle
    )
    {
        if (_diagnosticsLogged || _logger == null)
        {
            return;
        }

        _diagnosticsLogged = true;

        if (handle == IntPtr.Zero)
        {
            _logger.Error("[GUI] Failed to load native library {0}.", name);
            return;
        }

        var version = ReadHarfBuzzVersion(handle) ?? "unknown";
        var systemNote = OperatingSystem.IsLinux()
            ? Dl.IsLoaded("libharfbuzz.so.0")
                ? "; system libharfbuzz.so.0 is also resident"
                : "; no system libharfbuzz.so.0"
            : "";

        _logger.Notification
        (
            "[GUI] Native {0} loaded from {1} (harfbuzz {2}){3}",
            name,
            path,
            version,
            systemNote
        );
    }

    private static unsafe string? ReadHarfBuzzVersion
    (
        IntPtr handle
    )
    {
        try
        {
            if (!NativeLibrary.TryGetExport(handle, "hb_version_string", out var fn))
            {
                return null;
            }

            var ptr = ((delegate* unmanaged[Cdecl]<byte*>)fn)();
            return ptr == null ? null : Marshal.PtrToStringUTF8((IntPtr)ptr);
        }
        catch (Exception)
        {
            // Diagnostics must never take down startup.
            return null;
        }
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

    /// <summary>
    ///     Minimal <c>dlfcn.h</c> bindings. <c>dlopen</c> lives in libc since glibc 2.34 and in
    ///     libdl.so.2 before that, so both are attempted.
    /// </summary>
    private static class Dl
    {
        internal const int RtldNow = 0x2;
        internal const int RtldNoload = 0x4;
        internal const int RtldDeepbind = 0x8;

        internal static IntPtr Open
        (
            string file,
            int flags
        )
        {
            try
            {
                return LibcOpen(file, flags);
            }
            catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
            {
                try
                {
                    return LibdlOpen(file, flags);
                }
                catch (Exception inner) when (inner is DllNotFoundException
                                                  or EntryPointNotFoundException)
                {
                    return IntPtr.Zero;
                }
            }
        }

        /// <summary>
        ///     Returns whether <paramref name="soname" /> is already resident in the process.
        ///     Does not load it: <c>RTLD_NOLOAD</c> yields a handle only for a library that is
        ///     already mapped.
        /// </summary>
        internal static bool IsLoaded
        (
            string soname
        )
        {
            var handle = Open(soname, RtldNow | RtldNoload);
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            Close(handle);
            return true;
        }

        internal static string? LastError()
        {
            try
            {
                var ptr = LibcError();
                return ptr == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(ptr);
            }
            catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
            {
                return null;
            }
        }

        private static void Close
        (
            IntPtr handle
        )
        {
            try
            {
                LibcClose(handle);
            }
            catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
            {
                // Leaking one refcount on a diagnostic probe is harmless.
            }
        }

        [DllImport("libc.so.6", EntryPoint = "dlopen", CharSet = CharSet.Ansi)]
        private static extern IntPtr LibcOpen
        (
            string file,
            int flags
        );

        [DllImport("libdl.so.2", EntryPoint = "dlopen", CharSet = CharSet.Ansi)]
        private static extern IntPtr LibdlOpen
        (
            string file,
            int flags
        );

        [DllImport("libc.so.6", EntryPoint = "dlclose")]
        private static extern int LibcClose
        (
            IntPtr handle
        );

        [DllImport("libc.so.6", EntryPoint = "dlerror")]
        private static extern IntPtr LibcError();
    }
}
