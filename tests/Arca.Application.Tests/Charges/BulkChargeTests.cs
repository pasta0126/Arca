// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Charges;
using Arca.Application.Charges.MarkChargePaid;
using Arca.Application.Charges.ReturnDepositsInBulk;
using Arca.Application.Charges.WaiveChargesInBulk;
using Arca.Application.Feedback;
using Arca.Application.Students.RetireStudent;
using Arca.Domain.Charges;
using Arca.Domain.ConceptAmounts;
using Xunit;

namespace Arca.Application.Tests.Charges;

public sealed class BulkChargeTests
{
    const string Spec = "pagaments/cobraments";
    const string DepositSpec = "pagaments/fianca";

    sealed class Recorder : IProgress<OperationProgress>
    {
        public List<OperationProgress> Reports { get; } = [];

        public void Report(OperationProgress value) => Reports.Add(value);
    }

    static async Task<(PagamentsWorld World, Guid Year, Guid Zone)> WorldAsync()
    {
        var world = new PagamentsWorld();
        var year = await world.YearAsync(2026);
        await world.SeedAmountsAsync(year, 50m, 20m, 10m);
        return (world, year, await world.ZoneAsync("Planta 1"));
    }

    static async Task<List<Guid>> PendingFeesAsync(PagamentsWorld world, Guid year, int count)
    {
        var ids = new List<Guid>();
        for (var i = 0; i < count; i++)
        {
            var student = await world.StudentAsync("Alumne" + i, "Cognom" + i, $"a{i}@example.com");
            ids.Add((await world.SeedChargeAsync(student.Id, ChargeConcept.Fee, year, 50m)).Id);
        }

        return ids;
    }

    static async Task<List<Guid>> DepositsDueBackAsync(PagamentsWorld world, Guid zone, int count)
    {
        var ids = new List<Guid>();
        for (var i = 1; i <= count; i++)
        {
            var student = await world.StudentAsync("Alumne" + i, "Cognom" + i, $"a{i}@example.com");
            await world.AssignAsync(student.Id, await world.LockerAsync(i, zone));
            var deposit = world.ChargesOf(student.Id).Single(c => c.Concept == ChargeConcept.Deposit);
            await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(deposit.Id, null), default);
            await world.Assignments.Students.Retire.HandleAsync(new RetireStudentRequest(student.Id, "Trasllat"), default);
            ids.Add(deposit.Id);
        }

        return ids;
    }

    [Fact]
    [Trait("spec", Spec + ": Condonación en bloque (Condonación de varios cargos)")]
    public async Task Waiving_20_charges_waives_them_all_with_the_reason_and_their_history()
    {
        var (world, year, _) = await WorldAsync();
        var ids = await PendingFeesAsync(world, year, 20);
        var request = new WaiveChargesInBulkRequest(ids, "Família en dificultats");
        var plan = (await world.WaiveInBulk.AnalyzeAsync(request, null, default)).Value!;

        var result = await world.WaiveInBulk.ApplyAsync(request, plan, null, default);

        Assert.True(result.Value!.Applied);
        Assert.Equal(20, result.Value.Count);
        Assert.Equal(1000m, result.Value.TotalAmount);
        Assert.All(ids, id =>
        {
            var charge = world.Store.ChargeList.Single(c => c.Id == id);
            Assert.Equal(ChargeStatus.Waived, charge.Status);
            Assert.Equal("Família en dificultats", charge.Reason);
            Assert.Contains(world.Store.ChargeEventList, e => e.EntityId == id && e.Type == ChargeEventTypes.Waived);
        });
    }

    [Fact]
    [Trait("spec", Spec + ": Condonación en bloque (Confirmación con recuentos)")]
    public async Task The_analysis_shows_how_many_and_how_much_and_changes_nothing()
    {
        var (world, year, _) = await WorldAsync();
        var ids = await PendingFeesAsync(world, year, 3);

        var plan = (await world.WaiveInBulk.AnalyzeAsync(new WaiveChargesInBulkRequest(ids, "Motiu"), null, default)).Value!;

        Assert.Equal(3, plan.Eligible.Count);
        Assert.Equal(150m, plan.TotalAmount);
        Assert.All(ids, id => Assert.Equal(ChargeStatus.Pending, world.Store.ChargeList.Single(c => c.Id == id).Status));
    }

    [Fact]
    [Trait("spec", Spec + ": Condonación en bloque (Cargo que dejó de ser pendiente)")]
    public async Task A_charge_that_stopped_being_pending_makes_nothing_change_and_returns_the_updated_selection()
    {
        var (world, year, _) = await WorldAsync();
        var ids = await PendingFeesAsync(world, year, 5);
        var request = new WaiveChargesInBulkRequest(ids, "Motiu");
        var plan = (await world.WaiveInBulk.AnalyzeAsync(request, null, default)).Value!;
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(ids[2], null), default);

        var result = await world.WaiveInBulk.ApplyAsync(request, plan, null, default);

        Assert.False(result.Value!.Applied);
        Assert.Equal(ids[2], Assert.Single(result.Value.Plan.Ineligible));
        Assert.Contains(result.Notices, n => n.Code == "Charges.BulkIneligible");
        Assert.All(ids.Where(id => id != ids[2]), id => Assert.Equal(ChargeStatus.Pending, world.Store.ChargeList.Single(c => c.Id == id).Status));
    }

    [Fact]
    [Trait("spec", Spec + ": Condonación en bloque (Motivo obligatorio)")]
    public async Task Waiving_without_a_reason_is_rejected_at_the_analysis_and_at_the_confirmation()
    {
        var (world, year, _) = await WorldAsync();
        var ids = await PendingFeesAsync(world, year, 2);
        var request = new WaiveChargesInBulkRequest(ids, "  ");

        var analysis = await world.WaiveInBulk.AnalyzeAsync(request, null, default);
        var applied = await world.WaiveInBulk.ApplyAsync(request, new BulkChargePlan(ids, [], 100m), null, default);

        Assert.Equal("Charges.ReasonRequired", analysis.Error!.Code);
        Assert.Equal("Charges.ReasonRequired", applied.Error!.Code);
        Assert.All(ids, id => Assert.Equal(ChargeStatus.Pending, world.Store.ChargeList.Single(c => c.Id == id).Status));
    }

    [Fact]
    [Trait("spec", Spec + ": Condonación en bloque (Condonación de varios cargos)")]
    public async Task A_failure_in_the_middle_of_saving_leaves_no_charge_waived()
    {
        var (world, year, _) = await WorldAsync();
        var ids = await PendingFeesAsync(world, year, 20);
        var request = new WaiveChargesInBulkRequest(ids, "Motiu");
        var plan = (await world.WaiveInBulk.AnalyzeAsync(request, null, default)).Value!;
        var eventsBefore = world.Store.ChargeEventList.Count;
        world.Store.EventsBeforeFailure = 10;

        await Assert.ThrowsAsync<IOException>(() => world.WaiveInBulk.ApplyAsync(request, plan, null, default));

        Assert.All(ids, id => Assert.Equal(ChargeStatus.Pending, world.Store.ChargeList.Single(c => c.Id == id).Status));
        Assert.Equal(eventsBefore, world.Store.ChargeEventList.Count);
    }

    [Fact]
    [Trait("spec", Spec + ": Condonación en bloque (Confirmación con recuentos)")]
    public async Task Cancelling_the_analysis_changes_nothing_and_the_saving_reports_it_cannot_be_cancelled()
    {
        var (world, year, _) = await WorldAsync();
        var ids = await PendingFeesAsync(world, year, 25);
        var request = new WaiveChargesInBulkRequest(ids, "Motiu");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => world.WaiveInBulk.AnalyzeAsync(request, null, cancellation.Token));
        Assert.All(ids, id => Assert.Equal(ChargeStatus.Pending, world.Store.ChargeList.Single(c => c.Id == id).Status));

        var progress = new Recorder();
        var plan = (await world.WaiveInBulk.AnalyzeAsync(request, progress, default)).Value!;
        Assert.All(progress.Reports, r => Assert.True(r.CanCancel));
        var saving = new Recorder();
        await world.WaiveInBulk.ApplyAsync(request, plan, saving, default);
        Assert.Equal(new OperationProgress(25, 25, CanCancel: false), saving.Reports[^1]);
    }

    [Fact]
    [Trait("spec", DepositSpec + ": Devolución en bloque (Devolución de varias fianzas)")]
    public async Task Returning_40_deposits_marks_them_all_with_the_common_date_and_note()
    {
        var (world, _, zone) = await WorldAsync();
        var ids = await DepositsDueBackAsync(world, zone, 40);
        var date = world.Clock.Today.AddDays(-2);
        var request = new ReturnDepositsInBulkRequest(ids, date, "Entregades en mà");
        var plan = (await world.ReturnDepositsInBulk.AnalyzeAsync(request, null, default)).Value!;

        var result = await world.ReturnDepositsInBulk.ApplyAsync(request, plan, null, default);

        Assert.True(result.Value!.Applied);
        Assert.Equal(40, result.Value.Count);
        Assert.Equal(800m, result.Value.TotalAmount);
        Assert.All(ids, id =>
        {
            var deposit = world.Store.ChargeList.Single(c => c.Id == id);
            Assert.Equal(DepositReturnStatus.Returned, deposit.Return);
            Assert.Equal(date, deposit.ReturnedOn);
            Assert.Equal("Entregades en mà", deposit.ReturnNote);
        });
    }

    [Fact]
    [Trait("spec", DepositSpec + ": Devolución en bloque (Confirmación con recuentos)")]
    public async Task The_return_analysis_shows_counts_and_total_and_changes_nothing()
    {
        var (world, _, zone) = await WorldAsync();
        var ids = await DepositsDueBackAsync(world, zone, 4);

        var plan = (await world.ReturnDepositsInBulk.AnalyzeAsync(new ReturnDepositsInBulkRequest(ids, null, null), null, default)).Value!;

        Assert.Equal(4, plan.Eligible.Count);
        Assert.Equal(80m, plan.TotalAmount);
        Assert.All(ids, id => Assert.Equal(DepositReturnStatus.ToReturn, world.Store.ChargeList.Single(c => c.Id == id).Return));
    }

    [Fact]
    [Trait("spec", DepositSpec + ": Devolución en bloque (Fianza que dejó de estar por devolver)")]
    public async Task A_deposit_that_stopped_being_due_back_makes_nothing_change()
    {
        var (world, _, zone) = await WorldAsync();
        var ids = await DepositsDueBackAsync(world, zone, 5);
        var request = new ReturnDepositsInBulkRequest(ids, null, null);
        var plan = (await world.ReturnDepositsInBulk.AnalyzeAsync(request, null, default)).Value!;
        await world.ReturnDeposit.HandleAsync(new Arca.Application.Charges.MarkDepositReturned.MarkDepositReturnedRequest(ids[0], null, null), default);

        var result = await world.ReturnDepositsInBulk.ApplyAsync(request, plan, null, default);

        Assert.False(result.Value!.Applied);
        Assert.Equal(ids[0], Assert.Single(result.Value.Plan.Ineligible));
        Assert.All(ids.Skip(1), id => Assert.Equal(DepositReturnStatus.ToReturn, world.Store.ChargeList.Single(c => c.Id == id).Return));
    }

    [Fact]
    [Trait("spec", DepositSpec + ": Devolución en bloque (Devolución de varias fianzas)")]
    public async Task A_future_date_or_a_long_note_is_rejected_before_anything_is_returned()
    {
        var (world, _, zone) = await WorldAsync();
        var ids = await DepositsDueBackAsync(world, zone, 2);

        var future = await world.ReturnDepositsInBulk.AnalyzeAsync(new ReturnDepositsInBulkRequest(ids, world.Clock.Today.AddDays(1), null), null, default);
        var longNote = await world.ReturnDepositsInBulk.AnalyzeAsync(new ReturnDepositsInBulkRequest(ids, null, new string('a', 501)), null, default);

        Assert.Equal("Charges.DateInvalid", future.Error!.Code);
        Assert.Equal("Charges.NoteTooLong", longNote.Error!.Code);
    }

    [Fact]
    [Trait("spec", DepositSpec + ": Devolución en bloque (Devolución de varias fianzas)")]
    public async Task A_failure_in_the_middle_of_returning_leaves_no_deposit_returned()
    {
        var (world, _, zone) = await WorldAsync();
        var ids = await DepositsDueBackAsync(world, zone, 10);
        var request = new ReturnDepositsInBulkRequest(ids, null, null);
        var plan = (await world.ReturnDepositsInBulk.AnalyzeAsync(request, null, default)).Value!;
        world.Store.EventsBeforeFailure = 5;

        await Assert.ThrowsAsync<IOException>(() => world.ReturnDepositsInBulk.ApplyAsync(request, plan, null, default));

        Assert.All(ids, id => Assert.Equal(DepositReturnStatus.ToReturn, world.Store.ChargeList.Single(c => c.Id == id).Return));
    }
}
