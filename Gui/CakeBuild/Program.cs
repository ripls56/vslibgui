using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using Cake.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace CakeBuild;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}

public class BuildContext : FrostingContext
{
    public const string ProjectName = "Gui";

    public BuildContext(ICakeContext context)
        : base(context)
    {
        BuildConfiguration = context.Argument("configuration", "Release");
        SkipJsonValidation = context.Argument("skipJsonValidation", false);
        GenerateSuppression = context.Argument("generate-suppression", false);
        var modInfo = context.DeserializeJsonFromFile<ModInfo>($"../{ProjectName}/modinfo.json");
        Version = modInfo.Version;
        Name = modInfo.ModID;
    }

    public string BuildConfiguration { get; }
    public string Version { get; }
    public string Name { get; }
    public bool SkipJsonValidation { get; }
    public bool GenerateSuppression { get; }
}

[TaskName("ValidateJson")]
public sealed class ValidateJsonTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (context.SkipJsonValidation)
        {
            return;
        }

        var jsonFiles = context.GetFiles($"../{BuildContext.ProjectName}/assets/**/*.json");
        foreach (var file in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(file.FullPath);
                JToken.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new Exception(
                    $"Validation failed for JSON file: {file.FullPath}{Environment.NewLine}{ex.Message}",
                    ex
                );
            }
        }
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(ValidateJsonTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetClean(
            $"../{BuildContext.ProjectName}/{BuildContext.ProjectName}.csproj",
            new DotNetCleanSettings { Configuration = context.BuildConfiguration }
        );


        context.DotNetPublish(
            $"../{BuildContext.ProjectName}/{BuildContext.ProjectName}.csproj",
            new DotNetPublishSettings { Configuration = context.BuildConfiguration }
        );
    }
}

[TaskName("CheckApiCompat")]
[IsDependentOn(typeof(BuildTask))]
public sealed class CheckApiCompatTask : FrostingTask<BuildContext>
{
    private const string BaselineDll = "../compat/baseline/Gui.dll";
    private const string SuppressionFile = "../compat/ApiCompatSuppressions.xml";

    public override void Run(BuildContext context)
    {
        if (!context.FileExists(BaselineDll))
        {
            context.Log.Information("No baseline contract yet — skipping API compatibility check.");
            return;
        }

        // ponytail: apicompat ships as a local tool (manifest at the solution root) — restore it
        // so the build is reproducible without a manual `dotnet tool install -g`. Run from the
        // solution root so dotnet resolves the manifest regardless of the process working dir.
        var solutionRoot = context.MakeAbsolute(context.Directory("../"));

        var restoreExitCode = context.StartProcess("dotnet", new Cake.Core.IO.ProcessSettings
        {
            Arguments = "tool restore",
            WorkingDirectory = solutionRoot
        });
        if (restoreExitCode != 0)
        {
            throw new Exception($"dotnet tool restore failed (exit code {restoreExitCode}). See output above.");
        }

        var rightDll = $"../{BuildContext.ProjectName}/bin/{context.BuildConfiguration}/Mods/mod/publish/{BuildContext.ProjectName}.dll";
        var vsDir = context.Environment.GetEnvironmentVariable("VINTAGE_STORY")
            ?? throw new Exception("VINTAGE_STORY environment variable is not set.");

        var publishDir = $"../{BuildContext.ProjectName}/bin/{context.BuildConfiguration}/Mods/mod/publish";

        var refDirs = new List<string>(FrameworkReferenceDirs())
        {
            vsDir,
            Path.Combine(vsDir, "Lib"),
            Path.Combine(vsDir, "Mods"),
            publishDir
        };

        var refs = string.Join(",", refDirs);

        var args = BuildApiCompatArgs(BaselineDll, rightDll, refs, context);

        var exitCode = context.StartProcess("dotnet", new Cake.Core.IO.ProcessSettings
        {
            Arguments = args
        });

        if (exitCode != 0)
        {
            if (context.GenerateSuppression)
            {
                context.Log.Information($"Wrote API compatibility suppressions to {SuppressionFile}. Review, commit, then re-run without --generate-suppression.");
                return;
            }
            throw new Exception($"API compatibility check failed (exit code {exitCode}). Review the CP#### errors above and either fix the break or bless it via compat/ApiCompatSuppressions.xml, or regenerate with --generate-suppression.");
        }
    }

    private static string BuildApiCompatArgs(string left, string right, string refs, BuildContext context)
    {
        var parts = new System.Collections.Generic.List<string>
        {
            "apicompat",
            $"--left \"{left}\"",
            $"--right \"{right}\"",
            $"--left-assembly-references \"{refs}\"",
            $"--right-assembly-references \"{refs}\""
        };

        if (context.FileExists(SuppressionFile))
        {
            parts.Add($"--suppression-file \"{SuppressionFile}\"");
        }

        if (context.GenerateSuppression)
        {
            parts.Add("--generate-suppression-file");
            parts.Add($"--suppression-output-file \"{SuppressionFile}\"");
        }

        return string.Join(" ", parts);
    }

    private static IEnumerable<string> FrameworkReferenceDirs()
    {
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        yield return runtimeDir;

        var dotnetRoot = Directory
            .GetParent(runtimeDir.TrimEnd(Path.DirectorySeparatorChar))
            ?.Parent?.Parent?.FullName;
        if (dotnetRoot is null)
        {
            yield break;
        }

        var packsRoot = Path.Combine(dotnetRoot, "packs");
        foreach (var pack in new[] { "Microsoft.WindowsDesktop.App.Ref", "Microsoft.AspNetCore.App.Ref" })
        {
            var referenceDir = LatestReferenceDir(Path.Combine(packsRoot, pack));
            if (referenceDir is not null)
            {
                yield return referenceDir;
            }
        }
    }

    private static string LatestReferenceDir(string packDir)
    {
        if (!Directory.Exists(packDir))
        {
            return null;
        }

        var latestVersion = Directory.GetDirectories(packDir)
            .OrderBy(d => Version.TryParse(Path.GetFileName(d), out var v) ? v : new Version(0, 0))
            .LastOrDefault();
        if (latestVersion is null)
        {
            return null;
        }

        var refRoot = Path.Combine(latestVersion, "ref");
        if (!Directory.Exists(refRoot))
        {
            return null;
        }

        return Directory.GetDirectories(refRoot).OrderBy(d => d).LastOrDefault() ?? refRoot;
    }
}

[TaskName("Package")]
[IsDependentOn(typeof(CheckApiCompatTask))]
public sealed class PackageTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.EnsureDirectoryExists("../Releases");
        context.CleanDirectory("../Releases");
        context.EnsureDirectoryExists($"../Releases/{context.Name}");
        context.CopyFiles(
            $"../{BuildContext.ProjectName}/bin/{context.BuildConfiguration}/Mods/mod/publish/*",
            $"../Releases/{context.Name}"
        );
        if (context.DirectoryExists($"../{BuildContext.ProjectName}/assets"))
        {
            context.CopyDirectory(
                $"../{BuildContext.ProjectName}/assets",
                $"../Releases/{context.Name}/assets"
            );
        }

        if (context.DirectoryExists($"../{BuildContext.ProjectName}/native"))
        {
            context.CopyDirectory(
                $"../{BuildContext.ProjectName}/native",
                $"../Releases/{context.Name}/native"
            );

            // Native debug symbols add 62 MB that nothing loads at runtime. The csproj
            // Exclude covers the publish output only; this folder is copied from source.
            context.DeleteFiles($"../Releases/{context.Name}/native/**/*.pdb");
        }

        context.CopyFile(
            $"../{BuildContext.ProjectName}/modinfo.json",
            $"../Releases/{context.Name}/modinfo.json"
        );
        if (context.FileExists($"../{BuildContext.ProjectName}/modicon.png"))
        {
            context.CopyFile(
                $"../{BuildContext.ProjectName}/modicon.png",
                $"../Releases/{context.Name}/modicon.png"
            );
        }

        context.Zip(
            $"../Releases/{context.Name}",
            $"../Releases/{context.Name}_{context.Version}.zip"
        );
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(PackageTask))]
public class DefaultTask : FrostingTask
{
}
