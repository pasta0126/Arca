// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Xunit;

namespace Arca.Architecture.Tests;

/// <summary>Copies and restorations stay inside the computer and behind the service of Application (copies-de-seguretat D8, D11).</summary>
public sealed class BackupIsolationTests
{
    static readonly string[] _network = ["System.Net", "HttpClient", "WebClient", "Socket", "WebRequest"];

    static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found");
    }

    static IEnumerable<(string File, string Text)> Sources(string folder) =>
        Directory.EnumerateFiles(Path.Combine(Root(), "src", folder), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(f => (f, File.ReadAllText(f)));

    [Fact]
    [Trait("spec", "copies-de-seguretat/copia-de-seguretat: Sin red (Sin conexión)")]
    [Trait("spec", "copies-de-seguretat/restauracio: Sin red (Sin conexión)")]
    public void Nothing_that_makes_or_restores_a_backup_uses_the_network()
    {
        var code = Sources("Arca.Application/Backup").Concat(Sources("Arca.Infrastructure/Backup")).Concat(Sources("Arca.UI/Backup"));

        var offenders = code.Where(s => _network.Any(n => s.Text.Contains(n, StringComparison.Ordinal))).Select(s => Path.GetFileName(s.File));

        Assert.Empty(offenders);
    }

    [Fact]
    [Trait("spec", "copies-de-seguretat/design: D11 (la interfaz no conoce Infrastructure ni los ficheros de la base)")]
    public void The_application_and_the_screens_reach_backups_only_through_the_service()
    {
        var application = Sources("Arca.Application/Backup").Where(s => s.Text.Contains("System.IO", StringComparison.Ordinal)).Select(s => Path.GetFileName(s.File));
        var screens = Sources("Arca.UI/Backup").Where(s => s.Text.Contains("Arca.Infrastructure", StringComparison.Ordinal)).Select(s => Path.GetFileName(s.File));

        Assert.Empty(application);
        Assert.Empty(screens);
    }
}
