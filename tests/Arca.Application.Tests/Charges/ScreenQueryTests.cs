// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.Json;
using Arca.Application.Catalog.ListCatalog;
using Arca.Application.ConceptAmounts.GetConceptAmountsHistory;
using Arca.Application.Localization;
using Arca.Application.SchoolYears.GetYearScreen;
using Arca.Application.Lockers;
using Arca.Application.Lockers.GetLockerScreen;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Zones.DeactivateZone;
using Arca.Application.Zones.ListZoneRows;
using Arca.Application.Lockers.ReserveLocker;
using Arca.Application.Lockers.MarkLockerOutOfService;
using Arca.Application.Lockers.RetireLocker;
using Arca.Application.Search;
using Arca.Application.Charges.GetChargeHistory;
using Arca.Application.Charges.GetStudentChargesScreen;
using Arca.Application.Charges.MarkChargePaid;
using Arca.Application.Charges.VoidCharge;
using Arca.Application.Students.GetStudentScreen;
using Arca.Application.Students.ListStudentRows;
using Arca.Application.Students.RetireStudent;
using Arca.Domain.Lockers;
using Xunit;

namespace Arca.Application.Tests.Charges;

public sealed class ScreenQueryTests
{
    const string Spec = "pantalles-de-domini/design";

    static async Task<PagamentsWorld> WorldAsync()
    {
        var world = new PagamentsWorld();
        await world.SeedAmountsAsync(await world.YearAsync(2026), 50m, 20m, 10m);
        return world;
    }

    ListLockerRowsHandler LockerRows(PagamentsWorld world) =>
        new(world.Store.Lockers, world.Store.Zones, world.Store.Assignments, world.Store.Students, world.Store.Charges);

    ListStudentRowsHandler StudentRows(PagamentsWorld world) =>
        new(world.Assignments.Students.Search, world.Store.Charges);

    // --- Lockers ---

    [Fact]
    [Trait("spec", Spec + ": D6 Consultas de lectura (taquilla con zona, estado visible, alumno y marca de deuda)")]
    public async Task The_locker_rows_come_composed_with_zone_status_holder_and_debt_and_the_retired_are_told_apart()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var occupied = await world.LockerAsync(1, zone);
        var broken = await world.LockerAsync(2, zone);
        var free = await world.LockerAsync(3, zone);
        var retired = await world.LockerAsync(4, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, occupied);
        await world.Assignments.Inventory.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(broken, OutOfServiceKind.Broken), default);
        await world.Assignments.Inventory.RetireLocker.HandleAsync(new RetireLockerRequest(retired), default);

        var listing = (await LockerRows(world).HandleAsync(default)).Value!;

        Assert.Equal([1, 2, 3, 4], listing.Rows.Select(r => r.Number));
        Assert.Equal(
            [LockerStatusView.Occupied, LockerStatusView.Broken, LockerStatusView.Free, LockerStatusView.Retired],
            listing.Rows.Select(r => r.Status));
        Assert.All(listing.Rows, r => Assert.Equal("Planta 1", r.ZoneName));
        var first = listing.Rows[0];
        Assert.Equal("Marta Puig", first.StudentName);
        Assert.True(first.HasDebt);
        Assert.Null(listing.Rows[2].StudentName);
        Assert.Equal((3, 1, 1, 1), (listing.Counters.Active, listing.Counters.Free, listing.Counters.Occupied, listing.Counters.Broken)); // the retired one is not counted
        Assert.Equal(LockerEmptyState.None, listing.EmptyState);
        Assert.Equal(free, listing.Rows[2].Id);
    }

    [Fact]
    [Trait("spec", Spec + ": D6 Consultas de lectura (estados vacíos)")]
    public async Task Without_zones_or_without_lockers_the_listing_says_which_one_is_missing()
    {
        var world = await WorldAsync();

        Assert.Equal(LockerEmptyState.NoZones, (await LockerRows(world).HandleAsync(default)).Value!.EmptyState);

        await world.ZoneAsync("Planta 1");
        Assert.Equal(LockerEmptyState.NoLockers, (await LockerRows(world).HandleAsync(default)).Value!.EmptyState);
    }

    [Fact]
    [Trait("spec", Spec + ": D6 Consultas de lectura (los objetos no llevan correo ni identificador)")]
    public async Task The_lists_carry_no_email_and_no_identifier_of_any_student()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, locker);

        var lockers = (await LockerRows(world).HandleAsync(default)).Value!;
        var students = (await StudentRows(world).HandleAsync(default)).Value!;

        foreach (var json in new[] { JsonSerializer.Serialize(lockers), JsonSerializer.Serialize(students) })
        {
            Assert.DoesNotContain("marta@example.com", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Email", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(student.Id.ToString(), JsonSerializer.Serialize(lockers), StringComparison.OrdinalIgnoreCase); // a locker row never names its student's identity
        }
    }

    // --- Students ---

    [Fact]
    [Trait("spec", Spec + ": D6 Consultas de lectura (alumno con taquilla y estado de pago)")]
    public async Task The_student_rows_carry_locker_and_what_is_owed_and_keep_the_retired_ones()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(7, zone);
        var marta = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        var pau = await world.StudentAsync("Pau", "Alsina", "pau@example.com");
        var oriol = await world.StudentAsync("Oriol", "Zamora", "oriol@example.com");
        await world.AssignAsync(marta.Id, locker);
        await world.Assignments.Students.Retire.HandleAsync(new RetireStudentRequest(oriol.Id, "Ha marxat"), default);

        var listing = (await StudentRows(world).HandleAsync(default)).Value!;

        Assert.Equal(["Alsina", "Puig", "Zamora"], listing.Rows.Select(r => r.LastName));
        var rowOfMarta = listing.Rows.Single(r => r.Id == marta.Id);
        Assert.Equal(7, rowOfMarta.LockerNumber);
        Assert.True(rowOfMarta.HasDebt);
        Assert.Equal(world.ChargesOf(marta.Id).Where(c => c.CountsAsDebt).Sum(c => c.Amount.Amount), rowOfMarta.PendingTotal);
        Assert.False(listing.Rows.Single(r => r.Id == pau.Id).HasDebt);
        Assert.True(listing.Rows.Single(r => r.Id == oriol.Id).IsRetired);
        Assert.Equal(2, listing.Counters.Active);
    }

    [Fact]
    [Trait("spec", Spec + ": D6 Consultas de lectura (sin curso activo)")]
    public async Task Without_an_active_year_the_student_rows_fail_with_the_error_the_screen_explains()
    {
        var world = new PagamentsWorld();

        var result = await StudentRows(world).HandleAsync(default);

        Assert.False(result.IsSuccess);
        Assert.Equal("SchoolYears.NoActiveYear", result.Error!.Code);
    }

    // --- Catalogue ---

    [Fact]
    [Trait("spec", Spec + ": D5 Selectores compartidos")]
    public async Task The_catalogue_lists_levels_and_groups_by_name_as_people_read_them()
    {
        var world = await WorldAsync();
        await world.StudentAsync("Marta", "Puig", "marta@example.com");

        var catalog = (await new ListCatalogHandler(world.Store.Catalog).HandleAsync(default)).Value!;

        Assert.NotEmpty(catalog.Levels);
        Assert.Equal(catalog.Levels.Order(Arca.Domain.Common.TextComparer.Comparer), catalog.Levels);
    }

    // --- Lockers and zones: the reason of each operation ---

    [Fact]
    [Trait("spec", "pantalles-de-domini/design: D2 Acciones como objetos compartidos (los motivos salen de Application)")]
    public async Task A_locker_gives_the_reason_of_each_operation_from_the_domain_rules_themselves()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var free = await world.LockerAsync(1, zone);
        var busy = await world.LockerAsync(2, zone);
        var broken = await world.LockerAsync(3, zone);
        var reserved = await world.LockerAsync(4, zone);
        var retired = await world.LockerAsync(5, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, busy);
        var inventory = world.Assignments.Inventory;
        await inventory.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(broken, OutOfServiceKind.Broken), default);
        await inventory.ReserveLocker.HandleAsync(new ReserveLockerRequest(reserved, "Professorat"), default);
        await inventory.RetireLocker.HandleAsync(new RetireLockerRequest(retired), default);
        var handler = new GetLockerScreenHandler(LockerRows(world), world.Store.Lockers, new Arca.Application.Assignments.AssignmentOccupancy(world.Store.Assignments, world.Store.Lockers), world.Clock);

        async Task<LockerScreenDetail> Of(Guid id) => (await handler.HandleAsync(new GetLockerScreenRequest(id), default)).Value!;

        var onFree = await Of(free);
        Assert.Null(onFree.ReserveBlocked);
        Assert.Null(onFree.RetireBlocked);
        Assert.Null(onFree.BrokenBlocked);
        Assert.Equal("Lockers.NotReserved", onFree.RemoveReservationBlocked!.Code);
        Assert.Equal("Lockers.NotOutOfService", onFree.RestoreBlocked!.Code);

        var onBusy = await Of(busy);
        Assert.Equal("Lockers.NotFree", onBusy.ReserveBlocked!.Code);
        Assert.Equal("Lockers.HasAssignment", onBusy.RetireBlocked!.Code);
        Assert.Null(onBusy.BrokenBlocked); // allowed: the decision about the student comes next

        var onBroken = await Of(broken);
        Assert.Equal("Lockers.AlreadyOutOfService", onBroken.BrokenBlocked!.Code);
        Assert.Null(onBroken.MaintenanceBlocked); // changing the kind is allowed
        Assert.Null(onBroken.RestoreBlocked);

        var onReserved = await Of(reserved);
        Assert.Null(onReserved.RemoveReservationBlocked);
        Assert.Equal("Lockers.HasReservation", onReserved.RetireBlocked!.Code);

        var onRetired = await Of(retired);
        Assert.Equal("Lockers.Retired", onRetired.EditBlocked!.Code);
        Assert.Equal(LockerStatusView.Retired, onRetired.Row.Status);
    }

    [Fact]
    [Trait("spec", "pantalles-de-domini/pantalles-taquilles-i-zones: Vista de zonas")]
    public async Task A_zone_gives_the_reason_it_cannot_be_deactivated_or_deleted_and_an_empty_one_can_be_both()
    {
        var world = await WorldAsync();
        var used = await world.ZoneAsync("Planta 1");
        var empty = await world.ZoneAsync("Planta 2");
        var idle = await world.ZoneAsync("Planta 3");
        await world.LockerAsync(1, used);
        await world.Assignments.Inventory.DeactivateZone.HandleAsync(new DeactivateZoneRequest(idle), default);
        var handler = new ListZoneRowsHandler(world.Store.Zones, world.Store.Lockers);

        var rows = (await handler.HandleAsync(default)).Value!;

        Assert.Equal(["Planta 1", "Planta 2", "Planta 3"], rows.Select(r => r.Name));
        var first = rows[0];
        Assert.Equal(1, first.ActiveLockers);
        Assert.Equal("Zones.HasActiveLockers", first.DeactivationBlocked!.Code);
        Assert.Equal("Zones.HasHistory", first.DeletionBlocked!.Code);
        var second = rows.Single(r => r.Id == empty);
        Assert.Null(second.DeactivationBlocked);
        Assert.Null(second.DeletionBlocked);
        var third = rows.Single(r => r.Id == idle);
        Assert.False(third.IsActive);
        Assert.Null(third.ReactivationBlocked);
        Assert.Null(third.DeactivationBlocked); // it is already inactive: there is nothing to refuse
    }

    // --- Students: the reason of each operation ---

    GetStudentScreenHandler StudentScreen(PagamentsWorld world) => new(
        world.Store.Students, world.Store.Enrollments, world.Store.Catalog, world.Store.Years, world.Assignments.Students.StudentLockers,
        world.Store.Charges, world.Clock);

    [Fact]
    [Trait("spec", "pantalles-de-domini/pantalles-alumnes-i-assignacions: Ficha del alumno; Asignar; Cambiar y liberar; Dar de baja y reactivar")]
    public async Task A_student_gives_the_reason_each_operation_is_refused_with_their_locker_and_debt()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(7, zone);
        var without = await world.StudentAsync("Pau", "Alsina", "pau@example.com");
        var with = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        var gone = await world.StudentAsync("Oriol", "Zamora", "oriol@example.com");
        await world.AssignAsync(with.Id, locker);
        await world.Assignments.Students.Retire.HandleAsync(new RetireStudentRequest(gone.Id, "Ha marxat"), default);
        async Task<StudentScreenDetail> Of(Guid id) => (await StudentScreen(world).HandleAsync(new GetStudentScreenRequest(id), default)).Value!;

        var idle = await Of(without.Id);
        Assert.Null(idle.AssignBlocked);
        Assert.Null(idle.RetireBlocked);
        Assert.Equal("Assignments.NoAssignment", idle.ChangeBlocked!.Code);
        Assert.Equal("Assignments.NoAssignment", idle.ReleaseBlocked!.Code);
        Assert.Equal("Students.AlreadyActive", idle.ReactivateBlocked!.Code);
        Assert.False(idle.HasDebt);

        var holder = await Of(with.Id);
        Assert.Equal("Assignments.StudentHasLocker", holder.AssignBlocked!.Code);
        Assert.Null(holder.ChangeBlocked);
        Assert.Null(holder.ReleaseBlocked);
        Assert.Equal(locker, holder.LockerId);
        Assert.True(holder.HasDebt);

        var retired = await Of(gone.Id);
        Assert.Equal("Students.AlreadyRetired", retired.RetireBlocked!.Code);
        Assert.Null(retired.ReactivateBlocked);
        Assert.Equal("Assignments.StudentRetired", retired.AssignBlocked!.Code);
    }

    [Fact]
    [Trait("spec", "pantalles-de-domini/pantalles-alumnes-i-assignacions: Alumnos sin curso activo")]
    public async Task Without_an_active_year_assigning_and_reactivating_are_refused_for_that_reason()
    {
        var world = new PagamentsWorld();
        var year = await world.YearAsync(2026);
        var student = await world.StudentAsync("Pau", "Alsina", "pau@example.com");
        await world.Store.Years.RemoveAsync((await world.Store.Years.GetAsync(year, default))!, default); // no year is active any more

        var detail = (await StudentScreen(world).HandleAsync(new GetStudentScreenRequest(student.Id), default)).Value!;

        Assert.Equal("SchoolYears.NoActiveYear", detail.AssignBlocked!.Code);
        Assert.Equal("SchoolYears.NoActiveYear", detail.ChangeBlocked!.Code);
    }

    // --- Charges: the reason of each operation and the history ---

    [Fact]
    [Trait("spec", "pantalles-de-domini/pantalles-cobraments: Operaciones sobre un cargo; Cargos de un alumno")]
    public async Task The_charges_of_a_student_give_the_reason_each_operation_is_refused_and_their_standing()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, locker);
        var fee = world.ChargesOf(student.Id).Single(c => c.Concept == Domain.ConceptAmounts.ChargeConcept.Fee);
        var deposit = world.ChargesOf(student.Id).Single(c => c.Concept == Domain.ConceptAmounts.ChargeConcept.Deposit);
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(fee.Id), default);
        await world.Void.HandleAsync(new VoidChargeRequest(deposit.Id, "Duplicat"), default);
        var previous = await world.YearAsync(2025);
        var prior = await world.SeedChargeAsync(student.Id, Domain.ConceptAmounts.ChargeConcept.Fee, previous, 45m);
        var handler = new GetStudentChargesScreenHandler(world.Store.Charges, world.Store.Students, world.Store.Years, world.Store.ConceptAmounts, world.Clock);

        var screen = (await handler.HandleAsync(new GetStudentChargesScreenRequest(student.Id), default)).Value!;

        Assert.False(screen.UpToDate);
        Assert.Equal(45m, screen.PendingTotal);
        Assert.Equal(10m, screen.KeyReplacementAmount);
        Assert.Equal("2026-2027", screen.ActiveYearName);
        var paid = screen.Lines.Single(l => l.Id == fee.Id);
        Assert.Equal("Paid", paid.Status);
        Assert.Equal("Charges.InvalidStatus", paid.PayBlocked!.Code);
        Assert.Null(paid.RevertBlocked);
        var voided = screen.Lines.Single(l => l.Id == deposit.Id);
        Assert.NotNull(voided.RevertBlocked);
        Assert.NotNull(voided.VoidBlocked);
        var old = screen.Lines.Single(l => l.Id == prior.Id);
        Assert.False(old.IsActiveYear);
        Assert.Null(old.PayBlocked); // charges of previous years are managed like the active one
        Assert.Null(old.VoidBlocked);
        Assert.Equal(prior.Id, screen.Lines[^1].Id); // the active year first, the previous ones after
    }

    [Fact]
    [Trait("spec", "pantalles-de-domini/pantalles-cobraments: Historial del cargo (Historial visible)")]
    public async Task The_history_of_a_charge_lists_each_change_with_its_states_reason_and_amounts_most_recent_first()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, locker);
        var fee = world.ChargesOf(student.Id).Single(c => c.Concept == Domain.ConceptAmounts.ChargeConcept.Fee);
        world.Clock.Advance(TimeSpan.FromHours(1));
        await world.Adjust.HandleAsync(new Arca.Application.Charges.AdjustChargeAmount.AdjustChargeAmountRequest(fee.Id, 40m, "Descompte"), default);
        world.Clock.Advance(TimeSpan.FromHours(1));
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(fee.Id), default);
        var handler = new GetChargeHistoryHandler(world.Store.Charges, world.Store.ChargeEvents, new ResxLocalizer());

        var lines = (await handler.HandleAsync(new GetChargeHistoryRequest(fee.Id), default)).Value!;

        Assert.Equal(3, lines.Count); // created, amount adjusted, paid
        Assert.Contains("pagat", lines[0].Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("de 50,00", lines[1].Text, StringComparison.Ordinal);
        Assert.Contains("a 40,00", lines[1].Text, StringComparison.Ordinal);
        Assert.Contains("Motiu: Descompte", lines[1].Text, StringComparison.Ordinal);
        Assert.Contains("50,00", lines[2].Text, StringComparison.Ordinal);
    }

    // --- Courses ---

    [Fact]
    [Trait("spec", "pantalles-de-domini/pantalles-curs-i-imports: Activar un curso, Eliminar un curso vacío, Aviso del efecto")]
    public async Task A_year_says_why_it_cannot_be_activated_or_deleted_and_whether_it_has_charges_and_amounts()
    {
        var world = new PagamentsWorld();
        var current = await world.YearAsync(2026);
        var next = await world.YearAsync(2027);
        var handler = new GetYearScreenHandler(world.Store.Years, world.Store.ConceptAmounts, world.Store.Charges, world.Clock);

        var beforeAnything = (await handler.HandleAsync(new GetYearScreenRequest(next), default)).Value!;
        Assert.Equal("SchoolYears.AnotherActive", beforeAnything.ActivationBlocked!.Code);
        Assert.Null(beforeAnything.DeletionBlocked);
        Assert.False(beforeAnything.HasAmounts);
        Assert.False(beforeAnything.HasCharges);

        await world.SetAmountsAsync(current, 50m, 20m, 10m);
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, locker);
        world.Store.YearsWithData.Add(current); // the in-memory store does not derive it from the enrolments

        var active = (await handler.HandleAsync(new GetYearScreenRequest(current), default)).Value!;
        Assert.Null(active.ActivationBlocked); // the active year has nothing to activate
        Assert.Equal("SchoolYears.HasData", active.DeletionBlocked!.Code);
        Assert.True(active.HasAmounts);
        Assert.True(active.HasCharges);
        Assert.False(active.IsHistoric);
    }

    [Fact]
    [Trait("spec", "pantalles-de-domini/pantalles-curs-i-imports: Detalle del curso de solo lectura si no está activo")]
    public async Task A_year_that_has_ended_is_historic_and_its_amounts_are_blocked()
    {
        var world = new PagamentsWorld();
        await world.YearAsync(2026); // the first year of the system is the active one
        var old = await world.YearAsync(2020);
        var handler = new GetYearScreenHandler(world.Store.Years, world.Store.ConceptAmounts, world.Store.Charges, world.Clock);

        var detail = (await handler.HandleAsync(new GetYearScreenRequest(old), default)).Value!;

        Assert.True(detail.IsHistoric);
        Assert.Equal("ConceptAmounts.YearFinished", detail.AmountsBlocked!.Code);
    }

    [Fact]
    [Trait("spec", "pantalles-de-domini/pantalles-curs-i-imports: Aviso del efecto de cambiar un importe (Historial de importes)")]
    public async Task The_history_of_the_amounts_lists_each_definition_and_change_with_its_values_most_recent_first()
    {
        var world = new PagamentsWorld();
        var year = await world.YearAsync(2026);
        await world.SetAmountsAsync(year, 50m, 20m, 10m);
        world.Clock.Advance(TimeSpan.FromHours(1));
        await world.SetAmountsAsync(year, 55m, 20m, 10m);
        var handler = new GetConceptAmountsHistoryHandler(world.Store.Years, world.Store.ConceptAmounts, world.Store.ConceptAmountEvents, new ResxLocalizer());

        var lines = (await handler.HandleAsync(new GetConceptAmountsHistoryRequest(year), default)).Value!;

        Assert.Equal(4, lines.Count); // three definitions and one change
        Assert.Contains("de 50,00", lines[0].Text, StringComparison.Ordinal);
        Assert.Contains("a 55,00", lines[0].Text, StringComparison.Ordinal);
        Assert.Equal(lines.OrderByDescending(l => l.At), lines);
    }

    [Fact]
    [Trait("spec", "pantalles-de-domini/pantalles-cobraments: Historial del cargo (Historial visible)")]
    public async Task A_revert_in_the_history_says_the_charge_is_pending_again()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, locker);
        var fee = world.ChargesOf(student.Id).Single(c => c.Concept == Domain.ConceptAmounts.ChargeConcept.Fee);
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(fee.Id), default);
        world.Clock.Advance(TimeSpan.FromHours(1));
        await world.Revert.HandleAsync(new Arca.Application.Charges.RevertCharge.RevertChargeRequest(fee.Id, "Error"), default);
        var handler = new GetChargeHistoryHandler(world.Store.Charges, world.Store.ChargeEvents, new ResxLocalizer());

        var lines = (await handler.HandleAsync(new GetChargeHistoryRequest(fee.Id), default)).Value!;

        Assert.Contains("de pagat a pendent", lines[0].Text, StringComparison.Ordinal);
    }
}
