// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Domain.Charges;
using Arca.Domain.ConceptAmounts;

namespace Arca.Application.Charges;

/// <summary>
/// The texts that say what a charge operation did, with the counts and amounts that matter (pagaments, D10). They are
/// composed here from keys so no interface text is written in code. They never include the reason a person typed.
/// </summary>
public sealed class ChargeResultTexts(ILocalizer localizer)
{
    public string Paid(ChargeRow charge) => Of("Charges.Result.Paid", charge);

    public string Exempted(ChargeRow charge) => Of("Charges.Result.Exempted", charge);

    public string Waived(ChargeRow charge) => Of("Charges.Result.Waived", charge);

    public string Reverted(ChargeRow charge) => Of("Charges.Result.Reverted", charge);

    public string Voided(ChargeRow charge) => Of("Charges.Result.Voided", charge);

    public string AmountAdjusted(ChargeRow charge) => Of("Charges.Result.AmountAdjusted", charge);

    public string KeyReplacementCharged(ChargeRow charge) => Of("Charges.Result.KeyReplacementCharged", charge);

    public string DepositReturned(ChargeRow charge) => Of("Charges.Result.DepositReturned", charge);

    public string DepositReturnReverted(ChargeRow charge) => Of("Charges.Result.DepositReturnReverted", charge);

    /// <summary>The outcome of a bulk waiver: how many charges and how much.</summary>
    public string BulkWaived(BulkChargeResult result) =>
        localizer.Get(result.Count == 1 ? "Charges.Result.BulkWaivedOne" : "Charges.Result.BulkWaivedMany", result.Count, result.TotalAmount);

    /// <summary>The outcome of a bulk return: how many deposits and how much.</summary>
    public string BulkReturned(BulkChargeResult result) =>
        localizer.Get(result.Count == 1 ? "Charges.Result.BulkReturnedOne" : "Charges.Result.BulkReturnedMany", result.Count, result.TotalAmount);

    /// <summary>The name of a concept as the interface says it. The deposit always says it is the locker's.</summary>
    public string ConceptName(ChargeConcept concept) => concept switch
    {
        ChargeConcept.Fee => localizer.Get("Charges.Concept.Fee"),
        ChargeConcept.Deposit => localizer.Get("Charges.Concept.Deposit"),
        ChargeConcept.KeyReplacementFee => localizer.Get("Charges.Concept.KeyReplacementFee"),
        _ => string.Empty,
    };

    /// <summary>The name of a status of a charge, in the plain words of the interface.</summary>
    public string StatusName(ChargeStatus status) => status switch
    {
        ChargeStatus.Pending => localizer.Get("Charges.Status.Pending"),
        ChargeStatus.Paid => localizer.Get("Charges.Status.Paid"),
        ChargeStatus.Exempt => localizer.Get("Charges.Status.Exempt"),
        ChargeStatus.Waived => localizer.Get("Charges.Status.Waived"),
        ChargeStatus.Voided => localizer.Get("Charges.Status.Voided"),
        _ => string.Empty,
    };

    string Of(string key, ChargeRow charge) => localizer.Get(key, ConceptName(charge.Concept), charge.StudentName, charge.Amount);
}
