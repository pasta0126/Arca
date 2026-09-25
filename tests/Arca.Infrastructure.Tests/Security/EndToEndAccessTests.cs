// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Diagnostics;
using Arca.Application.Security;
using Arca.Application.Storage;
using Arca.Infrastructure.Backup;
using Arca.Infrastructure.Security;
using Arca.Infrastructure.Storage;
using Arca.Infrastructure.Tests.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Arca.Infrastructure.Tests.Security;

public sealed class EndToEndAccessTests
{
    const string Spec = "acces-i-xifrat/xifrat-de-la-base";
    const string First = "riu cadira blau gos";
    const string Second = "gat ratllat sota pluja";
    const string Third = "pati obert de matí";

    static readonly Argon2Parameters _cost = new(1024, 1, 1);

    static AccessService Service() => new(new NSecKeyCrypto(), new FileKeyFileStore(), _cost);

    static async Task<string> MarkerAsync(string path, DatabaseKey key)
    {
        await using var context = new ArcaDbContext(path, key);
        return (await context.Database.SqlQueryRaw<string>("SELECT Value AS Value FROM Probe").ToListAsync()).Single();
    }

    static string[] Groups(RecoveryKeyChallenge challenge, string key) => [.. challenge.Indices.Select(i => RecoveryKey.Groups(key)[i])];

    [Fact]
    [Trait("spec", Spec + ": Abrir la base de datos (Con la contraseña)")]
    public async Task Create_save_the_key_close_unlock_forget_recover_change_and_restore_an_old_backup()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");

        // 1. Create the password and the data; the recovery key is shown and confirmed.
        string recovery;
        using (var access = Service().CreateAccess(First, First).Value!)
        {
            recovery = access.RecoveryKey;
            Assert.True((await DatabaseCreator.CreateAsync(path, access, Groups(access.Challenge, recovery))).IsSuccess);
            await using var context = new ArcaDbContext(path, access.DataKey);
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE Probe (Value TEXT NOT NULL)");
            await context.Database.ExecuteSqlRawAsync("INSERT INTO Probe(Value) VALUES ('school year one')");
        }

        // 2. Close and unlock again with the password.
        SqliteConnection.ClearAllPools();
        using (var key = Service().Unlock(path, First).Value!)
        {
            Assert.Equal("school year one", await MarkerAsync(path, key));

            // 3. A backup is made while the first password is the centre's.
            Assert.True((await BackupMaker.CreateAsync(path, key, dir.File("old" + BackupContainer.Extension))).IsSuccess);
        }

        // 4. The password is forgotten: the recovery key opens the data and sets a new password and a new key.
        string newRecovery;
        using (var pending = Service().PrepareReset(path, recovery, Second, Second).Value!)
        {
            newRecovery = pending.RecoveryKey;
            Assert.True(Service().Commit(path, pending, Groups(pending.Challenge, newRecovery)).IsSuccess);
        }

        Assert.Equal("Keys.WrongCredentials", Service().Unlock(path, First).Error!.Code);
        Assert.False(Service().CheckRecoveryKey(path, recovery).IsSuccess);
        using (var key = Service().Unlock(path, Second).Value!)
        {
            Assert.Equal("school year one", await MarkerAsync(path, key));
        }

        // 5. The password is changed.
        Assert.True(Service().ChangePassword(path, Second, Third, Third).IsSuccess);
        Assert.Equal("Keys.WrongCredentials", Service().Unlock(path, Second).Error!.Code);
        Assert.True(Service().Unlock(path, Third).IsSuccess);
        Assert.True(Service().CheckRecoveryKey(path, newRecovery).IsSuccess);

        // 6. The old backup is restored: it opens with the password it had, and what was there is kept.
        SqliteConnection.ClearAllPools();
        var restorer = new BackupRestorer(Service());
        using var session = restorer.Open(dir.File("old" + BackupContainer.Extension), dir.Path).Value!;
        Assert.True((await restorer.UnlockWithPasswordAsync(session, First)).IsSuccess);
        var previous = restorer.Complete(session, path).Value!;

        using var restored = Service().Unlock(path, First).Value!;
        Assert.Equal("school year one", await MarkerAsync(path, restored));
        Assert.Equal("Keys.WrongCredentials", Service().Unlock(path, Third).Error!.Code);
        Assert.True(File.Exists(previous));
        Assert.True(File.Exists(previous + KeyFileStore.Extension));
    }

    [Fact]
    [Trait("spec", Spec + ": Derivación resistente a fuerza bruta (Tiempo de desbloqueo)")]
    public void Unlocking_with_the_real_argon2id_cost_takes_under_two_seconds()
    {
        // The starting cost of the design (64 MiB, 3 passes). This measures the computer running the tests; the
        // low-end computer of the spec has to be measured on real equipment before release (docs/riesgos.md).
        var service = new AccessService(new NSecKeyCrypto(), new FileKeyFileStore());
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        using var access = service.CreateAccess(First, First).Value!;
        KeyFileStore.Write(path, access.KeyFile);
        var stored = KeyFileStore.Read(path).Value!.Password.Parameters;

        var clock = Stopwatch.StartNew();
        var unlocked = service.Unlock(path, First);
        clock.Stop();

        Assert.True(unlocked.IsSuccess);
        Assert.Equal(new Argon2Parameters(64 * 1024, 3, 1), stored);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(2), $"unlocking took {clock.Elapsed.TotalSeconds:0.00} s");
    }
}
