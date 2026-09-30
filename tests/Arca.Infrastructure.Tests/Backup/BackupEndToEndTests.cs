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

/// <summary>The whole cycle over a real encrypted database: data, backup, later changes, restoration (copies-de-seguretat, 6.5).</summary>
public sealed class BackupEndToEndTests
{
    const string Password = "riu cadira blau gos";

    [Fact]
    [Trait("spec", "copies-de-seguretat/restauracio: Restauración correcta (extremo a extremo)")]
    public async Task Changes_made_after_the_backup_are_lost_on_restoring_and_the_data_before_are_kept_as_a_previous_copy()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        var access = new AccessService(new NSecKeyCrypto(), new FileKeyFileStore(), new Argon2Parameters(1024, 1, 1));
        var created = access.CreateAccess(Password, Password).Value!;
        var groups = created.Challenge.Indices.Select(i => RecoveryKey.Groups(created.RecoveryKey)[i]).ToArray();
        Assert.True((await DatabaseCreator.CreateAsync(path, created, groups)).IsSuccess);
        var key = created.DataKey;
        var store = new EfInventory(() => new ArcaDbContext(path, key));
        var zone = (await new CreateZoneHandler(store.Zones, store).HandleAsync(new CreateZoneRequest("Planta 1"), default)).Value!;
        var add = new AddLockerHandler(store.Lockers, store.Zones, store.Events, store, new SystemClock());
        await add.HandleAsync(new AddLockerRequest(1, zone.Id), default);
        await add.HandleAsync(new AddLockerRequest(2, zone.Id), default);
        var backup = dir.File("copia" + BackupContainer.Extension);
        Assert.True((await BackupMaker.MakeAsync(path, key, backup)).IsSuccess);
        await add.HandleAsync(new AddLockerRequest(3, zone.Id), default); // after the backup
        var restorer = new BackupRestorer(access);

        using var session = restorer.Open(backup, dir.Path).Value!;
        Assert.True((await restorer.UnlockWithPasswordAsync(session, Password)).IsSuccess);
        var done = await restorer.CompleteAsync(session, path, key);

        Assert.True(done.IsSuccess);
        using var restored = access.Unlock(path, Password).Value!;
        Assert.Equal(2, (await DatabaseInspector.CountAsync(path, restored)).Lockers); // the third one is gone
        Assert.Equal(3, (await DatabaseInspector.CountAsync(done.Value!.PreviousCopyPath, key)).Lockers); // and it is in the previous copy
    }
}
