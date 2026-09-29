// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.ConceptAmounts;
using Arca.Application.Feedback;
using Arca.Application.Localization;

namespace Arca.Application.Charges;

/// <summary>
/// The confirmations of the charge operations that need one, each saying its consequence before anything happens
/// (pagaments, D10): reverting a payment, the bulk operations, and voiding, which is final.
/// </summary>
public sealed class ChargeConfirmations(ILocalizer localizer)
{
    readonly ChargeResultTexts _texts = new(localizer);

    /// <summary>Reverting brings a paid, exempt or waived charge back to pending, so it counts as debt again.</summary>
    public ConfirmationRequest ForRevert(ChargeRow charge) => new(
        localizer.Get("Charges.Label.RevertTitle", _texts.ConceptName(charge.Concept), charge.StudentName),
        localizer.Get("Charges.Label.RevertConsequence", _texts.StatusName(charge.Status), charge.Amount),
        localizer.Get("Charges.Label.RevertConfirm"));

    /// <summary>The same confirmation for a screen that knows the charge by the stable names of its concept and status.</summary>
    public ConfirmationRequest ForRevert(string concept, string studentName, string status, decimal amount) => new(
        localizer.Get("Charges.Label.RevertTitle", ConceptNames.Of(localizer, concept), studentName),
        localizer.Get("Charges.Label.RevertConsequence", _texts.StatusName(status), amount),
        localizer.Get("Charges.Label.RevertConfirm"));

    /// <summary>The same confirmation for a screen that knows the charge by the stable name of its concept.</summary>
    public ConfirmationRequest ForVoid(string concept, string studentName, decimal amount) => new(
        localizer.Get("Charges.Label.VoidTitle", ConceptNames.Of(localizer, concept), studentName),
        localizer.Get("Charges.Label.VoidConsequence", amount),
        localizer.Get("Charges.Label.VoidConfirm"),
        Destructive: true);

    /// <summary>Voiding is final: the charge stops counting and cannot be reverted.</summary>
    public ConfirmationRequest ForVoid(ChargeRow charge) => new(
        localizer.Get("Charges.Label.VoidTitle", _texts.ConceptName(charge.Concept), charge.StudentName),
        localizer.Get("Charges.Label.VoidConsequence", charge.Amount),
        localizer.Get("Charges.Label.VoidConfirm"),
        Destructive: true);

    /// <summary>A bulk waiver, with how many charges and how much, before anything changes.</summary>
    public ConfirmationRequest ForBulkWaive(BulkChargePlan plan) => new(
        localizer.Get("Charges.Label.BulkWaiveTitle", plan.Eligible.Count),
        localizer.Get("Charges.Label.BulkWaiveConsequence", plan.Eligible.Count, plan.TotalAmount),
        localizer.Get("Charges.Label.BulkWaiveConfirm"));

    /// <summary>A bulk return of deposits, with how many and how much, before anything changes.</summary>
    public ConfirmationRequest ForBulkReturn(BulkChargePlan plan) => new(
        localizer.Get("Charges.Label.BulkReturnTitle", plan.Eligible.Count),
        localizer.Get("Charges.Label.BulkReturnConsequence", plan.Eligible.Count, plan.TotalAmount),
        localizer.Get("Charges.Label.BulkReturnConfirm"));
}
