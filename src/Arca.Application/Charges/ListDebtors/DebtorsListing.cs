// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Charges;
using Arca.Domain.ConceptAmounts;

namespace Arca.Application.Charges.ListDebtors;

/// <summary>
/// The filters of the debtors query. Year and concept narrow which pending charges count; level, group and zone narrow
/// which students appear, by their enrolment in the active year and by the locker they hold now.
/// </summary>
public sealed record DebtorFilter(Guid? YearId = null, ChargeConcept? Concept = null, Guid? LevelId = null, Guid? GroupId = null, Guid? ZoneId = null);

/// <param name="Filter">Optional filters.</param>
public sealed record ListDebtorsRequest(DebtorFilter? Filter = null);

/// <summary>
/// A student with pending charges. It carries no email, no external identifier and no reasons: those belong to the
/// student's record and to the charge itself (pagaments, D8).
/// </summary>
public sealed record DebtorRow(
    Guid StudentId, string FirstName, string LastName, string? LevelName, string? GroupName, bool IsRetired, int? LockerNumber,
    decimal PendingTotal, IReadOnlyList<DebtLine> Breakdown);

/// <summary>What the debtors query says when there is nothing to list.</summary>
public enum DebtorsEmptyState
{
    /// <summary>There are rows.</summary>
    None,

    /// <summary>No charge is pending at all: good news, not an empty list.</summary>
    NoDebt,

    /// <summary>There is debt, but not with these filters.</summary>
    NoResults,
}

/// <param name="Rows">The debtors, ordered by last name.</param>
/// <param name="StudentCount">How many students the filtered list has.</param>
/// <param name="PendingTotal">The total owed by them, under the filters.</param>
/// <param name="OverallStudentCount">How many students owe anything, with no filter.</param>
/// <param name="OverallPendingTotal">The total owed, with no filter.</param>
/// <param name="EmptyState">Why the list is empty, if it is.</param>
public sealed record DebtorsListing(
    IReadOnlyList<DebtorRow> Rows, int StudentCount, decimal PendingTotal, int OverallStudentCount, decimal OverallPendingTotal, DebtorsEmptyState EmptyState);
