// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Charges;
using Arca.Domain.Common;
using Arca.Domain.ConceptAmounts;
using Xunit;

namespace Arca.Domain.Tests.Charges;

public sealed class ChargeTests
{
    const string Spec = "pagaments/cobraments";

    static readonly DateTimeOffset _now = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);
    static readonly DateOnly _today = new(2026, 9, 27);
    static readonly Money _fee = Money.FromCents(5000);

    static Charge NewCharge() => Charge.Create(Guid.NewGuid(), Guid.NewGuid(), ChargeConcept.Fee, Guid.NewGuid(), _fee, _now).Charge;

    static Charge InStatus(ChargeStatus status) => status switch
    {
        ChargeStatus.Pending => NewCharge(),
        ChargeStatus.Paid => WithTransition(c => c.MarkPaid(null, _today, _now)),
        ChargeStatus.Exempt => WithTransition(c => c.MarkExempt("Beca", _now)),
        ChargeStatus.Waived => WithTransition(c => c.Waive("Situació especial", _now)),
        ChargeStatus.Voided => WithTransition(c => c.Void("Generat per error", _now)),
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    static Charge WithTransition(Func<Charge, Result<HistoryEvent>> transition)
    {
        var charge = NewCharge();
        Assert.True(transition(charge).IsSuccess);
        return charge;
    }

    [Fact]
    [Trait("spec", Spec + ": Cargo por alumno y concepto (Importe fijado al crear)")]
    public void A_new_charge_keeps_its_amount_even_if_asked_about_after_other_changes_elsewhere()
    {
        var created = Charge.Create(Guid.NewGuid(), Guid.NewGuid(), ChargeConcept.Fee, Guid.NewGuid(), _fee, _now);

        Assert.Equal(50m, created.Charge.Amount.Amount);
        Assert.Equal(ChargeEventTypes.Created, created.Event.Type);
    }

    [Fact]
    [Trait("spec", Spec + ": Estados de un cargo (Cargo nuevo)")]
    public void A_new_charge_is_pending()
    {
        var charge = NewCharge();

        Assert.Equal(ChargeStatus.Pending, charge.Status);
        Assert.True(charge.CountsAsDebt);
    }

    [Fact]
    [Trait("spec", Spec + ": Marcar un cargo como pagado (Pago correcto)")]
    public void Marking_paid_with_no_date_uses_today_and_records_the_change()
    {
        var charge = NewCharge();

        var result = charge.MarkPaid(null, _today, _now);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChargeStatus.Paid, charge.Status);
        Assert.Equal(_today, charge.PaidOn);
        Assert.False(charge.CountsAsDebt);
        Assert.Equal(ChargeEventTypes.Paid, result.Value!.Type);
    }

    [Fact]
    [Trait("spec", Spec + ": Marcar un cargo como pagado (Fecha pasada)")]
    public void A_payment_date_in_the_past_is_accepted()
    {
        var charge = NewCharge();
        var past = _today.AddDays(-5);

        var result = charge.MarkPaid(past, _today, _now);

        Assert.True(result.IsSuccess);
        Assert.Equal(past, charge.PaidOn);
    }

    [Fact]
    [Trait("spec", Spec + ": Marcar un cargo como pagado (Fecha futura)")]
    public void A_payment_date_in_the_future_is_refused()
    {
        var charge = NewCharge();

        var result = charge.MarkPaid(_today.AddDays(1), _today, _now);

        Assert.Equal("Charges.DateInvalid", result.Error!.Code);
        Assert.Equal(ChargeStatus.Pending, charge.Status);
    }

    [Theory]
    [Trait("spec", Spec + ": Marcar un cargo como pagado (Cargo que no está pendiente)")]
    [InlineData(ChargeStatus.Paid)]
    [InlineData(ChargeStatus.Exempt)]
    [InlineData(ChargeStatus.Waived)]
    [InlineData(ChargeStatus.Voided)]
    public void Marking_paid_a_charge_that_is_not_pending_is_refused(ChargeStatus status)
    {
        var charge = InStatus(status);

        var result = charge.MarkPaid(null, _today, _now);

        Assert.Equal("Charges.InvalidStatus", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Exento y condonado con motivo obligatorio (Exención por beca)")]
    public void Marking_a_charge_exempt_with_a_reason_stops_it_from_counting_as_debt()
    {
        var charge = NewCharge();

        var result = charge.MarkExempt("Beca", _now);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChargeStatus.Exempt, charge.Status);
        Assert.Equal("Beca", charge.Reason);
        Assert.False(charge.CountsAsDebt);
    }

    [Fact]
    [Trait("spec", Spec + ": Exento y condonado con motivo obligatorio (Condonación)")]
    public void Waiving_a_charge_with_a_reason_stops_it_from_counting_as_debt()
    {
        var charge = NewCharge();

        var result = charge.Waive("Situació familiar", _now);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChargeStatus.Waived, charge.Status);
        Assert.Equal("Situació familiar", charge.Reason);
        Assert.False(charge.CountsAsDebt);
    }

    [Theory]
    [Trait("spec", Spec + ": Exento y condonado con motivo obligatorio (Motivo vacío)")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Exempting_or_waiving_without_a_reason_is_refused(string? reason)
    {
        Assert.Equal("Charges.ReasonRequired", NewCharge().MarkExempt(reason, _now).Error!.Code);
        Assert.Equal("Charges.ReasonRequired", NewCharge().Waive(reason, _now).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Exento y condonado con motivo obligatorio (Motivo demasiado largo)")]
    public void A_reason_over_the_maximum_length_is_refused()
    {
        var tooLong = new string('a', Charge.MaximumReasonLength + 1);

        var result = NewCharge().MarkExempt(tooLong, _now);

        Assert.Equal("Charges.ReasonTooLong", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Revertir a pendiente (Corregir un pago erróneo)")]
    public void Reverting_a_paid_charge_goes_back_to_pending_and_keeps_everything_in_the_history()
    {
        var charge = InStatus(ChargeStatus.Paid);

        var result = charge.Revert("Error en marcar el pagament", _now);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChargeStatus.Pending, charge.Status);
        Assert.Null(charge.PaidOn);
        Assert.Equal(ChargeEventTypes.Reverted, result.Value!.Type);
        Assert.Equal("Error en marcar el pagament", result.Value.Reason);
        Assert.Contains("Paid", result.Value.BeforeJson!, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Revertir a pendiente (Revertir sin motivo)")]
    public void Reverting_without_a_reason_is_refused()
    {
        var charge = InStatus(ChargeStatus.Paid);

        var result = charge.Revert(null, _now);

        Assert.Equal("Charges.ReasonRequired", result.Error!.Code);
        Assert.Equal(ChargeStatus.Paid, charge.Status);
    }

    [Fact]
    [Trait("spec", Spec + ": Revertir a pendiente (Cargo anulado)")]
    public void Reverting_a_voided_charge_is_refused()
    {
        var charge = InStatus(ChargeStatus.Voided);

        var result = charge.Revert("Motiu", _now);

        Assert.Equal("Charges.InvalidStatus", result.Error!.Code);
    }

    [Theory]
    [Trait("spec", Spec + ": Revertir a pendiente (Corregir un pago erróneo)")]
    [InlineData(ChargeStatus.Paid)]
    [InlineData(ChargeStatus.Exempt)]
    [InlineData(ChargeStatus.Waived)]
    public void Every_non_final_status_can_be_reverted_to_pending(ChargeStatus status)
    {
        var charge = InStatus(status);

        var result = charge.Revert("Motiu", _now);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChargeStatus.Pending, charge.Status);
        Assert.Null(charge.Reason);
    }

    [Fact]
    [Trait("spec", Spec + ": Anular un cargo (Cargo generado por error)")]
    public void Voiding_a_pending_charge_with_a_reason_stops_it_from_counting_as_debt()
    {
        var charge = NewCharge();

        var result = charge.Void("Generat per error", _now);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChargeStatus.Voided, charge.Status);
        Assert.Equal("Generat per error", charge.Reason);
        Assert.False(charge.CountsAsDebt);
    }

    [Fact]
    [Trait("spec", Spec + ": Anular un cargo (Anular un cargo pagado)")]
    public void Voiding_a_paid_charge_is_refused_and_says_to_revert_first()
    {
        var charge = InStatus(ChargeStatus.Paid);

        var result = charge.Void("Motiu", _now);

        Assert.Equal("Charges.MustRevertFirst", result.Error!.Code);
        Assert.Equal(ChargeStatus.Paid, charge.Status);
    }

    [Theory]
    [Trait("spec", Spec + ": Anular un cargo (Anular un cargo pagado)")]
    [InlineData(ChargeStatus.Exempt)]
    [InlineData(ChargeStatus.Waived)]
    public void Voiding_an_exempt_or_waived_charge_is_also_refused_and_says_to_revert_first(ChargeStatus status)
    {
        var charge = InStatus(status);

        var result = charge.Void("Motiu", _now);

        Assert.Equal("Charges.MustRevertFirst", result.Error!.Code);
        Assert.Equal(status, charge.Status);
    }

    [Fact]
    [Trait("spec", Spec + ": Anular un cargo (Anular un cargo pagado)")]
    public void Voiding_an_already_voided_charge_is_refused_with_no_dead_end_message()
    {
        var charge = InStatus(ChargeStatus.Voided);

        var result = charge.Void("Motiu", _now);

        Assert.Equal("Charges.InvalidStatus", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Ajuste del importe de un cargo pendiente (Ajuste correcto)")]
    public void Adjusting_the_amount_of_a_pending_charge_keeps_the_previous_value_in_the_history()
    {
        var charge = NewCharge();

        var result = charge.AdjustAmount(55, "Error en l'import inicial", _now);

        Assert.True(result.IsSuccess);
        Assert.Equal(55m, charge.Amount.Amount);
        Assert.Contains("50", result.Value!.BeforeJson!, StringComparison.Ordinal);
        Assert.Contains("55", result.Value.AfterJson!, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Ajuste del importe de un cargo pendiente (Cargo pagado)")]
    public void Adjusting_the_amount_of_a_paid_charge_is_refused()
    {
        var charge = InStatus(ChargeStatus.Paid);

        var result = charge.AdjustAmount(55, "Motiu", _now);

        Assert.Equal("Charges.InvalidStatus", result.Error!.Code);
    }

    [Theory]
    [Trait("spec", Spec + ": Ajuste del importe de un cargo pendiente (Ajuste correcto)")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10.999)]
    [InlineData(10000)]
    public void Adjusting_to_an_invalid_amount_is_refused(double invalid)
    {
        var charge = NewCharge();

        var result = charge.AdjustAmount((decimal)invalid, "Motiu", _now);

        Assert.Equal("Charges.AmountInvalid", result.Error!.Code);
        Assert.Equal(50m, charge.Amount.Amount);
    }
}
