// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Charges;
using Arca.Application.Charges.WaiveChargesInBulk;
using Arca.Application.GlobalState;
using Arca.Application.ConceptAmounts.SetConceptAmounts;
using Arca.Application.Lockers.AddLocker;
using Arca.Application.SchoolYears.CreateAcademicYear;
using Arca.Application.Storage;
using Arca.Application.Students;
using Arca.Application.Students.AddStudent;
using Arca.Application.Students.RetireStudent;
using Arca.Application.Zones.CreateZone;
using Arca.Domain.Charges;
using Arca.Domain.Common;
using Arca.Domain.ConceptAmounts;
using Arca.Infrastructure.Inventory;
using Arca.Infrastructure.Storage;
using Arca.Infrastructure.Tests.Storage;
using Arca.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Arca.Infrastructure.Tests.Charges;

/// <summary>The amounts and the charges over a real encrypted database: indexes, the hooks inside the assignment's transaction, and atomicity.</summary>
public sealed class EfChargesTests : IDisposable
{
    const string Spec = "pagaments";

    readonly TempDirectory _dir = new();
    readonly DatabaseKey _key = TestKeys.FromSeed("charges");
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

    AssignmentServices Services(EfInventory store) => new(
        store.Assignments, store.Students, store.Lockers, store.Zones, store.Enrollments, store.Years, store.StudentEvents, store.Events,
        [new ChargeGenerationGuard(store.ConceptAmounts, store.Charges)],
        [new ChargeGenerationHandler(store.ConceptAmounts, store.Charges, store.ChargeEvents, _clock)], []);

    async Task<Guid> YearAsync(EfInventory store) =>
        (await new CreateAcademicYearHandler(store.Years, store)
            .HandleAsync(new CreateAcademicYearRequest(new DateOnly(2026, 9, 1), new DateOnly(2027, 6, 30)), default)).Value!.Id;

    async Task SetAmountsAsync(EfInventory store, Guid year) =>
        Assert.True((await new SetConceptAmountsHandler(store.Years, store.ConceptAmounts, store.ConceptAmountEvents, store, _clock)
            .HandleAsync(new SetConceptAmountsRequest(year, 50m, 20m, 10m), default)).IsSuccess);

    async Task<Guid> StudentAsync(EfInventory store, string first, string last)
    {
        var occupancy = new AssignmentOccupancy(store.Assignments, store.Lockers);
        var added = await new AddStudentHandler(store.Students, store.Enrollments, store.Catalog, store.Years, store.StudentEvents, occupancy, store, _clock)
            .HandleAsync(new AddStudentRequest(first, last, first.ToLowerInvariant() + "@example.com", "1r ESO", "A", ConfirmNewValues: true), default);
        return added.Value!.Student!.Id;
    }

    async Task<Guid> ZoneAsync(EfInventory store) =>
        (await new CreateZoneHandler(store.Zones, store).HandleAsync(new CreateZoneRequest("Planta 1"), default)).Value!.Id;

    async Task<Guid> LockerAsync(EfInventory store, int number, Guid zone)
    {
        _clock.Advance(TimeSpan.FromMinutes(1));
        return (await new AddLockerHandler(store.Lockers, store.Zones, store.Events, store, _clock).HandleAsync(new AddLockerRequest(number, zone), default)).Value!.Id;
    }

    async Task<Result<AssignLockerResult>> AssignAsync(EfInventory store, Guid student, Guid locker)
    {
        _clock.Advance(TimeSpan.FromMinutes(1));
        return await new AssignLockerHandler(Services(store), store, _clock).HandleAsync(new AssignLockerRequest(student, locker), default);
    }

    Task<Result<StudentDetail>> RetireAsync(EfInventory store, Guid student) =>
        new RetireStudentHandler(
            store.Students, store.Enrollments, store.Catalog, store.Years, store.StudentEvents, new AssignmentOccupancy(store.Assignments, store.Lockers),
            Services(store), [new DepositLifecycleHandler(store.Charges, store.ChargeEvents, _clock)], store, _clock)
            .HandleAsync(new RetireStudentRequest(student, "Trasllat"), default);

    static Charge NewCharge(Guid student, Guid year, ChargeConcept concept) =>
        Charge.Create(Guid.NewGuid(), student, concept, year, Money.FromCents(2000), DateTimeOffset.UtcNow).Charge;

    // --- Schema ---

    [Fact]
    [Trait("spec", Spec + "/cobraments: Cargo por alumno y concepto (Importe fijado al crear)")]
    public async Task The_migration_creates_the_tables_and_the_model_has_no_changes_left_to_migrate()
    {
        await NewDatabaseAsync();
        await using var context = Raw();

        var tables = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'").ToListAsync();

        Assert.Contains("ConceptAmounts", tables);
        Assert.Contains("ConceptAmountEvents", tables);
        Assert.Contains("Charges", tables);
        Assert.Contains("ChargeEvents", tables);
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    [Trait("spec", Spec + "/cobraments: Generación de la cuota (Primera asignación del curso)")]
    public async Task The_index_rejects_a_second_fee_of_the_same_student_and_year_even_without_the_domain_rule()
    {
        var store = await NewDatabaseAsync();
        var year = await YearAsync(store);
        var student = await StudentAsync(store, "Marta", "Puig");
        await using var context = Raw();

        context.Add(NewCharge(student, year, ChargeConcept.Fee));
        await context.SaveChangesAsync();
        context.Add(NewCharge(student, year, ChargeConcept.Fee));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    [Trait("spec", Spec + "/fianca: Una fianza vigente por alumno (Alumno con fianza vigente)")]
    public async Task The_index_rejects_a_second_current_deposit_but_allows_one_after_the_first_was_returned()
    {
        var store = await NewDatabaseAsync();
        var year = await YearAsync(store);
        var student = await StudentAsync(store, "Marta", "Puig");
        var first = NewCharge(student, year, ChargeConcept.Deposit);
        await using (var context = Raw())
        {
            context.Add(first);
            await context.SaveChangesAsync();
            context.Add(NewCharge(student, year, ChargeConcept.Deposit));
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        await using (var context = Raw())
        {
            var stored = await context.Set<Charge>().SingleAsync(c => c.Id == first.Id);
            var today = new DateOnly(2026, 9, 27);
            Assert.True(stored.MarkPaid(null, today, _clock.UtcNow).IsSuccess);
            Assert.True(stored.MarkReturnDue(_clock.UtcNow).IsSuccess);
            Assert.True(stored.MarkReturned(null, null, today, _clock.UtcNow).IsSuccess);
            await context.SaveChangesAsync();
            context.Add(NewCharge(student, year, ChargeConcept.Deposit));

            await context.SaveChangesAsync();
        }
    }

    [Fact]
    [Trait("spec", Spec + "/conceptes-de-cobrament: Un importe por concepto y curso")]
    public async Task The_index_rejects_two_amounts_of_the_same_concept_and_year()
    {
        var store = await NewDatabaseAsync();
        var year = await YearAsync(store);
        await using var context = Raw();

        context.Add(ConceptAmount.Create(Guid.NewGuid(), year, ChargeConcept.Fee, 50m, _clock.UtcNow).Value!.Amount);
        await context.SaveChangesAsync();
        context.Add(ConceptAmount.Create(Guid.NewGuid(), year, ChargeConcept.Fee, 60m, _clock.UtcNow).Value!.Amount);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    // --- Round trip ---

    [Fact]
    [Trait("spec", Spec + "/fianca: Devolución de la fianza (Devolución correcta)")]
    public async Task A_returned_deposit_its_exact_amount_and_its_history_survive_closing_and_opening_the_database()
    {
        var store = await NewDatabaseAsync();
        var year = await YearAsync(store);
        var student = await StudentAsync(store, "Marta", "Puig");
        var deposit = Charge.Create(Guid.NewGuid(), student, ChargeConcept.Deposit, year, Money.FromCents(1999), _clock.UtcNow);
        var today = new DateOnly(2026, 9, 27);
        var events = new List<HistoryEvent> { deposit.Event };
        events.Add(deposit.Charge.MarkPaid(null, today, _clock.UtcNow).Value!);
        events.Add(deposit.Charge.MarkReturnDue(_clock.UtcNow).Value!);
        events.Add(deposit.Charge.MarkReturned(today.AddDays(-1), "En mà", today, _clock.UtcNow).Value!);
        await store.RunAsync(async ct =>
        {
            await store.Charges.AddAsync(deposit.Charge, ct);
            foreach (var change in events)
            {
                await store.ChargeEvents.AddAsync(change, ct);
            }

            return Result<bool>.Success(true);
        }, default);
        SqliteConnection.ClearAllPools();

        var reopened = Open();
        var stored = await reopened.Charges.GetAsync(deposit.Charge.Id, default);
        var history = await reopened.ChargeEvents.ListAsync(deposit.Charge.Id, default);

        Assert.Equal(19.99m, stored!.Amount.Amount);
        Assert.Equal((DepositReturnStatus.Returned, today.AddDays(-1), "En mà"), (stored.Return, stored.ReturnedOn, stored.ReturnNote));
        Assert.Equal(4, history.Count);
        Assert.Empty(await reopened.Charges.ListDepositsDueBackAsync(default));
        Assert.Empty(await reopened.Charges.ListPendingAsync(default));
    }

    // --- Transactions ---

    [Fact]
    [Trait("spec", Spec + "/cobraments: Generación de la cuota (Primera asignación del curso)")]
    public async Task Assigning_a_locker_saves_the_assignment_the_fee_and_the_deposit_together()
    {
        var store = await NewDatabaseAsync();
        var year = await YearAsync(store);
        await SetAmountsAsync(store, year);
        var student = await StudentAsync(store, "Marta", "Puig");
        var locker = await LockerAsync(store, 1, await ZoneAsync(store));

        var result = await AssignAsync(store, student, locker);

        Assert.True(result.IsSuccess);
        var charges = await store.Charges.ListByStudentAsync(student, default);
        Assert.Equivalent(new[] { ChargeConcept.Fee, ChargeConcept.Deposit }, charges.Select(c => c.Concept));
        Assert.Equal(2, charges.Count);
        Assert.All(charges, c => Assert.Equal(ChargeStatus.Pending, c.Status));
        Assert.Equal(2, (await store.Charges.ListPendingAsync(default)).Count);
    }

    [Fact]
    [Trait("spec", Spec + "/cobraments: Generación de la cuota (Importes sin definir)")]
    public async Task Without_the_years_amounts_nothing_is_assigned_and_no_charge_is_saved()
    {
        var store = await NewDatabaseAsync();
        await YearAsync(store);
        var student = await StudentAsync(store, "Marta", "Puig");
        var locker = await LockerAsync(store, 1, await ZoneAsync(store));

        var result = await AssignAsync(store, student, locker);

        Assert.False(result.IsSuccess);
        Assert.Null(await store.Assignments.GetCurrentOfStudentAsync(student, default));
        Assert.Empty(await store.Charges.ListByStudentAsync(student, default));
    }

    [Fact]
    [Trait("spec", Spec + "/cobraments: Generación de la cuota (Primera asignación del curso)")]
    public async Task If_saving_a_charge_event_fails_the_assignment_and_the_charges_are_undone()
    {
        var store = await NewDatabaseAsync();
        var year = await YearAsync(store);
        await SetAmountsAsync(store, year);
        var student = await StudentAsync(store, "Marta", "Puig");
        var locker = await LockerAsync(store, 1, await ZoneAsync(store));
        var services = new AssignmentServices(
            store.Assignments, store.Students, store.Lockers, store.Zones, store.Enrollments, store.Years, store.StudentEvents, store.Events,
            [], [new ChargeGenerationHandler(store.ConceptAmounts, store.Charges, new FailingEvents(store.ChargeEvents, allowed: 1), _clock)], []);

        await Assert.ThrowsAsync<IOException>(
            () => new AssignLockerHandler(services, store, _clock).HandleAsync(new AssignLockerRequest(student, locker), default));

        Assert.Null(await store.Assignments.GetCurrentOfStudentAsync(student, default));
        Assert.Empty(await store.Charges.ListByStudentAsync(student, default));
    }

    [Fact]
    [Trait("spec", Spec + "/fianca: Baja del alumno y fianza (Bajas masivas por importación)")]
    public async Task Retiring_students_with_paid_deposits_leaves_them_due_back_in_the_database()
    {
        var store = await NewDatabaseAsync();
        var year = await YearAsync(store);
        await SetAmountsAsync(store, year);
        var zone = await ZoneAsync(store);
        var students = new List<Guid>();
        foreach (var (name, number) in new[] { ("Ana", 1), ("Bru", 2), ("Cesc", 3) })
        {
            var student = await StudentAsync(store, name, "Cognom" + number);
            Assert.True((await AssignAsync(store, student, await LockerAsync(store, number, zone))).IsSuccess);
            var deposit = (await store.Charges.ListByStudentAsync(student, default)).Single(c => c.Concept == ChargeConcept.Deposit);
            await store.RunAsync(async ct =>
            {
                var loaded = (await store.Charges.GetAsync(deposit.Id, ct))!;
                await store.ChargeEvents.AddAsync(loaded.MarkPaid(null, new DateOnly(2026, 9, 27), _clock.UtcNow).Value!, ct);
                await store.Charges.UpdateAsync(loaded, ct);
                return Result<bool>.Success(true);
            }, default);
            students.Add(student);
        }

        foreach (var student in students)
        {
            Assert.True((await RetireAsync(store, student)).IsSuccess);
        }

        var due = await store.Charges.ListDepositsDueBackAsync(default);
        Assert.Equal(3, due.Count);
        Assert.All(due, d => Assert.Equal(ChargeStatus.Paid, d.Status));
    }

    [Fact]
    [Trait("spec", Spec + "/cobraments: Condonación en bloque (Condonación de varios cargos)")]
    public async Task A_failure_in_the_middle_of_a_bulk_waiver_leaves_every_charge_pending()
    {
        var store = await NewDatabaseAsync();
        var year = await YearAsync(store);
        var ids = new List<Guid>();
        foreach (var name in new[] { "Ana", "Bru", "Cesc", "Dolors" })
        {
            var student = await StudentAsync(store, name, "Cognom");
            var fee = NewCharge(student, year, ChargeConcept.Fee);
            await store.RunAsync(async ct =>
            {
                await store.Charges.AddAsync(fee, ct);
                return Result<bool>.Success(true);
            }, default);
            ids.Add(fee.Id);
        }

        var handler = new WaiveChargesInBulkHandler(store.Charges, new FailingEvents(store.ChargeEvents, allowed: 2), store, _clock);
        var request = new WaiveChargesInBulkRequest(ids, "Motiu");
        var plan = (await handler.AnalyzeAsync(request, null, default)).Value!;

        await Assert.ThrowsAsync<IOException>(() => handler.ApplyAsync(request, plan, null, default));

        Assert.Equal(4, (await store.Charges.ListPendingAsync(default)).Count);
        foreach (var id in ids)
        {
            Assert.Empty(await store.ChargeEvents.ListAsync(id, default));
        }
    }

    // --- Global state ---

    [Fact]
    [Trait("spec", "ui-shell/navegacio-i-cerca: Cabecera con el estado global (Curso activo)")]
    public async Task The_global_state_reads_the_year_and_the_pending_charges_from_the_encrypted_database()
    {
        var store = await NewDatabaseAsync();
        var handler = new GetGlobalStateHandler(store.Years, store.Charges);
        var empty = (await handler.HandleAsync(default)).Value!;
        Assert.False(empty.HasActiveYear);
        Assert.Equal(0, empty.PendingCharges);

        var year = await YearAsync(store);
        await SetAmountsAsync(store, year);
        var student = await StudentAsync(store, "Marta", "Puig");
        Assert.True((await AssignAsync(store, student, await LockerAsync(store, 1, await ZoneAsync(store)))).IsSuccess);
        SqliteConnection.ClearAllPools();

        var state = (await new GetGlobalStateHandler(Open().Years, Open().Charges).HandleAsync(default)).Value!;

        Assert.Equal("2026-2027", state.ActiveYear!.Name);
        Assert.Equal(2, state.PendingCharges); // the fee and the deposit
    }

    // --- Privacy ---

    [Fact]
    [Trait("spec", Spec + "/cobraments: Datos sensibles (sin rastro en el registro técnico)")]
    public async Task A_failure_while_saving_a_charge_with_a_reason_and_a_name_leaves_no_trace_in_the_technical_log()
    {
        var store = await NewDatabaseAsync();
        var year = await YearAsync(store);
        var student = await StudentAsync(store, "Núria", "Garcia");
        var fee = NewCharge(student, year, ChargeConcept.Fee);
        await store.RunAsync(async ct =>
        {
            await store.Charges.AddAsync(fee, ct);
            return Result<bool>.Success(true);
        }, default);
        var handler = new WaiveChargesInBulkHandler(store.Charges, new FailingEvents(store.ChargeEvents, allowed: 0), store, _clock);
        var request = new WaiveChargesInBulkRequest([fee.Id], "Beca per a la Núria Garcia, correu nuria@example.com");
        var plan = (await handler.AnalyzeAsync(request, null, default)).Value!;
        var logs = _dir.File("logs");
        Directory.CreateDirectory(logs);

        var error = await Assert.ThrowsAsync<IOException>(() => handler.ApplyAsync(request, plan, null, default));
        using (var log = new Arca.Infrastructure.Common.FileErrorLog(logs))
        {
            log.LogUnexpected(error, "WaiveChargesInBulk");
        }

        var text = string.Join("\n", Directory.GetFiles(logs, "arca-*.log").Select(File.ReadAllText));
        Assert.NotEmpty(text);
        foreach (var forbidden in new[] { "Núria", "Garcia", "Beca", "nuria@example.com" })
        {
            Assert.DoesNotContain(forbidden, text, StringComparison.Ordinal);
        }
    }

    // --- Migration ---

    [Fact]
    [Trait("spec", Spec + "/cobraments: Migración sin datos previos")]
    public async Task Migrating_a_database_from_before_pagaments_adds_the_tables_and_keeps_its_data()
    {
        var zoneId = Guid.NewGuid();
        await using (var context = new ArcaDbContext(Path, _key, create: true))
        {
            await ((IInfrastructure<IServiceProvider>)context).GetService<IMigrator>().MigrateAsync("20260927095753_AlumnesIAssignacions");
            await context.Database.ExecuteSqlAsync($"INSERT INTO Zones (Id, Name, NameKey, IsActive) VALUES ({zoneId}, 'Planta 1', 'planta 1', 1)");
        }

        SqliteConnection.ClearAllPools();
        await using (var context = new ArcaDbContext(Path, _key))
        {
            await context.Database.MigrateAsync();
        }

        var store = Open();

        Assert.NotNull(await store.Zones.GetAsync(zoneId, default));
        Assert.Empty(await store.Charges.ListPendingAsync(default));
    }

    /// <summary>Lets some events be saved and then fails, like a disk that gives out in the middle of a save.</summary>
    sealed class FailingEvents(IChargeEventRepository inner, int allowed) : IChargeEventRepository
    {
        int _left = allowed;

        public Task AddAsync(HistoryEvent change, CancellationToken ct) =>
            _left-- > 0 ? inner.AddAsync(change, ct) : throw new IOException("the disk failed while saving");

        public Task<IReadOnlyList<HistoryEvent>> ListAsync(Guid chargeId, CancellationToken ct) => inner.ListAsync(chargeId, ct);
    }
}
