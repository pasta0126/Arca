// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Xunit;

namespace Arca.Application.Tests.Localization;

/// <summary>
/// The safety net of the i18n design: every code declared in code has its Catalan text, and every key written
/// literally in the sources exists. A missing key fails here instead of showing up on a user's screen.
/// </summary>
public sealed partial class ResourceCoverageTests
{
    const string Spec = "arquitectura-base/internacionalitzacio";

    static readonly Assembly[] _assemblies = [typeof(ResourceSource).Assembly, typeof(Error).Assembly];

    [GeneratedRegex(@"^[A-Z][A-Za-z]+\.[A-Z][A-Za-z]+$")]
    private static partial Regex CodeShape();

    [GeneratedRegex(@"\.Get\(\s*""([^""]+)""")]
    private static partial Regex GetCall();

    /// <summary>Any string literal shaped like a resource key: Capability.Label|Stage|Result|Empty|Loading.Name.</summary>
    [GeneratedRegex(@"""([A-Z][A-Za-z]+\.(?:Label|Stage|Result|Empty|Loading)\.[A-Za-z]+)""")]
    private static partial Regex KeyLiteral();

    [GeneratedRegex(@"new Notice\(\s*""([^""]+)""")]
    private static partial Regex NoticeCall();

    static object? Dummy(Type type) =>
        type == typeof(string) ? "x"
        : type == typeof(int) ? 1
        : type == typeof(long) ? 1L
        : type == typeof(Guid) ? Guid.NewGuid()
        : type.IsValueType ? Activator.CreateInstance(type)
        : null;

    /// <summary>Every Error declared by a public static XxxErrors class, as fields or as factory methods.</summary>
    static List<Error> DeclaredErrors()
    {
        var errors = new List<Error>();
        var types = _assemblies.SelectMany(a => a.GetExportedTypes())
            .Where(t => t.IsAbstract && t.IsSealed && t.Name.EndsWith("Errors", StringComparison.Ordinal));
        foreach (var type in types)
        {
            errors.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(Error)).Select(f => (Error)f.GetValue(null)!));
            errors.AddRange(type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.ReturnType == typeof(Error))
                .Select(m => (Error)m.Invoke(null, [.. m.GetParameters().Select(p => Dummy(p.ParameterType))])!));
        }

        return errors;
    }

    static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found");
    }

    static IEnumerable<string> SourceFiles() => Directory
        .EnumerateFiles(Path.Combine(RepositoryRoot(), "src"), "*.cs", SearchOption.AllDirectories)
        .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal));

    [Fact]
    public void Declared_errors_were_found()
    {
        // Guards the guard: if reflection found nothing, the coverage tests below would pass for nothing.
        Assert.True(DeclaredErrors().Count >= 10);
    }

    [Fact]
    [Trait("spec", Spec + ": Detección de claves faltantes (clave inexistente en verificación)")]
    public void Every_declared_error_code_has_a_catalan_message()
    {
        var localizer = new ResxLocalizer();

        var missing = DeclaredErrors()
            .Select(ResourceKeys.For)
            .Where(key => localizer.Get(key) == key || string.IsNullOrWhiteSpace(localizer.Get(key)))
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void Every_declared_error_code_is_capability_dot_name_and_unique()
    {
        var codes = DeclaredErrors().Select(e => e.Code).ToList();

        Assert.All(codes, code => Assert.Matches(CodeShape(), code));
        Assert.Equal(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    [Trait("spec", Spec + ": Detección de claves faltantes (clave inexistente en verificación)")]
    public void Every_key_written_in_the_sources_exists_in_the_base_language()
    {
        var localizer = new ResxLocalizer();
        var used = new List<string>();
        foreach (var file in SourceFiles())
        {
            var text = File.ReadAllText(file);
            used.AddRange(GetCall().Matches(text).Select(m => m.Groups[1].Value));
            used.AddRange(KeyLiteral().Matches(text).Select(m => m.Groups[1].Value));
            used.AddRange(NoticeCall().Matches(text).Select(m => ResourceKeys.For(new Notice(m.Groups[1].Value))));
        }

        var missing = used.Where(key => localizer.Get(key) == key).Distinct(StringComparer.Ordinal).ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void The_scanner_finds_literal_keys()
    {
        var sample = "x.Get(\"Lockers.Label.Number\"); new Notice(\"Assignments.PriorDebt\", [1]); new StartupStage(\"Startup.Stage.Key\");";

        Assert.Equal("Lockers.Label.Number", GetCall().Match(sample).Groups[1].Value);
        Assert.Equal("Assignments.PriorDebt", NoticeCall().Match(sample).Groups[1].Value);
        Assert.Contains("Startup.Stage.Key", KeyLiteral().Matches(sample).Select(m => m.Groups[1].Value));
    }

    [Fact]
    public void No_resource_key_is_a_duplicate_or_empty_in_the_base_files()
    {
        var files = Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), "src", "Arca.Application", "Resources"), "*.resx")
            .Where(f => !Path.GetFileNameWithoutExtension(f).Contains('.', StringComparison.Ordinal)); // neutral (Catalan) files only

        foreach (var file in files)
        {
            var doc = System.Xml.Linq.XDocument.Load(file);
            var entries = doc.Descendants("data")
                .Select(d => (Name: (string)d.Attribute("name")!, Value: d.Element("value")?.Value ?? string.Empty))
                .ToList();
            Assert.All(entries, e => Assert.False(string.IsNullOrWhiteSpace(e.Value), $"{file}: {e.Name} is empty"));
            Assert.Equal(entries.Count, entries.Select(e => e.Name).Distinct(StringComparer.Ordinal).Count());
            Assert.All(entries, e => Assert.Equal(Path.GetFileNameWithoutExtension(file), ResourceKeys.CapabilityOf(e.Name)));
        }
    }
}
