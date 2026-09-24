// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Security.Cryptography;
using Arca.Application.Storage;
using Arca.Infrastructure.Storage;
using Arca.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Arca.Infrastructure.Tests.Storage;

public sealed class SchemaMigratorTests
{
    const string Spec = "arquitectura-base/emmagatzematge-local";

    static readonly DateTimeOffset _start = new(2026, 9, 24, 10, 0, 0, TimeSpan.Zero);

    static byte[] Hash(string path) => SHA256.HashData(File.ReadAllBytes(path));

    static SchemaMigrator Migrator(Func<Microsoft.EntityFrameworkCore.DbContext> context,
        Func<string, DatabaseKey, CancellationToken, Task<bool>>? verify = null) =>
        new(context, verify, new SteppingTime(_start));

    /// <summary>Creates a database at the given schema by applying that context's migrations to a new file.</summary>
    static async Task<string> NewDatabaseAsync(TempDirectory dir, DatabaseKey key, Func<string, DatabaseKey, bool, Microsoft.EntityFrameworkCore.DbContext> context)
    {
        var path = dir.File("arca.db");
        await new SchemaMigrator(() => context(path, key, true)).ApplyToNewDatabaseAsync();
        SqliteConnection.ClearAllPools();
        return path;
    }

    static async Task<List<string>> ScalarsAsync(string path, DatabaseKey key, string sql)
    {
        await using var context = new ArcaDbContext(path, key);
        return await context.Database.SqlQueryRaw<string>(sql).ToListAsync();
    }

    static Task<List<string>> HistoryAsync(string path, DatabaseKey key) =>
        ScalarsAsync(path, key, "SELECT MigrationId AS Value FROM __EFMigrationsHistory ORDER BY MigrationId");

    static Task<List<string>> TablesAsync(string path, DatabaseKey key) =>
        ScalarsAsync(path, key, "SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name IN ('A','B','C')");

    [Fact]
    [Trait("spec", Spec + ": Migraciones de esquema seguras (esquema ya actualizado)")]
    public async Task Up_to_date_schema_makes_no_copy_and_applies_nothing()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("uptodate");
        var path = await NewDatabaseAsync(dir, key, (p, k, c) => new V2Context(p, k, c));
        var before = Hash(path);

        var result = await Migrator(() => new V2Context(path, key)).MigrateAsync(path, key);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.AppliedCount);
        Assert.Empty(SchemaMigrator.CopiesOf(path));
        Assert.Equal(before, Hash(path));
    }

    [Fact]
    [Trait("spec", Spec + ": Migraciones de esquema seguras (copia verificada antes de migrar)")]
    public async Task Older_file_is_copied_verified_and_then_migrated()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("pending");
        var path = await NewDatabaseAsync(dir, key, (p, k, c) => new V1Context(p, k, c));
        var original = Hash(path);

        var result = await Migrator(() => new V2Context(path, key)).MigrateAsync(path, key);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.AppliedCount);
        var copy = Assert.Single(SchemaMigrator.CopiesOf(path));
        Assert.Equal(copy, result.Value.BackupPath);
        Assert.Equal(original, Hash(copy)); // the copy is the file as it was before migrating
        Assert.Equal(["001_CreateA", "002_CreateB"], await HistoryAsync(path, key));
        Assert.Equal(["A", "B"], (await TablesAsync(path, key)).Order().ToList());
    }

    [Fact]
    [Trait("spec", Spec + ": Migraciones de esquema seguras (migración atómica)")]
    public async Task Failed_migration_leaves_the_database_exactly_as_it_was()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("broken");
        var path = await NewDatabaseAsync(dir, key, (p, k, c) => new V1Context(p, k, c));
        var before = Hash(path);

        var result = await Migrator(() => new BrokenContext(path, key)).MigrateAsync(path, key);

        Assert.Equal("Storage.MigrationFailed", result.Error!.Code);
        Assert.Equal(before, Hash(path));
        Assert.Equal(["001_CreateA"], await HistoryAsync(path, key));
        Assert.Equal(["A"], await TablesAsync(path, key)); // the half-done table C was rolled back
    }

    [Fact]
    [Trait("spec", Spec + ": Migraciones de esquema seguras (copia previa corrupta)")]
    public async Task Corrupt_copy_blocks_the_migration_and_leaves_the_original_intact()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("badcopy");
        var path = await NewDatabaseAsync(dir, key, (p, k, c) => new V1Context(p, k, c));
        var before = Hash(path);

        var result = await Migrator(() => new V2Context(path, key), verify: (_, _, _) => Task.FromResult(false))
            .MigrateAsync(path, key);

        Assert.Equal("Storage.BackupFailed", result.Error!.Code);
        Assert.Equal(before, Hash(path));
        Assert.Empty(SchemaMigrator.CopiesOf(path)); // the bad copy is not left behind
        Assert.Equal(["001_CreateA"], await HistoryAsync(path, key));
    }

    [Fact]
    [Trait("spec", Spec + ": Migraciones de esquema seguras (copia previa corrupta)")]
    public async Task A_copy_that_throws_while_being_checked_also_blocks_the_migration()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("throwing");
        var path = await NewDatabaseAsync(dir, key, (p, k, c) => new V1Context(p, k, c));
        var before = Hash(path);

        var result = await Migrator(() => new V2Context(path, key), verify: (_, _, _) => throw new IOException("disk"))
            .MigrateAsync(path, key);

        Assert.Equal("Storage.BackupFailed", result.Error!.Code);
        Assert.Equal(before, Hash(path));
    }

    [Fact]
    [Trait("spec", Spec + ": Rechazo de bases de datos de versión más nueva")]
    public async Task File_from_a_newer_version_is_refused_and_not_touched()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("newer");
        var path = await NewDatabaseAsync(dir, key, (p, k, c) => new V2Context(p, k, c));
        var before = Hash(path);

        var result = await Migrator(() => new V1Context(path, key)).MigrateAsync(path, key);

        Assert.Equal("Storage.SchemaNewer", result.Error!.Code);
        Assert.Equal(before, Hash(path));
        Assert.Empty(SchemaMigrator.CopiesOf(path));
    }

    static void PlaceOldCopies(string path)
    {
        foreach (var day in new[] { "01", "02", "03" })
        {
            File.WriteAllText($"{path}.premigration-202001{day}T000000000Z.bak", "old copy " + day);
        }
    }

    [Fact]
    [Trait("spec", Spec + ": Retención acotada de copias previas a migración (cuarta migración)")]
    public async Task Fourth_migration_keeps_the_three_newest_copies()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("retention");
        var path = await NewDatabaseAsync(dir, key, (p, k, c) => new V1Context(p, k, c));
        PlaceOldCopies(path);

        var result = await Migrator(() => new V2Context(path, key)).MigrateAsync(path, key);

        Assert.True(result.IsSuccess);
        var kept = SchemaMigrator.CopiesOf(path).Select(Path.GetFileName).ToList();
        Assert.Equal(SchemaMigrator.CopiesToKeep, kept.Count);
        Assert.DoesNotContain(kept, name => name!.Contains("20200101", StringComparison.Ordinal)); // the oldest is gone
        Assert.Contains(kept, name => name!.Contains("20200103", StringComparison.Ordinal));
        Assert.Contains(result.Value!.BackupPath!, SchemaMigrator.CopiesOf(path));
    }

    [Fact]
    [Trait("spec", Spec + ": Retención acotada de copias previas a migración (migración fallida)")]
    public async Task Failed_migration_deletes_no_existing_copy()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("keepall");
        var path = await NewDatabaseAsync(dir, key, (p, k, c) => new V1Context(p, k, c));
        PlaceOldCopies(path);

        var result = await Migrator(() => new BrokenContext(path, key)).MigrateAsync(path, key);

        Assert.False(result.IsSuccess);
        var kept = SchemaMigrator.CopiesOf(path).Select(Path.GetFileName).ToList();
        foreach (var day in new[] { "01", "02", "03" })
        {
            Assert.Contains(kept, name => name!.Contains("202001" + day, StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task Copies_are_only_those_of_this_database()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("names");
        var path = await NewDatabaseAsync(dir, key, (p, k, c) => new V1Context(p, k, c));
        await File.WriteAllTextAsync(dir.File("other.db.premigration-20200101T000000000Z.bak"), "x");

        Assert.Empty(SchemaMigrator.CopiesOf(path));
    }

    [Fact]
    [Trait("spec", "arquitectura-base/design: D5 Migraciones de EF Core envueltas en un migrador propio")]
    public void The_real_model_has_no_changes_without_a_migration()
    {
        using var key = TestKeys.FromSeed("model");
        using var context = new ArcaDbContext(Path.Combine(Path.GetTempPath(), "never-opened.db"), key);

        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    [Trait("spec", "arquitectura-base/design: D5 Migraciones de EF Core envueltas en un migrador propio")]
    public async Task New_database_is_built_by_the_migrations_without_a_copy()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("fresh");
        var path = dir.File("arca.db");

        var created = await ArcaDatabase.CreateAsync(path, key);
        await created.Value!.DisposeAsync();
        SqliteConnection.ClearAllPools();

        Assert.Contains(await HistoryAsync(path, key), id => id.EndsWith("_InitialCreate", StringComparison.Ordinal));
        Assert.Empty(SchemaMigrator.CopiesOf(path));
    }
}
