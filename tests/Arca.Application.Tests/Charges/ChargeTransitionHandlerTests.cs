// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Charges.AdjustChargeAmount;
using Arca.Application.Charges.MarkChargeExempt;
using Arca.Application.Charges.MarkChargePaid;
using Arca.Application.Charges.RevertCharge;
using Arca.Application.Charges.VoidCharge;
using Arca.Application.Charges.WaiveCharge;
using Arca.Domain.Charges;
using Arca.Domain.ConceptAmounts;
using Xunit;

namespace Arca.Application.Tests.Charges;

/// <summary>The application-layer wrapping of the charge transitions: repository lookup, saving and the history event.</summary>
public sealed class ChargeTransitionHandlerTests
{
    const string Spec = "pagaments/cobraments";

    static async Task<(PagamentsWorld World, Charge Charge)> ChargeAsync()
    {
        var world = new PagamentsWorld();
        var year = await world.YearAsync(2026);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        var charge = await world.SeedChargeAsync(student.Id, ChargeConcept.Fee, year, 50m);
        return (world, charge);
    }

    [Fact]
    [Trait("spec", Spec + ": Marcar un cargo como pagado (Pago correcto)")]
    public async Task Marking_paid_saves_the_change_and_its_history_event()
    {
        var (world, charge) = await ChargeAsync();

        var result = await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(charge.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChargeStatus.Paid, result.Value!.Status);
        Assert.Equal("Marta Puig", result.Value.StudentName);
        Assert.Contains(await world.Store.ChargeEvents.ListAsync(charge.Id, default), e => e.Type == ChargeEventTypes.Paid);
    }

    [Fact]
    [Trait("spec", Spec + ": Marcar un cargo como pagado (Cargo que no está pendiente)")]
    public async Task Marking_an_unknown_charge_paid_is_refused()
    {
        var world = new PagamentsWorld();

        var result = await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(Guid.NewGuid()), default);

        Assert.Equal("Charges.NotFound", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Exento y condonado con motivo obligatorio (Exención por beca)")]
    public async Task Marking_exempt_saves_the_reason()
    {
        var (world, charge) = await ChargeAsync();

        var result = await world.MarkExempt.HandleAsync(new MarkChargeExemptRequest(charge.Id, "Beca"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChargeStatus.Exempt, result.Value!.Status);
        Assert.Equal("Beca", result.Value.Reason);
    }

    [Fact]
    [Trait("spec", Spec + ": Exento y condonado con motivo obligatorio (Condonación)")]
    public async Task Waiving_saves_the_reason()
    {
        var (world, charge) = await ChargeAsync();

        var result = await world.Waive.HandleAsync(new WaiveChargeRequest(charge.Id, "Situació familiar"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChargeStatus.Waived, result.Value!.Status);
    }

    [Fact]
    [Trait("spec", Spec + ": Revertir a pendiente (Corregir un pago erróneo)")]
    public async Task Reverting_a_paid_charge_goes_back_to_pending()
    {
        var (world, charge) = await ChargeAsync();
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(charge.Id), default);

        var result = await world.Revert.HandleAsync(new RevertChargeRequest(charge.Id, "Error"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChargeStatus.Pending, result.Value!.Status);
    }

    [Fact]
    [Trait("spec", Spec + ": Anular un cargo (Cargo generado por error)")]
    public async Task Voiding_a_pending_charge_saves_the_reason()
    {
        var (world, charge) = await ChargeAsync();

        var result = await world.Void.HandleAsync(new VoidChargeRequest(charge.Id, "Generat per error"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChargeStatus.Voided, result.Value!.Status);
    }

    [Fact]
    [Trait("spec", Spec + ": Ajuste del importe de un cargo pendiente (Ajuste correcto)")]
    public async Task Adjusting_the_amount_saves_the_new_value()
    {
        var (world, charge) = await ChargeAsync();

        var result = await world.Adjust.HandleAsync(new AdjustChargeAmountRequest(charge.Id, 55m, "Error inicial"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(55m, result.Value!.Amount);
    }
}
