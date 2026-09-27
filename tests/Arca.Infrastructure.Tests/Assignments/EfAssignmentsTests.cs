// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Common;
using Arca.Application.Lockers.AddLocker;
using Arca.Application.SchoolYears.CreateAcademicYear;
using Arca.Application.Storage;
using Arca.Application.Students;
using Arca.Application.Students.AddStudent;
using Arca.Application.Students.RetireStudent;
using Arca.Application.Zones.CreateZone;
using Arca.Domain.Assignments;
using Arca.Domain.Students;
using Arca.Infrastructure.Inventory;
using Arca.Infrastructure.Storage;
using Arca.Infrastructure.Tests.Storage;
using Arca.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Arca.Infrastructure.Tests.Assignments;

/// <summary>The school years, students, catalogue and assignments over a real encrypted database: indexes, transactions and migration.</summary>
public sealed class EfAssignmentsTests : IDisposable
{
    const string Spec = "alumnes-i-assignacions";

    readonly TempDirectory _dir = new();
    readonly DatabaseKey _key = TestKeys.FromSeed("assignments");
    readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 27, 9, 0, 0, TimeSpan.Zero));
    string? _path;

    public void Dispose() => _dir.Dispose();

    string Path => _path ??= _dir.File("arca.db");

    async Task<EfInventory> NewDatabaseAsync()
    {
        var created = await ArcaDatabase.CreateAsync(Path, _key);
        Assert.True(created.IsSuccess);
        await created.Value!.DisposeAsync();
        return Open();
    }

    EfInventory Open() => new(() => new ArcaDbContext(Path, _key));

    ArcaDbContext Raw() => new(Path, _key);

    static AssignmentServices Services(EfInventory store) => new(
        store.Assignments, store.Students, store.Lockers, store.Zones, store.Enrollments, store.Years, store.StudentEvents, store.Events, [], [], []);

    async Task<Guid> YearAsync(EfInventory store, int startYear = 2026) =>
        (await new CreateAcademicYearHandler(store.Years, store)
            .HandleAsync(new CreateAcademicYearRequest(new DateOnly(startYear, 9, 1), new DateOnly(startYear + 1, 6, 30)), default)).Value!.Id;

    async Task<Guid> StudentAsync(EfInventory store, string first, string last, string email)
    {
        var occupancy = new AssignmentOccupancy(store.Assignments, store.Lockers);
        var added = await new AddStudentHandler(store.Students, store.Enrollments, store.Catalog, store.Years, store.StudentEvents, occupancy, store, _clock)
            .HandleAsync(new AddStudentRequest(first, last, email, "1r ESO", "A", ConfirmNewValues: true), default);
        return added.Value!.Student!.Id;
    }

    async Task<Guid> ZoneAsync(EfInventory store, string name) =>
        (await new CreateZoneHandler(store.Zones, store).HandleAsync(new CreateZoneRequest(name), default)).Value!.Id;

    async Task<Guid> LockerAsync(EfInventory store, int number, Guid zone)
    {
        _clock.Advance(TimeSpan.FromMinutes(1));
        var added = await new AddLockerHandler(store.Lockers, store.Zones, store.Events, store, _clock).HandleAsync(new AddLockerRequest(number, zone), default);
        return added.Value!.Id;
    }

    async Task<Guid> AssignedLockerAsync(EfInventory store, Guid student, Guid locker)
    {
        _clock.Advance(TimeSpan.FromMinutes(1));
        var assigned = await new AssignLockerHandler(Services(store), store, _clock).HandleAsync(new AssignLockerRequest(student, locker), default);
        Assert.True(assigned.IsSuccess, assigned.Error?.Code);
        return locker;
    }

    // --- Schema ---

    [Fact]
    [Trait("spec", Spec + "/assignacions: Asignación única (Índice único por curso, alumno y taquilla)")]
    public async Task The_migration_creates_the_tables_and_the_model_has_no_changes_left_to_migrate()
    {
        await NewDatabaseAsync();
        await using var context = Raw();

        var tables = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'").ToListAsync();

        Assert.Contains("AcademicYears", tables);
        Assert.Contains("Students", tables);
        Assert.Contains("Levels", tables);
        Assert.Contains("Groups", tables);
        Assert.Contains("Enrollments", tables);
        Assert.Contains("Assignments", tables);
        Assert.Contains("StudentEvents", tables);
        Assert.False(context.Database.HasPendingModelChanges());
    }

    // --- Unique indexes ---

    [Fact]
    [Trait("spec", Spec + "/assignacions: Asignación única (Alumno con dos asignaciones vigentes)")]
    public async Task The_index_rejects_two_current_assignments_of_the_same_student_even_without_the_domain_rule()
    {
        var store = await NewDatabaseAsync();
        var year = await YearAsync(store);
        var student = await StudentAsync(store, "Marta", "Puig", "marta@example.com");
        var zone = await ZoneAsync(store, "Planta 1");
        var first = await LockerAsync(store, 1, zone);
        var second = await LockerAsync(store, 2, zone);
        await using var context = Raw();

        context.Add(new Assignment(Guid.NewGuid(), student, first, year, _clock.UtcNow, null, null, null));
        await context.SaveChangesAsync();
        context.Add(new Assignment(Guid.NewGuid(), student, second, year, _clock.UtcNow, null, null, null));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    [Trait("spec", Spec + "/assignacions: Asignación única (Taquilla con dos asignaciones vigentes)")]
    public async Task The_index_rejects_two_current_assignments_of_the_same_locker_even_without_the_domain_rule()
    {
        var store = await NewDatabaseAsync();
        var year = await YearAsync(store);
        var first = await StudentAsync(store, "Marta", "Puig", "marta@example.com");
        var second = await StudentAsync(store, "Jordi", "Vidal", "jordi@example.com");
        var zone = await ZoneAsync(store, "Planta 1");
        var locker = await LockerAsync(store, 1, zone);
        await using var context = Raw();

        context.Add(new Assignment(Guid.NewGuid(), first, locker, year, _clock.UtcNow, null, null, null));
        await context.SaveChangesAsync();
        context.Add(new Assignment(Guid.NewGuid(), second, locker, year, _clock.UtcNow, null, null, null));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    [Trait("spec", Spec + "/alumnes: Correo como identificador único (Correo repetido)")]
    public async Task The_index_rejects_two_students_with_the_same_email_even_without_the_domain_rule()
    {
        await NewDatabaseAsync();
        await using var context = Raw();
        context.Add(new Student(Guid.NewGuid(), "Marta", "Puig", "marta@example.com", "marta puig", null, null));
        await context.SaveChangesAsync();
        context.Add(new Student(Guid.NewGuid(), "Marta", "Altra", "marta@example.com", "marta altra", null, null));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    // --- Use cases end to end ---

    [Fact]
    [Trait("spec", Spec + "/assignacions: Un único caso de uso para asignar (Asignación correcta)")]
    public async Task What_a_year_a_student_and_an_assignment_save_is_still_there_after_closing_and_opening_the_database_again()
    {
        var store = await NewDatabaseAsync();
        var year = await YearAsync(store);
        var student = await StudentAsync(store, "Marta", "Puig", "marta@example.com");
        var zone = await ZoneAsync(store, "Planta 1");
        var locker = await LockerAsync(store, 1, zone);
        await AssignedLockerAsync(store, student, locker);
        SqliteConnection.ClearAllPools();

        var reopened = Open();
        var current = await reopened.Assignments.GetCurrentOfStudentAsync(student, default);

        Assert.NotNull(current);
        Assert.Equal(locker, current!.LockerId);
        Assert.Equal(year, current.YearId);
        Assert.Contains(await reopened.StudentEvents.ListAsync(student, default), e => e.Type == StudentEventTypes.AssignmentOpened);
    }

    // --- Atomicity ---

    sealed class FailingClosedHandler : IAssignmentClosedHandler
    {
        public Task HandleAsync(AssignmentHookContext context, CancellationToken ct) => throw new InvalidOperationException("hook failed");
    }

    [Fact]
    [Trait("spec", Spec + "/assignacions: Ganchos de ciclo de vida (Gancho de baja que falla)")]
    public async Task If_a_closed_assignment_hook_fails_the_retirement_and_the_closing_are_undone_in_the_database()
    {
        var store = await NewDatabaseAsync();
        await YearAsync(store);
        var student = await StudentAsync(store, "Marta", "Puig", "marta@example.com");
        var zone = await ZoneAsync(store, "Planta 1");
        var locker = await LockerAsync(store, 1, zone);
        await AssignedLockerAsync(store, student, locker);
        var services = new AssignmentServices(
            store.Assignments, store.Students, store.Lockers, store.Zones, store.Enrollments, store.Years, store.StudentEvents, store.Events,
            [], [], [new FailingClosedHandler()]);
        var retire = new RetireStudentHandler(
            store.Students, store.Enrollments, store.Catalog, store.Years, store.StudentEvents,
            new AssignmentOccupancy(store.Assignments, store.Lockers), services, [], store, _clock);

        await Assert.ThrowsAsync<InvalidOperationException>(() => retire.HandleAsync(new RetireStudentRequest(student, "Trasllat"), default));

        await using var context = Raw();
        Assert.Null((await context.Set<Student>().SingleAsync(s => s.Id == student)).RetiredAtUtc);
        Assert.NotNull(await context.Set<Assignment>().SingleAsync(a => a.StudentId == student && a.EndedAtUtc == null));
    }

    // --- Migration ---

    [Fact]
    [Trait("spec", Spec + "/assignacions: Reserva para un alumne (Migración con reservas existentes)")]
    public async Task Migrating_an_older_database_keeps_an_existing_reservation_with_no_student()
    {
        var zoneId = Guid.NewGuid();
        var lockerId = Guid.NewGuid();
        await using (var context = new ArcaDbContext(Path, _key, create: true))
        {
            await ((IInfrastructure<IServiceProvider>)context).GetService<IMigrator>().MigrateAsync("20260925202352_TaquillesIZones");
            await context.Database.ExecuteSqlAsync($"INSERT INTO Zones (Id, Name, NameKey, IsActive) VALUES ({zoneId}, 'Planta 1', 'planta 1', 1)");
            await context.Database.ExecuteSqlAsync(
                $"INSERT INTO Lockers (Id, Number, ZoneId, IsReserved, ReservationNote, RetiredAtUtc) VALUES ({lockerId}, 1, {zoneId}, 1, 'Professorat', NULL)");
        }

        SqliteConnection.ClearAllPools();
        await using (var context = new ArcaDbContext(Path, _key))
        {
            await context.Database.MigrateAsync();
        }

        var store = Open();
        var locker = await store.Lockers.GetAsync(lockerId, default);

        Assert.NotNull(locker);
        Assert.True(locker!.IsReserved);
        Assert.Equal("Professorat", locker.ReservationNote);
        Assert.Null(locker.ReservedForStudentId);
    }
}
