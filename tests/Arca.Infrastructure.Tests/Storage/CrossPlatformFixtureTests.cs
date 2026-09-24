// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Infrastructure.Storage;
using Arca.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Arca.Infrastructure.Tests.Storage;

/// <summary>
/// A database file created once (on macOS) and versioned in the repository, opened by every test run.
/// Running it on another system proves the file is portable between systems (verified before production on Windows).
/// </summary>
public sealed class CrossPlatformFixtureTests
{
    const string FixtureName = "sample.arcafixture";
    const string Unicode = "Núria Çaragol l·l Nyerro";

    [Fact]
    [Trait("spec", "arquitectura-base/emmagatzematge-local: Fichero transportable entre equipos y sistemas operativos")]
    public async Task A_versioned_sample_file_opens_and_shows_the_same_data()
    {
        // Copy first: opening must never alter the versioned file.
        using var dir = new TempDirectory();
        var copy = dir.File("copy.db");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", FixtureName), copy);
        using var key = TestKeys.Fixture();

        var opened = await ArcaDatabase.OpenAsync(copy, key);

        Assert.True(opened.IsSuccess, opened.Error?.Code);
        await using var context = opened.Value!;
        var values = await context.Database.SqlQueryRaw<string>("SELECT Value AS Value FROM Probe").ToListAsync();
        Assert.Equal(Unicode, Assert.Single(values));
    }

    [Fact]
    [Trait("spec", "arquitectura-base/emmagatzematge-local: Migraciones de esquema seguras (copia verificada antes de migrar)")]
    public async Task The_old_sample_file_is_migrated_on_open_after_a_verified_copy()
    {
        // The sample predates the first migration, so opening it exercises the real upgrade path.
        using var dir = new TempDirectory();
        var copy = dir.File("copy.db");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", FixtureName), copy);
        using var key = TestKeys.Fixture();

        var opened = await ArcaDatabase.OpenAsync(copy, key);

        Assert.True(opened.IsSuccess, opened.Error?.Code);
        await using var context = opened.Value!;
        var history = await context.Database.SqlQueryRaw<string>("SELECT MigrationId AS Value FROM __EFMigrationsHistory").ToListAsync();
        Assert.Contains(history, id => id.EndsWith("_InitialCreate", StringComparison.Ordinal));
        Assert.Single(Arca.Infrastructure.Storage.SchemaMigrator.CopiesOf(copy));
    }

    [Fact]
    public async Task Regenerate_the_sample_file_when_asked()
    {
        Assert.SkipUnless(
            Environment.GetEnvironmentVariable("ARCA_REGENERATE_FIXTURE") == "1",
            "Set ARCA_REGENERATE_FIXTURE=1 to recreate the versioned sample file");

        var target = Path.Combine(RepositoryRoot(), "tests", "Arca.Infrastructure.Tests", "Fixtures", FixtureName);
        File.Delete(target);
        using var key = TestKeys.Fixture();
        var created = await ArcaDatabase.CreateAsync(target, key);
        await using var context = created.Value!;
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE Probe (Value TEXT NOT NULL)");
        await context.Database.ExecuteSqlRawAsync("INSERT INTO Probe(Value) VALUES ({0})", Unicode);
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
}
