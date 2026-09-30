// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text;
using Arca.Application.Lockers.AddLocker;
using Arca.Application.Storage;
using Arca.Application.Zones.CreateZone;
using Arca.Infrastructure.Backup;
using Arca.Infrastructure.Common;
using Arca.Infrastructure.Inventory;
using Arca.Infrastructure.Security;
using Arca.Infrastructure.Storage;
using Arca.Infrastructure.Tests.Storage;
using Arca.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Arca.Infrastructure.Tests.Backup;

/// <summary>Making a backup over a real encrypted database (copies-de-seguretat, copia-de-seguridad).</summary>
public sealed class BackupMakerTests : IDisposable
{
    const string Spec = "copies-de-seguretat/copia-de-seguretat";

    readonly TempDirectory _dir = new();
    readonly DatabaseKey _key = TestKeys.FromSeed("backup-maker");

    public void Dispose() => _dir.Dispose();

    async Task<(string Path, EfInventory Store)> OpenAsync()
    {
        var path = _dir.File("arca.db");
        var created = await Arca.Infrastructure.Storage.ArcaDatabase.CreateAsync(path, _key);
        await created.Value!.DisposeAsync();
        // The key file that travels with the data, as the application keeps it.
        File.WriteAllBytes(KeyFileStore.PathFor(path), [1, 2, 3, 4]);
        return (path, new EfInventory(() => new ArcaDbContext(path, _key)));
    }

    static async Task AddLockerAsync(EfInventory store, string zoneName, int number)
    {
        var zone = (await new CreateZoneHandler(store.Zones, store).HandleAsync(new CreateZoneRequest(zoneName), default)).Value!;
        await new AddLockerHandler(store.Lockers, store.Zones, store.Events, store, new SystemClock()).HandleAsync(new AddLockerRequest(number, zone.Id), default);
    }

    [Fact]
    [Trait("spec", Spec + ": Copia manual de la base de datos (Copia correcta); Copia consistente (Copia tras un cambio)")]
    public async Task A_backup_made_right_after_a_change_holds_it_and_says_where_it_is_and_how_big()
    {
        var (path, store) = await OpenAsync();
        await AddLockerAsync(store, "Planta 1", 1);
        var destination = _dir.File("copia.arcabackup");

        var made = await BackupMaker.MakeAsync(path, _key, destination);

        Assert.True(made.IsSuccess);
        Assert.Equal(destination, made.Value!.Path);
        Assert.Equal(new FileInfo(destination).Length, made.Value.SizeBytes);
        var extracted = BackupContainer.Extract(destination, _dir.File("check"));
        Assert.Equal(1, (await DatabaseInspector.CountAsync(extracted.Value!.DatabasePath, _key)).Lockers);
    }

    [Fact]
    [Trait("spec", Spec + ": Copia consistente")]
    public async Task A_backup_can_be_made_while_the_application_has_the_database_open_and_includes_what_was_confirmed()
    {
        var (path, store) = await OpenAsync();
        await using var inUse = new ArcaDbContext(path, _key);
        await inUse.Database.OpenConnectionAsync(); // the application keeps its connection
        await AddLockerAsync(store, "Planta 1", 1);
        await AddLockerAsync(store, "Planta 2", 2);

        var made = await BackupMaker.MakeAsync(path, _key, _dir.File("en-us.arcabackup"));

        Assert.True(made.IsSuccess);
        var extracted = BackupContainer.Extract(made.Value!.Path, _dir.File("check"));
        Assert.Equal(2, (await DatabaseInspector.CountAsync(extracted.Value!.DatabasePath, _key)).Lockers);
    }

    [Fact]
    [Trait("spec", Spec + ": Copia manual de la base de datos (Sin datos en claro)")]
    public async Task Nothing_can_be_read_in_the_backup_file()
    {
        var (path, store) = await OpenAsync();
        await AddLockerAsync(store, "Zona-Molt-Reconeixible", 1);
        var destination = _dir.File("copia.arcabackup");
        await BackupMaker.MakeAsync(path, _key, destination);

        var bytes = await File.ReadAllBytesAsync(destination);

        Assert.DoesNotContain("Zona-Molt-Reconeixible", Encoding.Latin1.GetString(bytes), StringComparison.Ordinal);
        Assert.DoesNotContain("SQLite format", Encoding.Latin1.GetString(bytes), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Nombre y destino de la copia (Nombre propuesto)")]
    public void The_name_proposed_has_the_date_and_the_time_and_the_extension_of_ARCA()
    {
        var name = BackupDestination.SuggestedName(new DateTimeOffset(2026, 9, 24, 10, 30, 0, TimeSpan.FromHours(2)));

        Assert.Equal("ARCA-copia-2026-09-24-1030.arcabackup", name);
    }

    // --- The destination ---

    [Fact]
    [Trait("spec", Spec + ": Destino no válido (Destino igual a la base de datos)")]
    public async Task The_database_and_its_key_file_can_never_be_the_destination_and_nothing_changes()
    {
        var (path, _) = await OpenAsync();
        var before = await File.ReadAllBytesAsync(path);

        var onDatabase = await BackupMaker.MakeAsync(path, _key, path);
        var onKeys = await BackupMaker.MakeAsync(path, _key, KeyFileStore.PathFor(path));

        Assert.Equal("Backup.DestinationIsDatabase", onDatabase.Error!.Code);
        Assert.Equal("Backup.DestinationIsDatabase", onKeys.Error!.Code);
        Assert.Equal(before, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    [Trait("spec", Spec + ": Destino no válido (Sin permisos)")]
    public async Task A_folder_that_does_not_exist_or_does_not_accept_writing_is_refused_with_its_reason()
    {
        var (path, _) = await OpenAsync();

        var missing = await BackupMaker.MakeAsync(path, _key, Path.Combine(_dir.File("no-existeix"), "copia.arcabackup"));
        Assert.Equal("Backup.FolderMissing", missing.Error!.Code);

        if (!OperatingSystem.IsWindows())
        {
            var readOnly = _dir.File("només-lectura");
            Directory.CreateDirectory(readOnly);
            File.SetUnixFileMode(readOnly, UnixFileMode.UserRead | UnixFileMode.UserExecute);
            try
            {
                var refused = await BackupMaker.MakeAsync(path, _key, Path.Combine(readOnly, "copia.arcabackup"));
                Assert.Equal("Backup.FolderNotWritable", refused.Error!.Code);
            }
            finally
            {
                File.SetUnixFileMode(readOnly, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }
    }

    // --- Nothing half done ---

    [Fact]
    [Trait("spec", Spec + ": Escritura atómica de la copia (Fallo a mitad)")]
    public async Task A_failure_halfway_leaves_no_partial_file_and_an_older_file_of_the_same_name_intact()
    {
        var (path, _) = await OpenAsync();
        var destination = _dir.File("copia.arcabackup");
        await File.WriteAllTextAsync(destination, "la còpia anterior");

        var failed = await BackupMaker.MakeAsync(path, TestKeys.FromSeed("not-the-key"), destination); // the snapshot cannot be opened

        Assert.Equal("Backup.CopyFailed", failed.Error!.Code);
        Assert.Equal("la còpia anterior", await File.ReadAllTextAsync(destination));
        Assert.Equal(["arca.arcakeys", "arca.db", "copia.arcabackup"], Directory.GetFileSystemEntries(_dir.Path).Select(Path.GetFileName).Order(StringComparer.Ordinal));
    }

    [Fact]
    [Trait("spec", Spec + ": Escritura atómica de la copia (Cancelación)")]
    public async Task Cancelling_changes_no_file_and_the_database_stays_as_it_was()
    {
        var (path, _) = await OpenAsync();
        var destination = _dir.File("copia.arcabackup");
        await File.WriteAllTextAsync(destination, "la còpia anterior");
        var before = await File.ReadAllBytesAsync(path);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => BackupMaker.MakeAsync(path, _key, destination, null, cancelled.Token));

        Assert.Equal("la còpia anterior", await File.ReadAllTextAsync(destination));
        Assert.Equal(before, await File.ReadAllBytesAsync(path));
        Assert.DoesNotContain(Directory.GetFileSystemEntries(_dir.Path).Select(Path.GetFileName), n => n!.StartsWith(".arca-backup-", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("spec", Spec + ": Feedback y progreso de la copia")]
    public async Task The_stages_are_told_as_they_happen_in_their_order()
    {
        var (path, _) = await OpenAsync();
        var steps = new List<string>();

        await BackupMaker.MakeAsync(path, _key, _dir.File("copia.arcabackup"), new Progress<string>(steps.Add));
        await Task.Delay(50);

        Assert.Equal(["Backup.Stage.Copying", "Backup.Stage.Verifying", "Backup.Stage.Finishing"], steps);
    }
}
