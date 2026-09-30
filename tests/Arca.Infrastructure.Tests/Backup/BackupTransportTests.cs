// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Lockers.AddLocker;
using Arca.Application.Security;
using Arca.Application.Storage;
using Arca.Application.Zones.CreateZone;
using Arca.Infrastructure.Backup;
using Arca.Infrastructure.Common;
using Arca.Infrastructure.Inventory;
using Arca.Infrastructure.Security;
using Arca.Infrastructure.Storage;
using Arca.Infrastructure.Tests.Storage;
using Xunit;

namespace Arca.Infrastructure.Tests.Backup;

/// <summary>
/// A backup is the same file in every system (copies-de-seguretat, 6.3). The repository keeps one made on macOS with fictitious data, and this test
/// restores it wherever the tests run: the CI of Windows and Linux restore what macOS wrote. To make it again (only when the format changes on
/// purpose) run the test with ARCA_WRITE_BACKUP_FIXTURE=1.
/// </summary>
public sealed class BackupTransportTests
{
    const string Password = "fixture only, not a secret";

    static string Fixture => Path.Combine(AppContext.BaseDirectory, "Backup", "Fixtures", "macos.arcabackup");

    /// <summary>The copy in the source tree, where it is written when it is made again.</summary>
    static string SourceFixture => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Backup", "Fixtures", "macos.arcabackup"));

    static AccessService Service() => new(new NSecKeyCrypto(), new FileKeyFileStore(), new Argon2Parameters(1024, 1, 1));

    [Fact]
    [Trait("spec", "copies-de-seguretat/copia-de-seguretat: Transportabilidad entre sistemas")]
    public async Task A_backup_made_on_macOS_is_restored_on_this_system()
    {
        if (Environment.GetEnvironmentVariable("ARCA_WRITE_BACKUP_FIXTURE") == "1")
        {
            await WriteFixtureAsync();
        }

        var fixture = Environment.GetEnvironmentVariable("ARCA_WRITE_BACKUP_FIXTURE") == "1" ? SourceFixture : Fixture;

        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        var restorer = new BackupRestorer(Service());

        using var session = restorer.Open(fixture, dir.Path).Value!;
        Assert.True((await restorer.UnlockWithPasswordAsync(session, Password)).IsSuccess);
        var done = await restorer.CompleteAsync(session, path);

        Assert.True(done.IsSuccess);
        using var key = Service().Unlock(path, Password).Value!;
        Assert.Equal(2, (await DatabaseInspector.CountAsync(path, key)).Lockers);
    }

    static async Task WriteFixtureAsync()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        var access = Service();
        var created = access.CreateAccess(Password, Password).Value!;
        var groups = created.Challenge.Indices.Select(i => RecoveryKey.Groups(created.RecoveryKey)[i]).ToArray();
        Assert.True((await DatabaseCreator.CreateAsync(path, created, groups)).IsSuccess);
        var store = new EfInventory(() => new ArcaDbContext(path, created.DataKey));
        var zone = (await new CreateZoneHandler(store.Zones, store).HandleAsync(new CreateZoneRequest("Planta 1"), default)).Value!;
        var add = new AddLockerHandler(store.Lockers, store.Zones, store.Events, store, new SystemClock());
        await add.HandleAsync(new AddLockerRequest(1, zone.Id), default);
        await add.HandleAsync(new AddLockerRequest(2, zone.Id), default);
        Assert.True((await BackupMaker.MakeAsync(path, created.DataKey, SourceFixture)).IsSuccess);
    }
}
