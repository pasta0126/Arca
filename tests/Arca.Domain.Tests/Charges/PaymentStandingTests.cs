// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Charges;
using Arca.Domain.Common;
using Arca.Domain.ConceptAmounts;
using Xunit;

namespace Arca.Domain.Tests.Charges;

public sealed class PaymentStandingTests
{
    const string Spec = "pagaments/cobraments";

    static readonly DateTimeOffset _now = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);
    static readonly DateOnly _today = new(2026, 9, 27);
    static readonly Guid _year = Guid.NewGuid();
    static readonly Guid _previousYear = Guid.NewGuid();

    static Charge Pending(ChargeConcept concept, decimal euros, Guid? year = null) =>
        Charge.Create(Guid.NewGuid(), Guid.NewGuid(), concept, year ?? _year, Money.FromCents((long)(euros * 100)), _now).Charge;

    [Fact]
    [Trait("spec", Spec + ": Estado al corriente de pago de un alumno (Al corriente)")]
    public void Paid_exempt_waived_and_voided_charges_leave_the_student_up_to_date()
    {
        var paid = Pending(ChargeConcept.Fee, 50);
        paid.MarkPaid(null, _today, _now);
        var exempt = Pending(ChargeConcept.Deposit, 20);
        exempt.MarkExempt("Beca", _now);
        var waived = Pending(ChargeConcept.KeyReplacementFee, 10);
        waived.Waive("Motiu", _now);
        var voided = Pending(ChargeConcept.Fee, 50, _previousYear);
        voided.Void("Error", _now);

        var standing = PaymentStanding.Of([paid, exempt, waived, voided], _year);

        Assert.True(standing.UpToDate);
        Assert.False(standing.ByExemption);
        Assert.Equal(0m, standing.PendingTotal);
        Assert.Empty(standing.Breakdown);
    }

    [Fact]
    [Trait("spec", Spec + ": Estado al corriente de pago de un alumno (Con deuda)")]
    public void A_pending_charge_of_any_year_means_owing_with_the_breakdown_by_concept_and_year()
    {
        var charges = new[]
        {
            Pending(ChargeConcept.Fee, 50), Pending(ChargeConcept.Deposit, 20),
            Pending(ChargeConcept.Fee, 45, _previousYear), Pending(ChargeConcept.Fee, 5, _previousYear),
        };

        var standing = PaymentStanding.Of(charges, _year);

        Assert.False(standing.UpToDate);
        Assert.Equal(120m, standing.PendingTotal);
        Assert.Equal(3, standing.Breakdown.Count);
        Assert.All(standing.Breakdown.Take(2), l => Assert.True(l.IsActiveYear));
        var previous = standing.Breakdown[2];
        Assert.False(previous.IsActiveYear);
        Assert.Equal((ChargeConcept.Fee, _previousYear, 50m), (previous.Concept, previous.YearId, previous.Amount));
    }

    [Fact]
    [Trait("spec", Spec + ": Estado al corriente de pago de un alumno (Alumno sin cargos)")]
    public void A_student_with_no_charges_is_up_to_date_and_not_by_exemption()
    {
        var standing = PaymentStanding.Of([], _year);

        Assert.True(standing.UpToDate);
        Assert.False(standing.ByExemption);
    }

    [Fact]
    [Trait("spec", Spec + ": Estado al corriente de pago de un alumno (Exento de todo)")]
    public void A_student_whose_charges_are_all_exempt_is_up_to_date_by_exemption()
    {
        var fee = Pending(ChargeConcept.Fee, 50);
        fee.MarkExempt("Beca", _now);
        var deposit = Pending(ChargeConcept.Deposit, 20);
        deposit.MarkExempt("Beca", _now);

        var standing = PaymentStanding.Of([fee, deposit], _year);

        Assert.True(standing.UpToDate);
        Assert.True(standing.ByExemption);
    }

    [Fact]
    [Trait("spec", Spec + ": Estado al corriente de pago de un alumno (Exento de todo)")]
    public void One_paid_charge_among_exempt_ones_is_up_to_date_but_not_by_exemption()
    {
        var fee = Pending(ChargeConcept.Fee, 50);
        fee.MarkPaid(null, _today, _now);
        var deposit = Pending(ChargeConcept.Deposit, 20);
        deposit.MarkExempt("Beca", _now);

        Assert.False(PaymentStanding.Of([fee, deposit], _year).ByExemption);
    }
}
