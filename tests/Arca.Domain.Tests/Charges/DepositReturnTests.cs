// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Charges;
using Arca.Domain.Common;
using Arca.Domain.ConceptAmounts;
using Xunit;

namespace Arca.Domain.Tests.Charges;

public sealed class DepositReturnTests
{
    const string Spec = "pagaments/fianca";

    static readonly DateTimeOffset _now = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);
    static readonly DateOnly _today = new(2026, 9, 27);

    static Charge Deposit() => Charge.Create(Guid.NewGuid(), Guid.NewGuid(), ChargeConcept.Deposit, Guid.NewGuid(), Money.FromCents(2000), _now).Charge;

    static Charge PaidDeposit()
    {
        var deposit = Deposit();
        Assert.True(deposit.MarkPaid(null, _today, _now).IsSuccess);
        return deposit;
    }

    static Charge DueBack()
    {
        var deposit = PaidDeposit();
        Assert.True(deposit.MarkReturnDue(_now).IsSuccess);
        return deposit;
    }

    [Fact]
    [Trait("spec", Spec + ": Una fianza vigente por alumno (Fianza exenta)")]
    public void A_deposit_is_current_until_it_is_returned_or_voided()
    {
        Assert.True(Deposit().IsCurrentDeposit);
        Assert.True(PaidDeposit().IsCurrentDeposit);

        var exempt = Deposit();
        exempt.MarkExempt("Beca de menjador", _now);
        Assert.True(exempt.IsCurrentDeposit);

        var waived = Deposit();
        waived.Waive("Situació especial", _now);
        Assert.True(waived.IsCurrentDeposit);

        var voided = Deposit();
        voided.Void("Error", _now);
        Assert.False(voided.IsCurrentDeposit);

        var returned = DueBack();
        returned.MarkReturned(null, null, _today, _now);
        Assert.False(returned.IsCurrentDeposit);
    }

    [Fact]
    [Trait("spec", Spec + ": Una fianza vigente por alumno (Primera asignación sin fianza)")]
    public void A_fee_is_never_a_current_deposit()
    {
        var fee = Charge.Create(Guid.NewGuid(), Guid.NewGuid(), ChargeConcept.Fee, Guid.NewGuid(), Money.FromCents(5000), _now).Charge;

        Assert.False(fee.IsCurrentDeposit);
    }

    [Fact]
    [Trait("spec", Spec + ": Baja del alumno y fianza (Fianza pagada)")]
    public void A_paid_deposit_becomes_due_back_and_stays_paid()
    {
        var deposit = PaidDeposit();

        var due = deposit.MarkReturnDue(_now);

        Assert.True(due.IsSuccess);
        Assert.Equal(ChargeEventTypes.ReturnDue, due.Value!.Type);
        Assert.Equal(DepositReturnStatus.ToReturn, deposit.Return);
        Assert.Equal(ChargeStatus.Paid, deposit.Status);
    }

    [Theory]
    [InlineData(ChargeStatus.Pending)]
    [InlineData(ChargeStatus.Exempt)]
    [InlineData(ChargeStatus.Waived)]
    [Trait("spec", Spec + ": Baja del alumno y fianza (Fianza exenta o condonada)")]
    public void Only_a_paid_deposit_can_become_due_back(ChargeStatus status)
    {
        var deposit = Deposit();
        _ = status switch
        {
            ChargeStatus.Exempt => deposit.MarkExempt("Beca", _now),
            ChargeStatus.Waived => deposit.Waive("Situació especial", _now),
            _ => Result<HistoryEvent>.Success(null!),
        };

        var due = deposit.MarkReturnDue(_now);

        Assert.False(due.IsSuccess);
        Assert.Equal("Charges.InvalidStatus", due.Error!.Code);
        Assert.Equal(DepositReturnStatus.None, deposit.Return);
    }

    [Fact]
    [Trait("spec", Spec + ": Devolución de la fianza (Devolución correcta)")]
    public void Returning_records_the_date_and_the_note_in_the_history()
    {
        var deposit = DueBack();

        var returned = deposit.MarkReturned(new DateOnly(2026, 9, 20), "  Entregada a la família  ", _today, _now);

        Assert.True(returned.IsSuccess);
        Assert.Equal(DepositReturnStatus.Returned, deposit.Return);
        Assert.Equal(new DateOnly(2026, 9, 20), deposit.ReturnedOn);
        Assert.Equal("Entregada a la família", deposit.ReturnNote);
        Assert.Equal(ChargeEventTypes.Returned, returned.Value!.Type);
        Assert.Equal("Entregada a la família", returned.Value.Reason);
    }

    [Fact]
    [Trait("spec", Spec + ": Devolución de la fianza (Devolución correcta)")]
    public void The_return_date_defaults_to_today_and_the_note_is_optional()
    {
        var deposit = DueBack();

        deposit.MarkReturned(null, "   ", _today, _now);

        Assert.Equal(_today, deposit.ReturnedOn);
        Assert.Null(deposit.ReturnNote);
    }

    [Fact]
    [Trait("spec", Spec + ": Devolución de la fianza (Fecha futura)")]
    public void A_future_return_date_is_rejected()
    {
        var deposit = DueBack();

        var returned = deposit.MarkReturned(_today.AddDays(1), null, _today, _now);

        Assert.Equal("Charges.DateInvalid", returned.Error!.Code);
        Assert.Equal(DepositReturnStatus.ToReturn, deposit.Return);
    }

    [Fact]
    [Trait("spec", Spec + ": Devolución de la fianza (Devolución correcta)")]
    public void A_note_of_500_characters_is_accepted_and_501_rejected()
    {
        Assert.True(DueBack().MarkReturned(null, new string('a', 500), _today, _now).IsSuccess);

        var tooLong = DueBack().MarkReturned(null, new string('a', 501), _today, _now);

        Assert.Equal("Charges.NoteTooLong", tooLong.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Devolución de la fianza (Fianza no pagada)")]
    public void A_deposit_that_is_not_due_back_cannot_be_returned()
    {
        Assert.Equal("Charges.InvalidStatus", Deposit().MarkReturned(null, null, _today, _now).Error!.Code);
        Assert.Equal("Charges.InvalidStatus", PaidDeposit().MarkReturned(null, null, _today, _now).Error!.Code);

        var twice = DueBack();
        twice.MarkReturned(null, null, _today, _now);
        Assert.Equal("Charges.InvalidStatus", twice.MarkReturned(null, null, _today, _now).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Corrección de una devolución (Devolución marcada por error)")]
    public void Reverting_a_return_makes_it_due_back_again_and_needs_a_reason()
    {
        var deposit = DueBack();
        deposit.MarkReturned(null, "Nota", _today, _now);

        Assert.Equal("Charges.ReasonRequired", deposit.RevertReturn(" ", _now).Error!.Code);
        var reverted = deposit.RevertReturn("Marcada per error", _now);

        Assert.True(reverted.IsSuccess);
        Assert.Equal(DepositReturnStatus.ToReturn, deposit.Return);
        Assert.Null(deposit.ReturnedOn);
        Assert.Null(deposit.ReturnNote);
        Assert.Equal("Marcada per error", reverted.Value!.Reason);
    }

    [Fact]
    [Trait("spec", Spec + ": Corrección de una devolución (Revertir el pago de una devuelta)")]
    public void A_returned_deposit_cannot_be_reverted_to_pending_before_reverting_the_return()
    {
        var deposit = DueBack();
        deposit.MarkReturned(null, null, _today, _now);

        var reverted = deposit.Revert("Error", _now);

        Assert.Equal("Charges.MustRevertReturnFirst", reverted.Error!.Code);
        Assert.Equal(ChargeStatus.Paid, deposit.Status);
    }

    [Fact]
    [Trait("spec", Spec + ": Reactivación del alumno y fianza (Reactivado con la fianza aún por devolver)")]
    public void Cancelling_the_return_due_leaves_the_deposit_paid_and_current()
    {
        var deposit = DueBack();

        var cancelled = deposit.CancelReturnDue(_now);

        Assert.True(cancelled.IsSuccess);
        Assert.Equal(DepositReturnStatus.None, deposit.Return);
        Assert.Equal(ChargeStatus.Paid, deposit.Status);
        Assert.True(deposit.IsCurrentDeposit);
    }

    [Fact]
    [Trait("spec", Spec + ": Corrección de una devolución (Revertir el pago de una devuelta)")]
    public void Reverting_a_paid_deposit_that_is_due_back_clears_the_return()
    {
        var deposit = DueBack();

        Assert.True(deposit.Revert("Error", _now).IsSuccess);

        Assert.Equal(ChargeStatus.Pending, deposit.Status);
        Assert.Equal(DepositReturnStatus.None, deposit.Return);
    }
}
