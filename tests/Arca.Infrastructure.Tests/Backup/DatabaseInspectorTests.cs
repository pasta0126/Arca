// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Backup;
using Arca.Application.SchoolYears.CreateAcademicYear;
using Arca.Application.Storage;
using Arca.Application.Lockers.AddLocker;
using Arca.Application.Zones.CreateZone;
using Arca.Infrastructure.Backup;
using Arca.Infrastructure.Common;
using Arca.Infrastructure.Inventory;
using Arca.Infrastructure.Storage;
using Arca.Infrastructure.Tests.Storage;
using Arca.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Arca.Infrastructure.Tests.Backup;

/// <summary>Looking inside a database file without changing it (copies-de-seguretat, Verificación previa de la copia y Vista previa del contenido).</summary>
public sealed class DatabaseInspectorTests : IDisposable
{
    const string Spec = "copies-de-seguretat/restauracio";

    readonly TempDirectory _dir = new();
    readonly DatabaseKey _key = TestKeys.FromSeed("inspector");

    public void Dispose() => _dir.Dispose();

    async Task<string> CreateAsync(string name = "arca.db")
    {
        var path = _dir.File(name);
        await (await ArcaDatabase.CreateAsync(path, _key)).Value!.DisposeAsync();
        return path;
    }

    [Fact]
    [Trait("spec", Spec + ": Vista previa del contenido (Comparación)")]
    public async Task The_counts_are_of_years_students_active_lockers_and_assignments_and_nothing_else()
    {
        var path = await CreateAsync();
        var store = new EfInventory(() => new ArcaDbContext(path, _key));
        var clock = new SystemClock();
        await new CreateAcademicYearHandler(store.Years, store).HandleAsync(new CreateAcademicYearRequest(new DateOnly(2026, 9, 1), new DateOnly(2027, 6, 30)), default);
        var zone = (await new CreateZoneHandler(store.Zones, store).HandleAsync(new CreateZoneRequest("Planta 1"), default)).Value!;
        var add = new AddLockerHandler(store.Lockers, store.Zones, store.Events, store, clock);
        await add.HandleAsync(new AddLockerRequest(1, zone.Id), default);
        await add.HandleAsync(new AddLockerRequest(2, zone.Id), default);

        var counts = await DatabaseInspector.CountAsync(path, _key);

        Assert.Equal(new ContentCounts(1, 0, 2, 0), counts);
        Assert.Equal(["Years", "Students", "Lockers", "Assignments"], typeof(ContentCounts).GetProperties().Select(p => p.Name)); // counts only: no name can be read from it
    }

    [Fact]
    [Trait("spec", Spec + ": Verificación previa de la copia (Copia de una versión anterior)")]
    public async Task A_table_the_schema_of_an_older_version_does_not_have_counts_as_none()
    {
        var path = _dir.File("old.db");
        await using (var old = new ArcaDbContext(path, _key, create: true))
        {
            await old.GetService<IMigrator>().MigrateAsync("20260924104538_InitialCreate"); // before students and assignments existed
        }

        var counts = await DatabaseInspector.CountAsync(path, _key);

        Assert.Equal(new ContentCounts(0, 0, 0, 0), counts);
    }

    [Fact]
    [Trait("spec", Spec + ": Verificación previa de la copia (Copia de una versión anterior, Copia de una versión más nueva)")]
    public async Task The_schema_of_a_file_is_the_same_older_or_newer_than_the_one_this_version_knows()
    {
        var same = await CreateAsync("same.db");
        var old = _dir.File("old.db");
        await using (var context = new ArcaDbContext(old, _key, create: true))
        {
            await context.GetService<IMigrator>().MigrateAsync("20260929200520_Identitat");
        }

        var newer = await CreateAsync("newer.db");
        await using (var context = new ArcaDbContext(newer, _key))
        {
            await context.Database.ExecuteSqlRawAsync("INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('29990101000000_FromTheFuture', '99.0.0')");
        }

        Assert.Equal(SchemaRelation.Same, await DatabaseInspector.ClassifyAsync(same, _key));
        Assert.Equal(SchemaRelation.Older, await DatabaseInspector.ClassifyAsync(old, _key));
        Assert.Equal(SchemaRelation.Newer, await DatabaseInspector.ClassifyAsync(newer, _key));
    }

    [Fact]
    [Trait("spec", Spec + ": Verificación previa de la copia (Fichero que no es una copia)")]
    public async Task A_file_that_is_not_a_database_or_a_database_of_another_application_is_refused()
    {
        var text = _dir.File("notes.txt");
        await File.WriteAllTextAsync(text, "això no és una base de dades");
        var other = await CreateAsync("other.db");
        await using (var context = new ArcaDbContext(other, _key))
        {
            await context.Database.ExecuteSqlRawAsync("PRAGMA application_id = 12345"); // an encrypted database that is not ARCA's
        }

        Assert.Equal("Storage.Unreadable", (await DatabaseVerifier.VerifyAsync(text, _key))!.Code);
        Assert.Equal("Storage.NotArcaDatabase", (await DatabaseVerifier.VerifyAsync(other, _key))!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Contraseña de la copia (Contraseña incorrecta)")]
    public async Task A_key_that_does_not_open_the_file_is_refused_as_unreadable()
    {
        var path = await CreateAsync();

        var refused = await DatabaseVerifier.VerifyAsync(path, TestKeys.FromSeed("another"));

        Assert.Equal("Storage.Unreadable", refused!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Verificación previa de la copia (Copia dañada)")]
    public async Task A_truncated_copy_is_refused_as_damaged()
    {
        var path = await CreateAsync();
        var damaged = _dir.File("damaged.db");
        var bytes = await File.ReadAllBytesAsync(path);
        await File.WriteAllBytesAsync(damaged, bytes[..(bytes.Length / 2)]);

        var refused = await DatabaseVerifier.VerifyAsync(damaged, _key);

        Assert.NotNull(refused);
    }
}
