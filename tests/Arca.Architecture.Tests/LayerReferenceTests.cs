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
        ["Microsoft.EntityFrameworkCore", "Microsoft.Data.Sqlite", "SQLite", "SQLitePCLRaw", "Avalonia"];

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
