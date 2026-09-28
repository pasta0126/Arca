// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Charges;
using Arca.Application.Charges.GetStudentPayment;
using Arca.Application.Charges.ListDebtors;
using Arca.Application.Charges.ListDepositsDueBack;
using Arca.Application.Charges.MarkChargeExempt;
using Arca.Application.Charges.MarkChargePaid;
using Arca.Application.Charges.ReturnDepositsInBulk;
using Arca.Application.Charges.WaiveChargesInBulk;
using Arca.Application.ConceptAmounts;
using Arca.Application.Localization;
using Arca.Application.Students.RetireStudent;
using Arca.Domain.ConceptAmounts;
using Xunit;

namespace Arca.Application.Tests.Charges;

public sealed class ChargeFeedbackTests
{
    const string Spec = "pagaments/cobraments";
    const string DepositSpec = "pagaments/fianca";

    readonly ResxLocalizer _localizer = new();

    string Money(decimal amount) => amount.ToString("C", _localizer.Culture);

    static async Task<(PagamentsWorld World, Guid Zone)> WorldAsync()
    {
        var world = new PagamentsWorld();
        await world.SeedAmountsAsync(await world.YearAsync(2026), 50m, 20m, 10m);
        return (world, await world.ZoneAsync("Planta 1"));
    }

    static async Task<Guid> AssignedAsync(PagamentsWorld world, Guid zone, int number, string first = "Marta", string last = "Puig")
    {
        var student = await world.StudentAsync(first, last, first + number + "@example.com");
        Assert.True((await world.AssignAsync(student.Id, await world.LockerAsync(number, zone))).IsSuccess);
        return student.Id;
    }

    // --- Results ---

    [Fact]
    [Trait("spec", Spec + ": Feedback y confirmaciones en los cobros (Resultado)")]
    public async Task Marking_paid_says_which_charge_of_whom_and_how_much()
    {
        var (world, zone) = await WorldAsync();
        var id = await AssignedAsync(world, zone, 1);
        var fee = world.ChargesOf(id).Single(c => c.Concept == ChargeConcept.Fee);

        var paid = (await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(fee.Id, null), default)).Value!;

        Assert.Equal($"S'ha marcat com a pagat el càrrec de quota de Marta Puig ({Money(50m)}).", new ChargeResultTexts(_localizer).Paid(paid));
    }

    [Fact]
    [Trait("spec", Spec + ": Feedback y confirmaciones en los cobros (Resultado)")]
    public async Task The_deposit_is_always_called_the_lockers_deposit_never_just_a_deposit()
    {
        var (world, zone) = await WorldAsync();
        var id = await AssignedAsync(world, zone, 1);
        var deposit = world.ChargesOf(id).Single(c => c.Concept == ChargeConcept.Deposit);

        var exempt = (await world.MarkExempt.HandleAsync(new MarkChargeExemptRequest(deposit.Id, "Beca"), default)).Value!;

        Assert.Equal($"S'ha marcat com a exempt el càrrec de dipòsit de la taquilla de Marta Puig ({Money(20m)}).", new ChargeResultTexts(_localizer).Exempted(exempt));
    }

    [Fact]
    [Trait("spec", Spec + ": Condonación en bloque (Condonación de varios cargos)")]
    public void A_bulk_waiver_says_how_many_charges_and_how_much_with_singular_and_plural()
    {
        var texts = new ChargeResultTexts(_localizer);
        var plan = new BulkChargePlan([], [], 0m);

        Assert.Equal($"S'ha condonat 1 càrrec, per un total de {Money(50m)}.", texts.BulkWaived(new BulkChargeResult(true, 1, 50m, plan)));
        Assert.Equal($"S'han condonat 20 càrrecs, per un total de {Money(1000m)}.", texts.BulkWaived(new BulkChargeResult(true, 20, 1000m, plan)));
    }

    [Fact]
    [Trait("spec", DepositSpec + ": Devolución en bloque (Devolución de varias fianzas)")]
    public void A_bulk_return_says_how_many_deposits_and_how_much()
    {
        var texts = new ChargeResultTexts(_localizer);
        var plan = new BulkChargePlan([], [], 0m);

        Assert.Equal($"S'ha retornat 1 dipòsit, per un total de {Money(20m)}.", texts.BulkReturned(new BulkChargeResult(true, 1, 20m, plan)));
        Assert.Equal($"S'han retornat 40 dipòsits, per un total de {Money(800m)}.", texts.BulkReturned(new BulkChargeResult(true, 40, 800m, plan)));
    }

    // --- Confirmations ---

    [Fact]
    [Trait("spec", Spec + ": Reversión de un cargo (Confirmación)")]
    public async Task Reverting_says_the_charge_will_count_as_debt_again()
    {
        var (world, zone) = await WorldAsync();
        var id = await AssignedAsync(world, zone, 1);
        var fee = world.ChargesOf(id).Single(c => c.Concept == ChargeConcept.Fee);
        var paid = (await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(fee.Id, null), default)).Value!;

        var request = new ChargeConfirmations(_localizer).ForRevert(paid);

        Assert.Equal("Revertir el càrrec de quota de Marta Puig?", request.Title);
        Assert.Contains("tornarà a ser pendent", request.Consequence);
        Assert.Contains("pagat", request.Consequence);
        Assert.False(request.Destructive);
    }

    [Fact]
    [Trait("spec", Spec + ": Anulación de un cargo (Confirmación)")]
    public async Task Voiding_asks_for_a_destructive_confirmation_saying_it_is_final()
    {
        var (world, zone) = await WorldAsync();
        var id = await AssignedAsync(world, zone, 1);
        var fee = world.ChargesOf(id).Single(c => c.Concept == ChargeConcept.Fee);
        var row = (await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(fee.Id, null), default)).Value!;

        var request = new ChargeConfirmations(_localizer).ForVoid(row);

        Assert.True(request.Destructive);
        Assert.Contains("no es podrà revertir", request.Consequence);
    }

    [Fact]
    [Trait("spec", Spec + ": Condonación en bloque (Confirmación con recuentos)")]
    public async Task The_bulk_waiver_confirmation_shows_counts_and_total_before_acting()
    {
        var (world, zone) = await WorldAsync();
        var ids = new List<Guid>();
        for (var i = 1; i <= 3; i++)
        {
            ids.Add(world.ChargesOf(await AssignedAsync(world, zone, i, "Alumne" + i)).Single(c => c.Concept == ChargeConcept.Fee).Id);
        }

        var plan = (await world.WaiveInBulk.AnalyzeAsync(new WaiveChargesInBulkRequest(ids, "Motiu"), null, default)).Value!;
        var request = new ChargeConfirmations(_localizer).ForBulkWaive(plan);

        Assert.Equal("Condonar 3 càrrecs?", request.Title);
        Assert.Contains($"3 càrrecs, per un total de {Money(150m)}", request.Consequence);
        Assert.All(ids, id => Assert.Equal(Domain.Charges.ChargeStatus.Pending, world.Store.ChargeList.Single(c => c.Id == id).Status));
    }

    [Fact]
    [Trait("spec", DepositSpec + ": Devolución en bloque (Confirmación con recuentos)")]
    public async Task The_bulk_return_confirmation_shows_counts_and_total_before_acting()
    {
        var (world, zone) = await WorldAsync();
        var ids = new List<Guid>();
        for (var i = 1; i <= 2; i++)
        {
            var student = await AssignedAsync(world, zone, i, "Alumne" + i);
            var deposit = world.ChargesOf(student).Single(c => c.Concept == ChargeConcept.Deposit);
            await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(deposit.Id, null), default);
            await world.Assignments.Students.Retire.HandleAsync(new RetireStudentRequest(student, "Trasllat"), default);
            ids.Add(deposit.Id);
        }

        var plan = (await world.ReturnDepositsInBulk.AnalyzeAsync(new ReturnDepositsInBulkRequest(ids, null, null), null, default)).Value!;
        var request = new ChargeConfirmations(_localizer).ForBulkReturn(plan);

        Assert.Equal("Marcar 2 dipòsits com a retornats?", request.Title);
        Assert.Contains($"2 dipòsits de la taquilla com a retornats, per un total de {Money(40m)}", request.Consequence);
    }

    // --- Empty states ---

    [Fact]
    [Trait("spec", Spec + ": Feedback y confirmaciones en los cobros (Sin cargos que mostrar)")]
    public async Task A_student_without_charges_explains_they_are_generated_when_assigning_a_locker()
    {
        var (world, _) = await WorldAsync();
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        var payment = (await world.StudentPayment.HandleAsync(new GetStudentPaymentRequest(student.Id), default)).Value!;

        var guide = ChargeEmptyStates.ForStudent(payment, _localizer);

        Assert.Contains("Es generen en assignar-li una taquilla", guide!.Message);
    }

    [Fact]
    [Trait("spec", Spec + ": Consulta de morosos (Sin morosos)")]
    public async Task No_debt_says_everything_is_up_to_date_and_debt_hidden_by_filters_offers_clearing_them()
    {
        var (world, zone) = await WorldAsync();
        var none = (await world.Debtors.HandleAsync(new ListDebtorsRequest(), default)).Value!;
        await AssignedAsync(world, zone, 1);
        var hidden = (await world.Debtors.HandleAsync(new ListDebtorsRequest(new DebtorFilter(YearId: Guid.NewGuid())), default)).Value!;
        var some = (await world.Debtors.HandleAsync(new ListDebtorsRequest(), default)).Value!;

        Assert.Contains("Tot està al corrent", ChargeEmptyStates.ForDebtors(none, _localizer)!.Message);
        Assert.Equal([ChargeSuggestedAction.ClearFilters], ChargeEmptyStates.ForDebtors(hidden, _localizer)!.Actions);
        Assert.Null(ChargeEmptyStates.ForDebtors(some, _localizer));
    }

    [Fact]
    [Trait("spec", DepositSpec + ": Lista de fianzas por devolver (Sin fianzas por devolver)")]
    public async Task With_no_deposits_due_back_the_guide_explains_when_they_appear()
    {
        var (world, _) = await WorldAsync();
        var listing = (await world.DepositsDueBack.HandleAsync(new ListDepositsDueBackRequest(), default)).Value!;

        var guide = ChargeEmptyStates.ForDepositsDueBack(listing, _localizer);

        Assert.Contains("causa baixa", guide!.Message);
    }

    [Fact]
    [Trait("spec", Spec + ": Feedback y confirmaciones en los cobros (Importes sin definir)")]
    public void Undefined_amounts_offer_to_define_them_and_defined_ones_show_nothing()
    {
        var yearId = Guid.NewGuid();
        var undefined = ConceptAmountsView.Of(yearId, [], isEditable: true, isProposed: false);

        var guide = ChargeEmptyStates.ForAmounts(undefined, _localizer);

        Assert.Equal([ChargeSuggestedAction.DefineAmounts], guide!.Actions);
        Assert.Equal("Defineix els imports", ChargeEmptyStates.Label(ChargeSuggestedAction.DefineAmounts, _localizer));
        Assert.Null(ChargeEmptyStates.ForAmounts(new ConceptAmountsView(yearId, 50m, 20m, null, true, false), _localizer));
    }

    [Fact]
    [Trait("spec", Spec + ": Aviso de deuda de cursos anteriores")]
    public void The_prior_debt_warning_reads_well_for_one_charge_and_for_many()
    {
        foreach (var count in new[] { 1, 3 })
        {
            var text = _localizer.Get("Charges.Warning.PriorDebt", count, 90m);

            Assert.Contains($"pendents de cursos anteriors: {count}, per un total de {Money(90m)}", text);
        }
    }
}
