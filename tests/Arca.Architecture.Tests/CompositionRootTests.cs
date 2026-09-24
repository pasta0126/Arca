// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.RegularExpressions;
using Xunit;

namespace Arca.Architecture.Tests;

/// <summary>Arca.Desktop knows Infrastructure only in its composition root (arquitectura-base, D1).</summary>
public sealed partial class CompositionRootTests
{
    [GeneratedRegex(@"Arca\.Infrastructure")]
    private static partial Regex InfrastructureUse();

    static string DesktopFolder()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
        {
            dir = dir.Parent;
        }

        return Path.Combine(dir!.FullName, "src", "Arca.Desktop");
    }

    [Fact]
    public void Only_the_composition_folder_uses_infrastructure()
    {
        var desktop = DesktopFolder();
        var offenders = Directory.EnumerateFiles(desktop, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Where(f => !Path.GetRelativePath(desktop, f).StartsWith("Composition" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Where(f => InfrastructureUse().IsMatch(File.ReadAllText(f)))
            .Select(f => Path.GetRelativePath(desktop, f))
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void The_composition_folder_does_use_infrastructure()
    {
        // Guards the guard: if the composition root stopped referencing Infrastructure, the rule above would mean nothing.
        var composition = Path.Combine(DesktopFolder(), "Composition");

        Assert.Contains(
            Directory.EnumerateFiles(composition, "*.cs"),
            f => InfrastructureUse().IsMatch(File.ReadAllText(f)));
    }
}
