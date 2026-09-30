// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Arca.Architecture.Tests;

/// <summary>The version of the application is defined once (versio-de-l-aplicacio, Una sola fuente de la versión), and every place that shows it or names a package with it reads that definition.</summary>
public sealed class VersionSourceTests
{
    const string Spec = "versio-visible/versio-de-l-aplicacio: Una sola fuente de la versión";

    static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found");
    }

    static string Version()
    {
        var props = XDocument.Load(Path.Combine(Root(), "Directory.Build.props"));
        return props.Descendants("Version").Single().Value;
    }

    static IEnumerable<string> Sources(params string[] extensions)
    {
        var root = Root();
        foreach (var folder in new[] { "src", "tools", "build" })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, folder), "*", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || !extensions.Contains(Path.GetExtension(file)))
                {
                    continue;
                }

                yield return file;
            }
        }
    }

    [Fact]
    [Trait("spec", Spec + " (Cambiar la versión)")]
    public void The_version_is_defined_once_in_the_props_file_and_in_no_project_file()
    {
        Assert.Matches(new Regex(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.]+)?$"), Version()); // MAJOR.MINOR.PATCH with an optional suffix

        var root = Root();
        var projects = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "tools"), "*.csproj", SearchOption.AllDirectories));
        Assert.All(projects, p =>
        {
            var document = XDocument.Load(p);
            Assert.Empty(document.Descendants("Version"));
            Assert.Empty(document.Descendants("AssemblyVersion"));
            Assert.Empty(document.Descendants("InformationalVersion"));
        });
    }

    [Fact]
    [Trait("spec", Spec + " (Cambiar la versión)")]
    public void The_number_of_the_version_is_written_nowhere_else_in_the_code_the_texts_or_the_scripts()
    {
        var version = Version();

        var writes = Sources(".cs", ".resx", ".axaml", ".xaml", ".sh", ".ps1", ".json", ".props", ".plist", ".txt")
            .Where(f => File.ReadAllText(f).Contains(version, StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(Root(), f))
            .ToList();

        Assert.Empty(writes);
    }

    [Fact]
    [Trait("spec", Spec + " (Coherencia con el paquete)")]
    public void The_packages_take_their_version_from_the_props_file_the_application_is_built_with()
    {
        var script = File.ReadAllText(Path.Combine(Root(), "build", "package.sh"));

        Assert.Contains("Directory.Build.props", script, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"version=""\$\(sed -n 's:\.\*<Version>"), script); // read from the tag, never typed
        Assert.Contains("ARCA-${version}-${rid}", script, StringComparison.Ordinal); // the name of the package carries it
        Assert.Contains("<key>CFBundleShortVersionString</key><string>${version}</string>", script, StringComparison.Ordinal);
    }
}
