using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace Gui.Tests.Compat;

/// <summary>
///     Approval test over the whole public API surface of <c>Gui.dll</c>. The surface is the
///     binary contract with dependent mods: an added, removed, retyped or re-defaulted public
///     or protected member fails the test and forces a deliberate update of the baseline file
///     <c>compat/PublicAPI.approved.txt</c>.
///     <para>
///         The binary ApiCompat gate (<c>compat/baseline/Gui.dll</c>) only runs inside the
///         Cake packaging pipeline, so a break is otherwise invisible to `dotnet build` and
///         `dotnet test`. This test brings the same discipline into the normal test run.
///         Additions are visible here too, unlike the binary gate.
///     </para>
/// </summary>
[TestFixture]
public class PublicApiApprovalTests
{
    private const string ApprovedFileName = "PublicAPI.approved.txt";
    private const string ActualFileName = "PublicAPI.actual.txt";

    private static readonly Assembly GuiAssembly = typeof(Gui.Widgets.Framework.Widget).Assembly;

    [Test]
    public void PublicApi_MatchesApprovedBaseline()
    {
        var compatDir = FindCompatDir();
        var approvedPath = Path.Combine(compatDir, ApprovedFileName);
        var actual = DumpPublicApi();

        if (!File.Exists(approvedPath))
        {
            File.WriteAllText(approvedPath, actual);
            Assert.Fail(
                "No public-API baseline existed. " + approvedPath + " was created from the current " +
                "assembly. Review it, commit it, and re-run.");
            return;
        }

        var approved = File.ReadAllText(approvedPath);
        var actualPath = Path.Combine(compatDir, ActualFileName);
        if (approved == actual)
        {
            if (File.Exists(actualPath))
            {
                File.Delete(actualPath);
            }

            return;
        }

        File.WriteAllText(actualPath, actual);
        Assert.Fail(
            "The public API surface changed: compat/PublicAPI.approved.txt (old) vs " +
            "compat/PublicAPI.actual.txt (new). Removed or retyped entries break mods " +
            "compiled against the previous release (compat/API-POLICY.md). If the change is " +
            "intentional, review the diff and copy PublicAPI.actual.txt over the approved " +
            "file.\n" + DescribeDiff(approved, actual));
    }

    /// <summary>
    ///     The 3.x constructor signatures that mods compiled against. Optional-argument
    ///     defaults are baked into the callers, so the parameter lists are binary contracts.
    ///     Deleting one of these overloads is a release-blocking regression, not a refactor.
    /// </summary>
    [Test]
    public void FrozenCtors_FromReleasedVersions_StillBind()
    {
        AssertCtor(
            "Gui.Widgets.Basic.WindowFrame",
            "restore the 3.x forwarding overload instead of deleting it",
            typeof(string),
            typeof(Gui.Widgets.Framework.Widget),
            typeof(Action),
            typeof(float),
            typeof(OpenTK.Mathematics.Vector4?),
            typeof(OpenTK.Mathematics.Vector4?),
            typeof(float),
            typeof(bool),
            typeof(bool),
            typeof(bool),
            typeof(Gui.Widgets.Framework.Key));

        AssertCtor(
            "Gui.Widgets.Basic.WindowTitleBar",
            "restore the 3.x forwarding overload instead of deleting it",
            typeof(string),
            typeof(float),
            typeof(Action),
            typeof(Action<bool>),
            typeof(OpenTK.Mathematics.Vector4?),
            typeof(OpenTK.Mathematics.Vector4?),
            typeof(OpenTK.Mathematics.Vector4?),
            typeof(OpenTK.Mathematics.Vector4?),
            typeof(float),
            typeof(bool),
            typeof(bool),
            typeof(Gui.Widgets.Framework.Key));

        AssertCtor(
            "Gui.Widgets.Framework.ThemeData",
            "restore the 3.x forwarding overload instead of deleting it",
            typeof(Gui.Widgets.Framework.ColorScheme?),
            typeof(Gui.Widgets.Framework.TextTheme?),
            typeof(Gui.Widgets.Framework.ProgressBarStyle?),
            typeof(Gui.Widgets.Inventory.ItemSlotStyle?),
            typeof(Gui.Widgets.Basic.HotkeyBadgeStyle?),
            typeof(Gui.Widgets.Framework.DropdownStyle?),
            typeof(Gui.Widgets.Framework.CheckboxStyle?),
            typeof(Gui.Widgets.Framework.RadioButtonStyle?),
            typeof(Gui.Widgets.Framework.SliderStyle?),
            typeof(Gui.Widgets.Framework.ButtonStyle?),
            typeof(Gui.Widgets.Framework.TextFieldStyle?),
            typeof(Gui.Widgets.Framework.IconButtonStyle?));

        AssertCtor(
            "Gui.Widgets.Inventory.ItemStackDisplay",
            "restore the pre-tooltip forwarding overload instead of deleting it",
            typeof(Vintagestory.API.Common.ItemStack),
            typeof(float?),
            typeof(float?),
            typeof(int),
            typeof(Gui.Widgets.Framework.Key));
    }

    /// <summary>
    ///     A suppression blesses a binary break, so it is legal only in a major bump over the
    ///     baseline. A non-empty suppression file on a minor or patch release means a break is
    ///     about to ship as an update that dependent mods load without recompiling. The
    ///     <c>ItemStackDisplay</c> hierarchy change sat in an unpublished release for a whole
    ///     cycle because nothing checked this.
    /// </summary>
    [Test]
    public void Suppressions_OnlyLegalInMajorBump()
    {
        var compatDir = FindCompatDir();
        var suppressions = File.ReadAllText(Path.Combine(compatDir, "ApiCompatSuppressions.xml"));
        var count = Regex.Matches(suppressions, "<Suppression>").Count;
        if (count == 0)
        {
            return;
        }

        var baseline = ReadVersion(Path.Combine(compatDir, "baseline", "version.txt"));
        if (baseline is null)
        {
            Assert.Ignore("No compat baseline version recorded yet.");
            return;
        }

        var modinfoPath = Path.GetFullPath(Path.Combine(compatDir, "..", "Gui", "modinfo.json"));
        var current = ReadVersion(modinfoPath, "\"version\"\\s*:\\s*\"([^\"]+)\"");
        Assert.That(current, Is.Not.Null, "Could not read a version from " + modinfoPath);
        Assert.That
        (
            current!.Major,
            Is.GreaterThan(baseline.Major),
            count + " ApiCompat suppression(s) are active while modinfo.json (" + current
            + ") is not a major bump over the baseline (" + baseline + "). Add a forwarding "
            + "shim (compat/API-POLICY.md, R4), or publish, refresh the baseline and clear "
            + "the suppression file."
        );
    }

    private static Version? ReadVersion
    (
        string path,
        string? pattern = null
    )
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var text = File.ReadAllText(path);
        if (pattern is not null)
        {
            var match = Regex.Match(text, pattern);
            if (!match.Success)
            {
                return null;
            }

            text = match.Groups[1].Value;
        }

        return Version.TryParse(text.Trim(), out var version) ? version : null;
    }

    private static void AssertCtor
    (
        string typeName,
        string hint,
        params Type[] parameters
    )
    {
        var type = GuiAssembly.GetType(typeName);
        Assert.That(type, Is.Not.Null, typeName + " disappeared from the public surface.");
        var ctor = type!.GetConstructor(parameters);
        Assert.That
        (
            ctor,
            Is.Not.Null,
            typeName + " no longer exposes the constructor ("
            + string.Join(", ", parameters.Select(p => p.ToString())) + "): " + hint
        );
    }

    private static string DescribeDiff
    (
        string approved,
        string actual
    )
    {
        var oldLines = approved.Split('\n').ToHashSet(StringComparer.Ordinal);
        var newLines = actual.Split('\n').ToHashSet(StringComparer.Ordinal);

        var sb = new StringBuilder();
        sb.AppendLine("Only in the approved baseline (removed or retyped):");
        foreach (var line in oldLines.Where(l => l.Length > 0 && !newLines.Contains(l))
                     .OrderBy(l => l, StringComparer.Ordinal).Take(20))
        {
            sb.AppendLine("  - " + line);
        }

        sb.AppendLine("Only in the current assembly (added):");
        foreach (var line in newLines.Where(l => l.Length > 0 && !oldLines.Contains(l))
                     .OrderBy(l => l, StringComparer.Ordinal).Take(20))
        {
            sb.AppendLine("  + " + line);
        }

        return sb.ToString();
    }

    private static string DumpPublicApi()
    {
        var lines = new List<string>();
        foreach (var type in GuiAssembly.GetTypes().Where(IsVisible)
                     .OrderBy(t => t.ToString(), StringComparer.Ordinal))
        {
            lines.Add(TypeLine(type));
            lines.AddRange(MembersOf(type).Select(m => "  " + m));
        }

        return string.Join("\n", lines) + "\n";
    }

    private static bool IsVisible
    (
        Type type
    )
    {
        if (type.IsDefined(typeof(CompilerGeneratedAttribute), false))
        {
            return false;
        }

        if (type.DeclaringType is { } parent)
        {
            return IsVisible(parent)
                   && (type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem);
        }

        return type.IsPublic;
    }

    private static string TypeLine
    (
        Type type
    )
    {
        var sb = new StringBuilder();
        sb.Append(Visibility(type));
        if (type.IsInterface)
        {
            sb.Append("interface ");
        }
        else if (type.IsAbstract && type.IsSealed)
        {
            sb.Append("static ");
        }
        else if (type.IsAbstract)
        {
            sb.Append("abstract ");
        }
        else if (type.IsSealed && !type.IsValueType)
        {
            sb.Append("sealed ");
        }

        if (!type.IsInterface)
        {
            sb.Append(type.IsEnum
                          ? "enum "
                          : type.IsValueType
                              ? "struct "
                              : "class ");
        }

        sb.Append(type.ToString());
        if (type.BaseType is { } baseType
            && baseType != typeof(object)
            && baseType != typeof(ValueType)
            && baseType != typeof(Enum))
        {
            sb.Append(" : ").Append(baseType.ToString());
        }

        foreach (var iface in type.GetInterfaces().OrderBy(i => i.ToString(), StringComparer.Ordinal))
        {
            sb.Append(" | ").Append(iface.ToString());
        }

        if (type.IsDefined(typeof(ObsoleteAttribute), false))
        {
            sb.Append(" [Obsolete]");
        }

        return sb.ToString();
    }

    private static string Visibility
    (
        Type type
    )
    {
        if (type.IsNestedFamily)
        {
            return "protected ";
        }

        if (type.IsNestedFamORAssem)
        {
            return "protected internal ";
        }

        return "public ";
    }

    private static IEnumerable<string> MembersOf
    (
        Type type
    )
    {
        const BindingFlags all =
            BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        var result = new List<string>();

        result.AddRange
        (
            type.GetConstructors
                    (
                        BindingFlags.Public | BindingFlags.NonPublic
                        | BindingFlags.Instance | BindingFlags.DeclaredOnly
                    )
                .Where(IsExternallyVisible)
                .Select(CtorLine)
        );

        foreach (var m in type.GetMethods(all)
                     .Where(m => IsExternallyVisible(m) && !IsAccessorOrGenerated(m)))
        {
            var line = new StringBuilder();
            line.Append(m.ReturnType.ToString()).Append(' ').Append(m.Name)
                .Append('(').Append(Params(m)).Append(')');
            AppendMemberFlags(line, m);
            result.Add(line.ToString());
        }

        foreach (var p in type.GetProperties(all))
        {
            var accessor = p.GetMethod ?? p.SetMethod;
            if (accessor is null || !IsExternallyVisible(accessor))
            {
                continue;
            }

            var accessors = new List<string>();
            if (p.GetMethod is { } getter)
            {
                accessors.Add(AccessorName(getter, "get"));
            }

            if (p.SetMethod is { } setter)
            {
                accessors.Add(AccessorName(setter, IsInit(setter) ? "init" : "set"));
            }

            var line = new StringBuilder();
            line.Append(p.PropertyType.ToString()).Append(' ').Append(p.Name)
                .Append(" { ").Append(string.Join("; ", accessors)).Append(" }");
            AppendMemberFlags(line, accessor);
            if (p.IsDefined(typeof(ObsoleteAttribute), false))
            {
                line.Append(" [Obsolete]");
            }

            result.Add(line.ToString());
        }

        foreach (var e in type.GetEvents(all))
        {
            if (e.AddMethod is not { } add || !IsExternallyVisible(add))
            {
                continue;
            }

            result.Add("event " + e.EventHandlerType + " " + e.Name);
        }

        foreach (var f in type.GetFields(all).Where(IsExternallyVisible))
        {
            var line = new StringBuilder();
            line.Append(f.FieldType.ToString()).Append(' ').Append(f.Name);
            if (f.IsLiteral)
            {
                line.Append(" const");
            }

            if (f.IsStatic && !f.IsLiteral)
            {
                line.Append(" static");
            }

            if (f.IsInitOnly)
            {
                line.Append(" readonly");
            }

            if (f.IsDefined(typeof(ObsoleteAttribute), false))
            {
                line.Append(" [Obsolete]");
            }

            result.Add(line.ToString());
        }

        return result.OrderBy(s => s, StringComparer.Ordinal);
    }

    private static bool IsExternallyVisible
    (
        MethodBase m
    )
    {
        return m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly;
    }

    private static bool IsExternallyVisible
    (
        FieldInfo f
    )
    {
        return f.IsPublic || f.IsFamily || f.IsFamilyOrAssembly;
    }

    private static bool IsAccessorOrGenerated
    (
        MethodInfo m
    )
    {
        if (m.IsDefined(typeof(CompilerGeneratedAttribute), false))
        {
            return true;
        }

        if (!m.IsSpecialName)
        {
            return false;
        }

        var name = m.Name;
        return name.StartsWith("get_")
               || name.StartsWith("set_")
               || name.StartsWith("add_")
               || name.StartsWith("remove_")
               || name.StartsWith("raise_")
               || name.StartsWith("__");
    }

    private static string CtorLine
    (
        ConstructorInfo c
    )
    {
        var line = new StringBuilder();
        line.Append(".ctor(").Append(Params(c)).Append(')');
        AppendMemberFlags(line, c);
        return line.ToString();
    }

    private static void AppendMemberFlags
    (
        StringBuilder line,
        MethodBase m
    )
    {
        if (m.IsStatic)
        {
            line.Append(" static");
        }

        if (m.IsAbstract)
        {
            line.Append(" abstract");
        }
        else if (m.IsVirtual)
        {
            line.Append(" virtual");
        }

        if (m.IsFinal && m.IsVirtual)
        {
            line.Append(" sealed");
        }

        if (m.IsDefined(typeof(ObsoleteAttribute), false))
        {
            line.Append(" [Obsolete]");
        }
    }

    private static string Params
    (
        MethodBase m
    )
    {
        return string.Join(", ", m.GetParameters().Select(FormatParam));
    }

    private static string FormatParam
    (
        ParameterInfo p
    )
    {
        var type = p.ParameterType;
        var prefix = "";
        if (type.IsByRef)
        {
            type = type.GetElementType()!;
            prefix = p.IsOut ? "out " : "ref ";
        }

        var line = prefix + type.ToString();
        if (p.HasDefaultValue)
        {
            var value = p.DefaultValue;
            line += " = " + (value switch
            {
                null => "null",
                string s => "\"" + s + "\"",
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null"
            });
        }

        return line;
    }

    private static string AccessorName
    (
        MethodInfo accessor,
        string kind
    )
    {
        if (accessor.IsPublic)
        {
            return kind;
        }

        if (accessor.IsFamily)
        {
            return "protected " + kind;
        }

        if (accessor.IsFamilyOrAssembly)
        {
            return "protected internal " + kind;
        }

        return kind;
    }

    private static bool IsInit
    (
        MethodInfo setter
    )
    {
        var modifiers = setter.ReturnParameter.GetRequiredCustomModifiers()
            .Concat(setter.GetParameters().SelectMany(p => p.GetRequiredCustomModifiers()));
        return modifiers.Any(t => t.ToString()!.Contains("IsExternalInit"));
    }

    private static string FindCompatDir()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Gui", "compat");
            if (File.Exists(Path.Combine(candidate, "ApiCompatSuppressions.xml")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate Gui/compat starting from " + AppContext.BaseDirectory);
    }
}
