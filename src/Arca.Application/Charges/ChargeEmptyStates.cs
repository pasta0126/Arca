// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.ConceptAmounts;
using Arca.Application.Charges.ListDebtors;
using Arca.Application.Charges.ListDepositsDueBack;
using Arca.Application.Charges.GetStudentPayment;
using Arca.Application.Localization;

namespace Arca.Application.Charges;

/// <summary>What an empty screen of the charges can suggest doing next.</summary>
public enum ChargeSuggestedAction
{
    ClearFilters,
    DefineAmounts,
}

/// <summary>The guidance of an empty screen: what it means and what to do about it (UX transversal).</summary>
public sealed record ChargeEmptyStateGuide(string Message, IReadOnlyList<ChargeSuggestedAction> Actions);

/// <summary>The messages of the charge screens when there is nothing to list, so none is an unexplained empty list.</summary>
public static class ChargeEmptyStates
{
    /// <summary>A student with no charges: they are generated when a locker is assigned.</summary>
    public static ChargeEmptyStateGuide? ForStudent(StudentPayment payment, ILocalizer localizer) =>
        payment.Charges.Count == 0 ? new ChargeEmptyStateGuide(localizer.Get("Charges.Empty.NoCharges"), []) : null;

    /// <summary>No debt at all is good news and says so; debt hidden by the filters offers to clear them.</summary>
    public static ChargeEmptyStateGuide? ForDebtors(DebtorsListing listing, ILocalizer localizer) => listing.EmptyState switch
    {
        DebtorsEmptyState.NoDebt => new ChargeEmptyStateGuide(localizer.Get("Charges.Empty.NoDebt"), []),
        DebtorsEmptyState.NoResults => new ChargeEmptyStateGuide(localizer.Get("Charges.Empty.NoDebtResults"), [ChargeSuggestedAction.ClearFilters]),
        _ => null,
    };

    public static ChargeEmptyStateGuide? ForDepositsDueBack(DepositsDueBackListing listing, ILocalizer localizer) =>
        listing.Rows.Count > 0 ? null
            : listing.IsEmpty ? new ChargeEmptyStateGuide(localizer.Get("Charges.Empty.NoDepositsDue"), [])
            : new ChargeEmptyStateGuide(localizer.Get("Charges.Empty.NoDepositsDueResults"), [ChargeSuggestedAction.ClearFilters]);

    /// <summary>The amounts of the year are not defined: nothing can be assigned until they are.</summary>
    public static ChargeEmptyStateGuide? ForAmounts(ConceptAmountsView view, ILocalizer localizer) =>
        view.IsComplete ? null : new ChargeEmptyStateGuide(localizer.Get("Charges.Empty.AmountsNotDefined"), [ChargeSuggestedAction.DefineAmounts]);

    /// <summary>The label of a suggested action, for its button.</summary>
    public static string Label(ChargeSuggestedAction action, ILocalizer localizer) => action switch
    {
        ChargeSuggestedAction.ClearFilters => localizer.Get("Charges.Label.ClearFilters"),
        ChargeSuggestedAction.DefineAmounts => localizer.Get("Charges.Label.DefineAmounts"),
        _ => string.Empty,
    };
}
