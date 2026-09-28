// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.ConceptAmounts;

namespace Arca.Application.ConceptAmounts;

/// <summary>
/// The three amounts of a year as a screen shows them (pagaments, D5): defined, or proposed from the previous year when
/// none is defined yet, or empty when there is no previous year either. Editable only while the year has not finished.
/// </summary>
public sealed record ConceptAmountsView(
    Guid YearId, decimal? Fee, decimal? Deposit, decimal? KeyReplacementFee, bool IsEditable, bool IsProposed)
{
    /// <summary>Whether the amounts needed to assign a locker (the fee and the deposit) are defined for the year.</summary>
    public bool IsComplete => Fee is not null && Deposit is not null;

    public static ConceptAmountsView Of(Guid yearId, IReadOnlyList<ConceptAmount> amounts, bool isEditable, bool isProposed) => new(
        yearId, AmountOf(amounts, ChargeConcept.Fee), AmountOf(amounts, ChargeConcept.Deposit), AmountOf(amounts, ChargeConcept.KeyReplacementFee),
        isEditable, isProposed);

    static decimal? AmountOf(IReadOnlyList<ConceptAmount> amounts, ChargeConcept concept) =>
        amounts.FirstOrDefault(a => a.Concept == concept)?.Amount.Amount;
}
