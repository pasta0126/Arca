// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Security.Cryptography;
using Arca.Application.Backup;
using Arca.Application.Security;
using Arca.Application.Storage;
using Arca.Infrastructure.Backup;
using Arca.Infrastructure.Security;
using Arca.Infrastructure.Storage;
using Arca.Infrastructure.Tests.Storage;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Arca.Infrastructure.Tests.Backup;

/// <summary>Restoring a backup over the data of the centre (copies-de-seguretat, restauracio), over real encrypted files.</summary>
public sealed class BackupRestoreTests : IDisposable
{
    const string Spec = "copies-de-seguretat/restauracio";
    const string Password = "riu cadira blau gos";
    const string Newer = "gat ratllat sota pluja";

    static readonly Argon2Parameters _cost = new(1024, 1, 1);

    readonly TempDirectory _dir = new();

    sealed class TickingTime : TimeProvider
    {
        DateTimeOffset _now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now = _now.AddSeconds(1);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        _dir.Dispose();
    }

    static AccessService Service() => new(new NSecKeyCrypto(), new FileKeyFileStore(), _cost);

    static byte[] Hash(string path) => SHA256.HashData(File.ReadAllBytes(path));

    static async Task<DatabaseKey> CreateAsync(string path, string password, string marker)
    {
        var access = Service().CreateAccess(password, password).Value!;
        var groups = access.Challenge.Indices.Select(i => RecoveryKey.Groups(access.RecoveryKey)[i]).ToArray();
        Assert.True((await DatabaseCreator.CreateAsync(path, access, groups)).IsSuccess);
        await using var context = new ArcaDbContext(path, access.DataKey);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE Probe (Value TEXT NOT NULL)");
        await context.Database.ExecuteSqlRawAsync("INSERT INTO Probe(Value) VALUES ({0})", marker);
        return access.DataKey;
    }

    static async Task<string> MarkerOfAsync(string path, DatabaseKey key)
    {
        await using var context = new ArcaDbContext(path, key);
        return (await context.Database.SqlQueryRaw<string>("SELECT Value AS Value FROM Probe").ToListAsync()).Single();
    }

    /// <summary>A centre with its data, and a backup of another one made with another password.</summary>
    async Task<(string Path, DatabaseKey Current, string Backup, BackupRestorer Restorer)> ArrangeAsync(Func<string, DatabaseKey, Task>? tamperBackup = null)
    {
        var other = _dir.File("other/arca.db");
        Directory.CreateDirectory(Path.GetDirectoryName(other)!);
        var otherKey = await CreateAsync(other, Password, "data of the backup");
        if (tamperBackup is not null)
        {
            await tamperBackup(other, otherKey);
        }

        var backup = _dir.File("copia" + BackupContainer.Extension);
        Assert.True((await BackupMaker.MakeAsync(other, otherKey, backup)).IsSuccess);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        var path = _dir.File("arca.db");
        var current = await CreateAsync(path, Newer, "current data");
        return (path, current, backup, new BackupRestorer(Service(), new TickingTime()));
    }

    static async Task<RestoreSession> UnlockedAsync(BackupRestorer restorer, string backup, string folder)
    {
        var session = restorer.Open(backup, folder).Value!;
        Assert.True((await restorer.UnlockWithPasswordAsync(session, Password)).IsSuccess);
        return session;
    }

    [Fact]
    [Trait("spec", Spec + ": Restauración guiada (Restauración correcta); Copia previa automática (Copia previa)")]
    public async Task Restoring_replaces_the_data_and_keeps_a_checked_copy_of_the_current_ones_and_tells_the_stages()
    {
        var (path, current, backup, restorer) = await ArrangeAsync();
        using var session = await UnlockedAsync(restorer, backup, _dir.Path);
        var steps = new List<string>();

        var done = await restorer.CompleteAsync(session, path, current, new Progress<string>(steps.Add));
        await Task.Delay(50);

        Assert.True(done.IsSuccess);
        Assert.False(done.Value!.Migrated);
        using var restored = Service().Unlock(path, Password).Value!;
        Assert.Equal("data of the backup", await MarkerOfAsync(path, restored));
        Assert.Equal("current data", await MarkerOfAsync(done.Value.PreviousCopyPath, current)); // what was replaced is still there
        Assert.Equal(["Backup.Stage.PreviousCopy", "Backup.Stage.Replacing"], steps);
    }

    [Fact]
    [Trait("spec", Spec + ": Migración de copias antiguas (Restaurar una copia antigua)")]
    public async Task A_backup_of_an_older_version_is_brought_up_to_date_while_restoring_and_the_backup_file_is_not_touched()
    {
        var (path, current, backup, restorer) = await ArrangeAsync(async (other, key) =>
        {
            await using var context = new ArcaDbContext(other, key);
            await context.Database.ExecuteSqlRawAsync("DROP TABLE HomeCards");
            await context.Database.ExecuteSqlRawAsync("DROP TABLE HomeCardsState");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM __EFMigrationsHistory WHERE MigrationId LIKE '%_HomeCards'"); // the schema of the version before
        });
        var before = Hash(backup);
        using var session = await UnlockedAsync(restorer, backup, _dir.Path);
        var steps = new List<string>();

        var done = await restorer.CompleteAsync(session, path, current, new Progress<string>(steps.Add));
        await Task.Delay(50);

        Assert.True(done.IsSuccess);
        Assert.True(done.Value!.Migrated);
        using var restored = Service().Unlock(path, Password).Value!;
        Assert.Equal(SchemaRelation.Same, await DatabaseInspector.ClassifyAsync(path, restored));
        Assert.Equal("data of the backup", await MarkerOfAsync(path, restored));
        Assert.Equal(before, Hash(backup)); // the file of the backup is as it was
        Assert.Contains("Backup.Stage.Migrating", steps);
        Assert.Empty(SchemaMigrator.CopiesOf(path + ".restoring")); // no leftovers of the migration on the temporary file
    }

    [Fact]
    [Trait("spec", Spec + ": Verificación previa de la copia (Copia de una versión más nueva)")]
    public async Task A_backup_of_a_newer_version_is_refused_before_anything_is_touched()
    {
        var (path, current, backup, restorer) = await ArrangeAsync(async (other, key) =>
        {
            await using var context = new ArcaDbContext(other, key);
            await context.Database.ExecuteSqlRawAsync("INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('29990101000000_FromTheFuture', '99.0.0')");
        });
        var before = Hash(path);
        using var session = await UnlockedAsync(restorer, backup, _dir.Path);

        var refused = await restorer.CompleteAsync(session, path, current);

        Assert.Equal("Backup.VersionNewer", refused.Error!.Code);
        Assert.Equal(before, Hash(path));
        Assert.Empty(BackupRestorer.PreviousCopiesOf(path));
    }

    [Fact]
    [Trait("spec", Spec + ": Sustitución atómica y recuperación automática (Fallo al migrar una copia antigua)")]
    public async Task A_migration_that_fails_leaves_the_current_data_and_no_copy_or_temporary_file_behind()
    {
        var (path, current, backup, restorer) = await ArrangeAsync(async (other, key) =>
        {
            await using var context = new ArcaDbContext(other, key);
            await context.Database.ExecuteSqlRawAsync("DELETE FROM __EFMigrationsHistory WHERE MigrationId LIKE '%_HomeCards'"); // pending again, but its tables exist: it cannot apply
        });
        var before = Hash(path);
        using var session = await UnlockedAsync(restorer, backup, _dir.Path);

        var failed = await restorer.CompleteAsync(session, path, current);

        Assert.Equal("Backup.MigrationFailed", failed.Error!.Code);
        Assert.Equal(before, Hash(path));
        Assert.Equal("current data", await MarkerOfAsync(path, current));
        Assert.Empty(BackupRestorer.PreviousCopiesOf(path));
        Assert.False(File.Exists(path + ".restoring"));
        Assert.Empty(SchemaMigrator.CopiesOf(path + ".restoring"));
    }

    [Fact]
    [Trait("spec", Spec + ": Copia previa automática de los datos actuales (Copia previa que falla)")]
    public async Task When_the_copy_of_the_current_data_cannot_be_checked_nothing_is_restored()
    {
        var (path, _, backup, restorer) = await ArrangeAsync();
        var before = Hash(path);
        using var session = await UnlockedAsync(restorer, backup, _dir.Path);

        var failed = await restorer.CompleteAsync(session, path, Arca.Testing.TestKeys.FromSeed("not-the-key-of-the-data"));

        Assert.Equal("Backup.PreviousCopyFailed", failed.Error!.Code);
        Assert.Equal(before, Hash(path));
        Assert.Empty(BackupRestorer.PreviousCopiesOf(path));
    }

    [Fact]
    [Trait("spec", Spec + ": Restauración sin conexión y con la base dañada (Base de datos dañada)")]
    public async Task A_damaged_current_database_is_kept_byte_for_byte_as_the_previous_copy_and_the_restore_goes_ahead()
    {
        var (path, _, backup, restorer) = await ArrangeAsync();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        var garbage = new byte[2000];
        new Random(1).NextBytes(garbage);
        await File.WriteAllBytesAsync(path, garbage);
        using var session = await UnlockedAsync(restorer, backup, _dir.Path);

        var done = await restorer.CompleteAsync(session, path, currentKey: null); // the current data cannot be opened: no key to check them with

        Assert.True(done.IsSuccess);
        Assert.Equal(garbage, await File.ReadAllBytesAsync(done.Value!.PreviousCopyPath));
        using var restored = Service().Unlock(path, Password).Value!;
        Assert.Equal("data of the backup", await MarkerOfAsync(path, restored));
    }

    [Fact]
    [Trait("spec", Spec + ": Restauración en otro equipo (Instalación nueva)")]
    public async Task On_a_new_installation_without_data_the_backup_is_restored_and_there_is_no_previous_copy()
    {
        var (path, _, backup, restorer) = await ArrangeAsync();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(path);
        File.Delete(KeyFileStore.PathFor(path));
        using var session = await UnlockedAsync(restorer, backup, _dir.Path);

        var done = await restorer.CompleteAsync(session, path);

        Assert.True(done.IsSuccess);
        Assert.Empty(BackupRestorer.PreviousCopiesOf(path));
        using var restored = Service().Unlock(path, Password).Value!;
        Assert.Equal("data of the backup", await MarkerOfAsync(path, restored));
    }

    [Fact]
    [Trait("spec", Spec + ": Copia previa automática de los datos actuales (Retención)")]
    public async Task Only_the_three_most_recent_previous_copies_are_kept_with_their_key_files()
    {
        var (path, _, backup, restorer) = await ArrangeAsync();
        var firstPrevious = string.Empty;
        for (var i = 0; i < 4; i++)
        {
            using var session = await UnlockedAsync(restorer, backup, _dir.Path);
            var done = await restorer.CompleteAsync(session, path, null);
            firstPrevious = i == 0 ? done.Value!.PreviousCopyPath : firstPrevious;
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        }

        var kept = BackupRestorer.PreviousCopiesOf(path);
        Assert.Equal(3, kept.Count);
        Assert.DoesNotContain(firstPrevious, kept); // the oldest went
        Assert.False(File.Exists(firstPrevious + KeyFileStore.Extension));
        Assert.All(kept, k => Assert.True(File.Exists(k + KeyFileStore.Extension)));
    }

    [Fact]
    [Trait("spec", Spec + ": Feedback y guía en la restauración (Cancelar antes de sustituir)")]
    public async Task Cancelling_before_the_replacement_changes_nothing_and_leaves_no_copy()
    {
        var (path, current, backup, restorer) = await ArrangeAsync();
        var before = Hash(path);
        using var session = await UnlockedAsync(restorer, backup, _dir.Path);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => restorer.CompleteAsync(session, path, current, null, cancelled.Token));

        Assert.Equal(before, Hash(path));
        Assert.Empty(BackupRestorer.PreviousCopiesOf(path));
        Assert.False(File.Exists(path + ".restoring"));
    }

    [Fact]
    [Trait("spec", Spec + ": Sustitución atómica y recuperación automática (Fallo al sustituir)")]
    public async Task Work_folders_and_temporary_files_left_by_an_interruption_are_cleaned_at_start_and_previous_copies_never()
    {
        var (path, _, backup, restorer) = await ArrangeAsync();
        using (var session = await UnlockedAsync(restorer, backup, _dir.Path))
        {
            Assert.True((await restorer.CompleteAsync(session, path)).IsSuccess);
        }

        Directory.CreateDirectory(_dir.File(".arca-restore-abc"));
        Directory.CreateDirectory(_dir.File(".arca-backup-def"));
        await File.WriteAllTextAsync(path + ".restoring", "half");
        await File.WriteAllTextAsync(KeyFileStore.PathFor(path) + ".restoring", "half");

        BackupRestorer.CleanLeftovers(path);

        Assert.False(Directory.Exists(_dir.File(".arca-restore-abc")));
        Assert.False(Directory.Exists(_dir.File(".arca-backup-def")));
        Assert.False(File.Exists(path + ".restoring"));
        Assert.False(File.Exists(KeyFileStore.PathFor(path) + ".restoring"));
        Assert.Single(BackupRestorer.PreviousCopiesOf(path));
        Assert.True(File.Exists(path));
    }
}
