// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Security.Cryptography;
using System.Text;
using Arca.Application.Security;
using Arca.Application.Storage;
using Arca.Infrastructure.Backup;
using Arca.Infrastructure.Security;
using Arca.Infrastructure.Storage;
using Arca.Infrastructure.Tests.Storage;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Arca.Infrastructure.Tests.Security;

public sealed class BackupKeysTests
{
    const string Spec = "acces-i-xifrat/xifrat-de-la-base";
    const string Password = "riu cadira blau gos";
    const string Newer = "gat ratllat sota pluja";
    const string Marker = "Núria Çaragol l·l";

    static readonly Argon2Parameters _cost = new(1024, 1, 1);

    static AccessService Service() => new(new NSecKeyCrypto(), new FileKeyFileStore(), _cost);

    static byte[] Hash(string path) => SHA256.HashData(File.ReadAllBytes(path));

    /// <summary>A real database at <paramref name="path"/> with a row that says which one it is.</summary>
    static async Task<(DatabaseKey Key, string Recovery)> CreateAsync(string path, string password, string marker)
    {
        var access = Service().CreateAccess(password, password).Value!;
        var groups = access.Challenge.Indices.Select(i => RecoveryKey.Groups(access.RecoveryKey)[i]).ToArray();
        Assert.True((await DatabaseCreator.CreateAsync(path, access, groups)).IsSuccess);
        await using (var context = new ArcaDbContext(path, access.DataKey))
        {
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE Probe (Value TEXT NOT NULL)");
            await context.Database.ExecuteSqlRawAsync("INSERT INTO Probe(Value) VALUES ({0})", marker);
        }

        return (access.DataKey, access.RecoveryKey);
    }

    static async Task<string> MarkerOfAsync(string path, DatabaseKey key)
    {
        await using var context = new ArcaDbContext(path, key);
        return (await context.Database.SqlQueryRaw<string>("SELECT Value AS Value FROM Probe").ToListAsync()).Single();
    }

    [Fact]
    [Trait("spec", Spec + ": Copias de seguridad con sus llaves (Contenido de la copia)")]
    public async Task A_backup_contains_the_database_and_the_current_key_file_and_is_verified()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        var (key, _) = await CreateAsync(path, Password, Marker);
        var backup = dir.File("copia" + BackupContainer.Extension);

        var made = await BackupMaker.CreateAsync(path, key, backup);

        Assert.True(made.IsSuccess);
        var extracted = BackupContainer.Extract(backup, dir.File("out")).Value!;
        Assert.Equal(Hash(KeyFileStore.PathFor(path)), Hash(extracted.KeyFilePath));
        Assert.Equal(Marker, await MarkerOfAsync(extracted.DatabasePath, key));
        Assert.Empty(Directory.EnumerateFileSystemEntries(dir.Path, ".arca-backup-*"));
    }

    [Fact]
    [Trait("spec", Spec + ": Copias de seguridad con sus llaves (Contenido de la copia)")]
    public async Task A_backup_is_refused_when_the_key_file_is_missing_and_leaves_nothing()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        var (key, _) = await CreateAsync(path, Password, Marker);
        File.Delete(KeyFileStore.PathFor(path));
        var backup = dir.File("copia" + BackupContainer.Extension);

        var made = await BackupMaker.CreateAsync(path, key, backup);

        Assert.Equal("Keys.FileMissing", made.Error!.Code);
        Assert.False(File.Exists(backup));
    }

    [Theory]
    [Trait("spec", Spec + ": Copias de seguridad con sus llaves (Restaurar con la contraseña de la copia)")]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(-1)]
    public async Task A_truncated_or_foreign_file_is_reported_as_damaged(int cut)
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        var (key, _) = await CreateAsync(path, Password, Marker);
        var backup = dir.File("copia" + BackupContainer.Extension);
        await BackupMaker.CreateAsync(path, key, backup);
        var bytes = File.ReadAllBytes(backup);
        File.WriteAllBytes(backup, cut switch { 0 => Encoding.UTF8.GetBytes("not a backup"), 5 => bytes[..5], _ => bytes[..^10] });

        var result = new BackupRestorer(Service()).Open(backup, dir.Path);

        Assert.Equal("Keys.BackupDamaged", result.Error!.Code);
        Assert.Empty(Directory.EnumerateFileSystemEntries(dir.Path, ".arca-restore-*"));
    }

    [Fact]
    [Trait("spec", Spec + ": Adoptar la llave de la copia al restaurar (Aviso)")]
    public async Task Restoring_with_the_password_of_the_backup_adopts_it_and_warns_first()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        var (key, _) = await CreateAsync(path, Password, "old data");
        var backup = dir.File("copia" + BackupContainer.Extension);
        await BackupMaker.CreateAsync(path, key, backup);
        SqliteClear();
        File.Delete(path);
        File.Delete(KeyFileStore.PathFor(path));
        var (currentKey, _) = await CreateAsync(path, Newer, "current data");
        var restorer = new BackupRestorer(Service());

        using var session = restorer.Open(backup, dir.Path).Value!;
        var unlocked = await restorer.UnlockWithPasswordAsync(session, Password);

        Assert.Contains(unlocked.Notices, n => n.Code == "Keys.RestoreAdoptsPassword"); // shown before confirming
        Assert.Equal("current data", await MarkerOfAsync(path, currentKey)); // nothing has changed yet
        Assert.True(restorer.Complete(session, path).IsSuccess);
        using var restored = Service().Unlock(path, Password).Value!;
        Assert.Equal("old data", await MarkerOfAsync(path, restored));
        Assert.Equal("Keys.WrongCredentials", Service().Unlock(path, Newer).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Adoptar la llave de la copia al restaurar (Conservación de lo anterior)")]
    public async Task The_previous_copy_keeps_the_database_and_key_file_that_were_replaced()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        var (key, _) = await CreateAsync(path, Password, "old data");
        var backup = dir.File("copia" + BackupContainer.Extension);
        await BackupMaker.CreateAsync(path, key, backup);
        SqliteClear();
        File.Delete(path);
        File.Delete(KeyFileStore.PathFor(path));
        var (currentKey, _) = await CreateAsync(path, Newer, "current data");
        var databaseBefore = Hash(path);
        var keysBefore = Hash(KeyFileStore.PathFor(path));
        var restorer = new BackupRestorer(Service());
        using var session = restorer.Open(backup, dir.Path).Value!;
        await restorer.UnlockWithPasswordAsync(session, Password);

        var previous = restorer.Complete(session, path).Value!;

        Assert.Equal(databaseBefore, Hash(previous));
        Assert.Equal(keysBefore, Hash(previous + KeyFileStore.Extension));
        Assert.Contains(previous, BackupRestorer.PreviousCopiesOf(path));
        Assert.Equal("current data", await MarkerOfAsync(previous, currentKey));
    }

    [Fact]
    [Trait("spec", Spec + ": Restaurar con la contraseña de la copia (con la clave de recuperación)")]
    public async Task Restoring_with_the_recovery_key_sets_a_new_password_and_key_for_the_restored_data()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        var (key, recovery) = await CreateAsync(path, Password, "old data");
        var backup = dir.File("copia" + BackupContainer.Extension);
        await BackupMaker.CreateAsync(path, key, backup);
        var restorer = new BackupRestorer(Service());
        using var session = restorer.Open(backup, dir.Path).Value!;

        using var pending = restorer.BeginRecovery(session, recovery.ToLowerInvariant(), Newer, Newer).Value!;
        var groups = pending.Challenge.Indices.Select(i => RecoveryKey.Groups(pending.RecoveryKey)[i]).ToArray();
        var confirmed = await restorer.ConfirmRecoveryAsync(session, pending, groups);

        Assert.True(confirmed.IsSuccess);
        Assert.True(restorer.Complete(session, path).IsSuccess);
        using var restored = Service().Unlock(path, Newer).Value!;
        Assert.Equal("old data", await MarkerOfAsync(path, restored));
        Assert.Equal("Keys.WrongCredentials", Service().Unlock(path, Password).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Copias de seguridad con sus llaves (Restaurar con la contraseña de la copia)")]
    public async Task A_backup_made_before_a_password_change_still_opens_with_the_old_password()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        var (key, _) = await CreateAsync(path, Password, "data");
        var backup = dir.File("copia" + BackupContainer.Extension);
        await BackupMaker.CreateAsync(path, key, backup);
        Assert.True(Service().ChangePassword(path, Password, Newer, Newer).IsSuccess);
        var restorer = new BackupRestorer(Service());
        using var session = restorer.Open(backup, dir.Path).Value!;

        var withNew = await restorer.UnlockWithPasswordAsync(session, Newer);
        var withOld = await restorer.UnlockWithPasswordAsync(session, Password);

        Assert.Equal("Keys.WrongCredentials", withNew.Error!.Code);
        Assert.True(withOld.IsSuccess);
    }

    [Fact]
    [Trait("spec", Spec + ": Restaurar con la contraseña de la copia (Restaurar con la contraseña de la copia)")]
    public async Task A_wrong_password_unlocks_nothing_and_completing_is_refused()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        var (key, _) = await CreateAsync(path, Password, "data");
        var backup = dir.File("copia" + BackupContainer.Extension);
        await BackupMaker.CreateAsync(path, key, backup);
        var before = Hash(path);
        var restorer = new BackupRestorer(Service());
        using var session = restorer.Open(backup, dir.Path).Value!;

        var wrong = await restorer.UnlockWithPasswordAsync(session, Newer);

        Assert.Equal("Keys.WrongCredentials", wrong.Error!.Code);
        Assert.False(session.IsUnlocked);
        Assert.False(restorer.Complete(session, path).IsSuccess);
        Assert.Equal(before, Hash(path));
        Assert.Empty(BackupRestorer.PreviousCopiesOf(path));
    }

    [Fact]
    [Trait("spec", Spec + ": Compatibilidad entre compilaciones (Otro equipo)")]
    public async Task A_backup_restores_on_another_computer_with_no_data()
    {
        using var source = new TempDirectory();
        var path = source.File("arca.db");
        var (key, _) = await CreateAsync(path, Password, "school data");
        var backup = source.File("copia" + BackupContainer.Extension);
        await BackupMaker.CreateAsync(path, key, backup);

        using var other = new TempDirectory();
        var moved = other.File("copia" + BackupContainer.Extension);
        File.Copy(backup, moved);
        var target = other.File("data" + Path.DirectorySeparatorChar + "arca.db");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var restorer = new BackupRestorer(Service());

        using var session = restorer.Open(moved, Path.GetDirectoryName(target)!).Value!;
        Assert.True((await restorer.UnlockWithPasswordAsync(session, Password)).IsSuccess);
        Assert.True(restorer.Complete(session, target).IsSuccess);

        using var restored = Service().Unlock(target, Password).Value!;
        Assert.Equal("school data", await MarkerOfAsync(target, restored));
        Assert.Empty(BackupRestorer.PreviousCopiesOf(target)); // there was nothing to keep
    }

    [Fact]
    [Trait("spec", Spec + ": Adoptar la llave de la copia al restaurar (Conservación de lo anterior)")]
    public async Task A_failure_while_replacing_puts_the_previous_data_back()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        var (key, _) = await CreateAsync(path, Password, "old data");
        var backup = dir.File("copia" + BackupContainer.Extension);
        await BackupMaker.CreateAsync(path, key, backup);
        SqliteClear();
        File.Delete(path);
        File.Delete(KeyFileStore.PathFor(path));
        var (currentKey, _) = await CreateAsync(path, Newer, "current data");
        var databaseBefore = Hash(path);
        var keysBefore = Hash(KeyFileStore.PathFor(path));
        var restorer = new BackupRestorer(Service());
        using var session = restorer.Open(backup, dir.Path).Value!;
        await restorer.UnlockWithPasswordAsync(session, Password);
        Directory.CreateDirectory(KeyFileStore.PathFor(path) + ".restoring"); // the key file swap cannot happen

        var result = restorer.Complete(session, path);

        Assert.Equal("Keys.RestoreFailed", result.Error!.Code);
        Assert.Equal(databaseBefore, Hash(path));
        Assert.Equal(keysBefore, Hash(KeyFileStore.PathFor(path)));
        Assert.Equal("current data", await MarkerOfAsync(path, currentKey));
    }

    [Fact]
    [Trait("spec", Spec + ": Adoptar la llave de la copia al restaurar (Conservación de lo anterior)")]
    public async Task A_failed_restore_into_an_empty_place_leaves_no_half_applied_state()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        var (key, _) = await CreateAsync(path, Password, "old data");
        var backup = dir.File("copia" + BackupContainer.Extension);
        await BackupMaker.CreateAsync(path, key, backup);
        SqliteClear();
        File.Delete(path);
        File.Delete(KeyFileStore.PathFor(path)); // nothing there: a new computer
        var restorer = new BackupRestorer(Service());
        using var session = restorer.Open(backup, dir.Path).Value!;
        await restorer.UnlockWithPasswordAsync(session, Password);
        Directory.CreateDirectory(KeyFileStore.PathFor(path)); // the key file cannot take its place, after the database has

        var result = restorer.Complete(session, path);

        Assert.Equal("Keys.RestoreFailed", result.Error!.Code);
        Assert.False(File.Exists(path)); // the restored database did not stay behind without its key file
        Assert.False(File.Exists(KeyFileStore.PathFor(path)));
    }

    static void SqliteClear() => Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
}
