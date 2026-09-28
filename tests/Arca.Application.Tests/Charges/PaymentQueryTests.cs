// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Charges;
using Arca.Application.Charges.GetLockerPayment;
using Arca.Application.Charges.GetStudentPayment;
using Arca.Application.Charges.ListDebtors;
using Arca.Application.Charges.ListDepositsDueBack;
using Arca.Application.Charges.MarkChargeExempt;
using Arca.Application.Charges.MarkChargePaid;
using Arca.Application.Students.RetireStudent;
using Arca.Domain.Charges;
using Arca.Domain.ConceptAmounts;
using Xunit;

namespace Arca.Application.Tests.Charges;

public sealed class PaymentQueryTests
{
    const string Spec = "pagaments/cobraments";
    const string DepositSpec = "pagaments/fianca";

    static async Task<(PagamentsWorld World, Guid Year, Guid Zone)> WorldAsync()
    {
        var world = new PagamentsWorld();
        var year = await world.YearAsync(2026);
        await world.SeedAmountsAsync(year, 50m, 20m, 10m);
        return (world, year, await world.ZoneAsync("Planta 1"));
    }

    static async Task<Guid> AssignedStudentAsync(PagamentsWorld world, string first, string last, Guid locker, string level = "1r ESO", string group = "A")
    {
        var student = await world.Assignments.StudentAsync(first, last, first + "@example.com", level, group);
        Assert.True((await world.AssignAsync(student.Id, locker)).IsSuccess);
        return student.Id;
    }

    static Charge ChargeOf(PagamentsWorld world, Guid studentId, ChargeConcept concept) =>
        world.ChargesOf(studentId).Single(c => c.Concept == concept);

    [Fact]
    [Trait("spec", Spec + ": Estado al corriente de pago de un alumno (Con deuda)")]
    public async Task The_record_of_a_student_shows_the_standing_and_every_charge_with_its_reason()
    {
        var (world, _, zone) = await WorldAsync();
        var id = await AssignedStudentAsync(world, "Marta", "Puig", await world.LockerAsync(1, zone));
        await world.MarkExempt.HandleAsync(new MarkChargeExemptRequest(ChargeOf(world, id, ChargeConcept.Deposit).Id, "Beca de menjador"), default);

        var payment = (await world.StudentPayment.HandleAsync(new GetStudentPaymentRequest(id), default)).Value!;

        Assert.False(payment.Standing.UpToDate);
        Assert.Equal(50m, payment.Standing.PendingTotal);
        Assert.Equal(2, payment.Charges.Count);
        Assert.Equal("Beca de menjador", payment.Charges.Single(c => c.Concept == ChargeConcept.Deposit).Reason);
    }

    [Fact]
    [Trait("spec", Spec + ": Estado al corriente de pago de un alumno (Alumno sin cargos)")]
    public async Task A_student_without_charges_is_up_to_date_and_has_an_empty_list()
    {
        var (world, _, _) = await WorldAsync();
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");

        var payment = (await world.StudentPayment.HandleAsync(new GetStudentPaymentRequest(student.Id), default)).Value!;

        Assert.True(payment.Standing.UpToDate);
        Assert.Empty(payment.Charges);
    }

    [Fact]
    [Trait("spec", Spec + ": Estado de pago de una taquilla (Taquilla con alumno moroso)")]
    public async Task An_occupied_locker_shows_the_standing_of_its_student()
    {
        var (world, _, zone) = await WorldAsync();
        var locker = await world.LockerAsync(1, zone);
        await AssignedStudentAsync(world, "Marta", "Puig", locker);

        var payment = (await world.LockerPayment.HandleAsync(new GetLockerPaymentRequest(locker), default)).Value!;

        Assert.False(payment.Standing!.UpToDate);
    }

    [Fact]
    [Trait("spec", Spec + ": Estado de pago de una taquilla (Taquilla libre)")]
    public async Task A_free_locker_shows_no_payment_state()
    {
        var (world, _, zone) = await WorldAsync();

        var payment = (await world.LockerPayment.HandleAsync(new GetLockerPaymentRequest(await world.LockerAsync(1, zone)), default)).Value!;

        Assert.Null(payment.Standing);
    }

    [Fact]
    [Trait("spec", Spec + ": Consulta de morosos (Listado por defecto)")]
    public async Task Debtors_are_listed_by_last_name_with_their_breakdown_and_the_totals()
    {
        var (world, _, zone) = await WorldAsync();
        await AssignedStudentAsync(world, "Ana", "Zapata", await world.LockerAsync(1, zone));
        var abad = await AssignedStudentAsync(world, "Pau", "Abad", await world.LockerAsync(2, zone));
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(ChargeOf(world, abad, ChargeConcept.Fee).Id, null), default);

        var listing = (await world.Debtors.HandleAsync(new ListDebtorsRequest(), default)).Value!;

        Assert.Equal(["Abad", "Zapata"], listing.Rows.Select(r => r.LastName));
        Assert.Equal(20m, listing.Rows[0].PendingTotal);
        Assert.Equal(70m, listing.Rows[1].PendingTotal);
        Assert.Equal(2, listing.Rows[1].Breakdown.Count);
        Assert.Equal((2, 90m), (listing.StudentCount, listing.PendingTotal));
        Assert.Equal((2, 90m), (listing.OverallStudentCount, listing.OverallPendingTotal));
        Assert.Equal("1r ESO", listing.Rows[1].LevelName);
        Assert.Equal(1, listing.Rows[1].LockerNumber);
    }

    [Fact]
    [Trait("spec", Spec + ": Consulta de morosos (Alumno de baja con deuda)")]
    public async Task A_retired_student_with_a_pending_fee_appears_marked_as_retired()
    {
        var (world, _, zone) = await WorldAsync();
        var id = await AssignedStudentAsync(world, "Marta", "Puig", await world.LockerAsync(1, zone));
        await world.Assignments.Students.Retire.HandleAsync(new RetireStudentRequest(id, "Trasllat"), default);

        var listing = (await world.Debtors.HandleAsync(new ListDebtorsRequest(), default)).Value!;

        var row = Assert.Single(listing.Rows);
        Assert.True(row.IsRetired);
        Assert.Equal(50m, row.PendingTotal);
        Assert.Null(row.LockerNumber);
    }

    [Fact]
    [Trait("spec", Spec + ": Consulta de morosos (Filtro por concepto)")]
    public async Task Filtering_by_concept_year_level_group_and_zone_narrows_rows_and_totals()
    {
        var (world, _, zone) = await WorldAsync();
        var otherZone = await world.ZoneAsync("Planta 2");
        var a = await AssignedStudentAsync(world, "Ana", "Abad", await world.LockerAsync(1, zone), "1r ESO", "A");
        var b = await AssignedStudentAsync(world, "Bru", "Bosch", await world.LockerAsync(2, otherZone), "2n ESO", "B");
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(ChargeOf(world, b, ChargeConcept.Fee).Id, null), default);
        var levels = await world.Store.Catalog.ListLevelsAsync(default);
        var groups = await world.Store.Catalog.ListGroupsAsync(default);

        var deposit = (await world.Debtors.HandleAsync(new ListDebtorsRequest(new DebtorFilter(Concept: ChargeConcept.Deposit)), default)).Value!;
        var fee = (await world.Debtors.HandleAsync(new ListDebtorsRequest(new DebtorFilter(Concept: ChargeConcept.Fee)), default)).Value!;
        var byLevel = (await world.Debtors.HandleAsync(new ListDebtorsRequest(new DebtorFilter(LevelId: levels.Single(l => l.Name == "2n ESO").Id)), default)).Value!;
        var byGroup = (await world.Debtors.HandleAsync(new ListDebtorsRequest(new DebtorFilter(GroupId: groups.Single(g => g.Name == "A").Id)), default)).Value!;
        var byZone = (await world.Debtors.HandleAsync(new ListDebtorsRequest(new DebtorFilter(ZoneId: otherZone)), default)).Value!;
        var byOtherYear = (await world.Debtors.HandleAsync(new ListDebtorsRequest(new DebtorFilter(YearId: Guid.NewGuid())), default)).Value!;

        Assert.Equal(2, deposit.StudentCount);
        Assert.Equal(40m, deposit.PendingTotal);
        Assert.Equal([a], fee.Rows.Select(r => r.StudentId));
        Assert.Equal(50m, fee.PendingTotal);
        Assert.Equal([b], byLevel.Rows.Select(r => r.StudentId));
        Assert.Equal([a], byGroup.Rows.Select(r => r.StudentId));
        Assert.Equal([b], byZone.Rows.Select(r => r.StudentId));
        Assert.Equal(DebtorsEmptyState.NoResults, byOtherYear.EmptyState);
        Assert.Equal(2, byOtherYear.OverallStudentCount);
    }

    [Fact]
    [Trait("spec", Spec + ": Consulta de morosos (Sin morosos)")]
    public async Task With_nothing_pending_the_query_says_there_is_no_debt()
    {
        var (world, _, _) = await WorldAsync();

        var listing = (await world.Debtors.HandleAsync(new ListDebtorsRequest(), default)).Value!;

        Assert.Empty(listing.Rows);
        Assert.Equal(DebtorsEmptyState.NoDebt, listing.EmptyState);
    }

    [Fact]
    [Trait("spec", Spec + ": Consulta de morosos (Privacidad)")]
    public void No_row_or_listing_of_the_payment_queries_carries_an_email_an_identifier_or_a_reason()
    {
        var forbidden = new[] { "Email", "Dni", "Identifier", "Reason", "Note" };
        foreach (var type in new[] { typeof(DebtorRow), typeof(DebtorsListing), typeof(DepositDueRow), typeof(DepositsDueBackListing) })
        {
            Assert.DoesNotContain(type.GetProperties(), p => forbidden.Any(f => p.Name.Contains(f, StringComparison.OrdinalIgnoreCase)));
        }
    }

    [Fact]
    [Trait("spec", DepositSpec + ": Lista de fianzas por devolver (Listado por defecto)")]
    public async Task Deposits_due_back_are_listed_by_departure_date_with_the_total()
    {
        var (world, _, zone) = await WorldAsync();
        var late = await AssignedStudentAsync(world, "Ana", "Abad", await world.LockerAsync(1, zone));
        var early = await AssignedStudentAsync(world, "Pau", "Zapata", await world.LockerAsync(2, zone));
        foreach (var id in new[] { late, early })
        {
            await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(ChargeOf(world, id, ChargeConcept.Deposit).Id, null), default);
        }

        world.Clock.Advance(TimeSpan.FromHours(1));
        await world.Assignments.Students.Retire.HandleAsync(new RetireStudentRequest(early, "Trasllat"), default);
        world.Clock.Advance(TimeSpan.FromHours(1));
        await world.Assignments.Students.Retire.HandleAsync(new RetireStudentRequest(late, "Trasllat"), default);

        var listing = (await world.DepositsDueBack.HandleAsync(new ListDepositsDueBackRequest(), default)).Value!;

        Assert.Equal(["Zapata", "Abad"], listing.Rows.Select(r => r.LastName));
        Assert.Equal((2, 40m), (listing.Count, listing.TotalAmount));
        Assert.False(listing.IsEmpty);
    }

    [Fact]
    [Trait("spec", DepositSpec + ": Lista de fianzas por devolver (Exentas fuera de la lista)")]
    public async Task An_exempt_deposit_of_a_retired_student_is_not_listed()
    {
        var (world, _, zone) = await WorldAsync();
        var id = await AssignedStudentAsync(world, "Marta", "Puig", await world.LockerAsync(1, zone));
        await world.MarkExempt.HandleAsync(new MarkChargeExemptRequest(ChargeOf(world, id, ChargeConcept.Deposit).Id, "Beca"), default);
        await world.Assignments.Students.Retire.HandleAsync(new RetireStudentRequest(id, "Trasllat"), default);

        var listing = (await world.DepositsDueBack.HandleAsync(new ListDepositsDueBackRequest(), default)).Value!;

        Assert.Empty(listing.Rows);
    }

    [Fact]
    [Trait("spec", DepositSpec + ": Lista de fianzas por devolver (Sin fianzas por devolver)")]
    public async Task With_no_deposits_due_back_the_listing_says_so()
    {
        var (world, _, _) = await WorldAsync();

        var listing = (await world.DepositsDueBack.HandleAsync(new ListDepositsDueBackRequest(), default)).Value!;

        Assert.True(listing.IsEmpty);
        Assert.Equal(0m, listing.TotalAmount);
    }
}
