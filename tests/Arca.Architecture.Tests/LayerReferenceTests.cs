// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Reflection;
using System.Xml.Linq;
using Xunit;

namespace Arca.Architecture.Tests;

/// <summary>
/// Layer rules of arquitectura-base D1: dependencies point inwards.
/// Checked twice: on the project files (declared references) and on the compiled assemblies (real usage).
/// </summary>
public sealed class LayerReferenceTests
{
    static readonly string[] _forbiddenInInnerLayers =
        ["Microsoft.EntityFrameworkCore", "Microsoft.Data.Sqlite", "SQLite", "SQLitePCLRaw", "Avalonia", "NSec"];

    public static TheoryData<string, string[]> AllowedProjectReferences => new()
    {
        { "Arca.Domain", [] },
        { "Arca.Application", ["Arca.Domain"] },
        { "Arca.Infrastructure", ["Arca.Application"] },
        { "Arca.UI", ["Arca.Application"] },
        { "Arca.Desktop", ["Arca.Application", "Arca.Infrastructure", "Arca.UI"] },
    };

    [Theory]
    [MemberData(nameof(AllowedProjectReferences))]
    public void Project_references_only_allowed_layers(string project, string[] allowed)
    {
        var actual = ReferencedProjects(project);
        Assert.Empty(actual.Except(allowed));
    }

    [Theory]
    [InlineData("Arca.Domain")]
    [InlineData("Arca.Application")]
    public void Inner_layers_have_no_infrastructure_or_ui_packages(string project)
    {
        var packages = ProjectFile(project).Descendants("PackageReference")
            .Select(e => (string?)e.Attribute("Include") ?? "")
            .Where(name => _forbiddenInInnerLayers.Any(f => name.StartsWith(f, StringComparison.OrdinalIgnoreCase)));
        Assert.Empty(packages);
    }

    [Theory]
    [Trait("spec", "acces-i-xifrat/xifrat-de-la-base: Ningún secreto en el código (las capas internas no conocen el cifrado)")]
    [Trait("spec", "taquilles-i-zones/design: D4 y D5 (Domain y Application no referencian EF Core)")]
    [InlineData(typeof(Domain.AssemblyMarker))]
    [InlineData(typeof(Application.AssemblyMarker))]
    public void Inner_layers_do_not_reference_EF_Core_the_cryptography_or_the_encrypted_database(Type marker)
    {
        var referenced = marker.Assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty);

        Assert.DoesNotContain(referenced, n => n.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("NSec", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("SQLite", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("Microsoft.Data.Sqlite", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Domain_has_no_package_references()
    {
        Assert.Empty(ProjectFile("Arca.Domain").Descendants("PackageReference"));
    }

    [Theory]
    [InlineData(typeof(Domain.AssemblyMarker), new string[0])]
    [InlineData(typeof(Application.AssemblyMarker), new[] { "Arca.Domain" })]
    [InlineData(typeof(Infrastructure.AssemblyMarker), new[] { "Arca.Application", "Arca.Domain" })]
    [InlineData(typeof(UI.AssemblyMarker), new[] { "Arca.Application", "Arca.Domain" })]
    public void Compiled_assemblies_reference_only_allowed_arca_assemblies(Type marker, string[] allowed)
    {
        var arca = marker.Assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? "")
            .Where(n => n.StartsWith("Arca.", StringComparison.Ordinal));
        Assert.Empty(arca.Except(allowed));
    }

    [Fact]
    [Trait("spec", "ux-fonaments/design: D1 Biblioteca de componentes separada de las pantallas")]
    public void The_component_library_uses_neither_the_domain_model_nor_infrastructure()
    {
        // Result, Error and Notice live in Domain.Common because every layer shares them; nothing else of the domain
        // (entities, rules, repositories) may show up in a component, and infrastructure never does.
        var offending = Directory
            .EnumerateFiles(Path.Combine(RepositoryRoot(), "src", "Arca.UI"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .SelectMany(f => File.ReadLines(f).Select(line => (File: Path.GetFileName(f), Line: line.Trim())))
            .Where(x => x.Line.StartsWith("using Arca.", StringComparison.Ordinal)
                && (x.Line.StartsWith("using Arca.Infrastructure", StringComparison.Ordinal)
                    || (x.Line.StartsWith("using Arca.Domain", StringComparison.Ordinal) && x.Line != "using Arca.Domain.Common;")))
            .Select(x => x.File + ": " + x.Line)
            .ToList();

        Assert.Empty(offending);
        Assert.DoesNotContain("Arca.Infrastructure", ReferencedProjects("Arca.UI"));
        Assert.DoesNotContain("Arca.Domain", ReferencedProjects("Arca.UI"));
    }

    [Fact]
    [Trait("spec", "pantalles-de-domini/design: Riesgos, las reglas se filtran a las vistas")]
    public void The_view_models_of_the_domain_screens_reach_the_data_only_through_application()
    {
        // The screens live in Arca.UI/Screens: the model of a screen may know Result and Error, and nothing else of the domain.
        var screens = Path.Combine(RepositoryRoot(), "src", "Arca.UI", "Screens");
        var offending = Directory.EnumerateFiles(screens, "*.cs", SearchOption.AllDirectories)
            .SelectMany(f => File.ReadLines(f).Select(line => (File: Path.GetFileName(f), Line: line.Trim())))
            .Where(x => x.Line.StartsWith("using Arca.Domain", StringComparison.Ordinal) && x.Line != "using Arca.Domain.Common;"
                || x.Line.StartsWith("using Arca.Infrastructure", StringComparison.Ordinal))
            .Select(x => x.File + ": " + x.Line)
            .ToList();

        Assert.Empty(offending);
    }

    static string[] ReferencedProjects(string project) =>
        ProjectFile(project).Descendants("ProjectReference")
            .Select(e => Path.GetFileNameWithoutExtension(((string?)e.Attribute("Include") ?? "").Replace('\\', '/')))
            .ToArray();

    static XDocument ProjectFile(string project) =>
        XDocument.Load(Path.Combine(RepositoryRoot(), "src", project, project + ".csproj"));

    static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found");
    }
}
