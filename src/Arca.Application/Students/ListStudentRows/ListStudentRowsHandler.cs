// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Charges;
using Arca.Application.Students.SearchStudents;
using Arca.Domain.Common;

namespace Arca.Application.Students.ListStudentRows;

/// <summary>
/// A student as the list of the Students section shows it: name, level, group, locker and whether they owe anything, and how much.
/// It has no email and no identifier, and no reason for the debt: that is in the record of their payments.
/// </summary>
public sealed record StudentListRow(
    Guid Id, string FirstName, string LastName, string? LevelName, string? GroupName, bool IsRetired, int? LockerNumber,
    bool HasDebt, decimal PendingTotal);

/// <param name="Rows">Every student of the active year, retired ones included, by surname: the screen narrows them with its search and filters.</param>
public sealed record StudentRowsListing(IReadOnlyList<StudentListRow> Rows, StudentCounters Counters, StudentEmptyState EmptyState);

/// <summary>
/// The list of students for its screen (pantalles-de-domini, D6): the students of the active year as <see cref="SearchStudentsHandler"/>
/// finds them, with the payment state added from one batch of pending charges, not one query per student. No rule of its own.
/// </summary>
public sealed class ListStudentRowsHandler(SearchStudentsHandler search, IChargeRepository charges)
{
    public async Task<Result<StudentRowsListing>> HandleAsync(CancellationToken ct)
    {
        var listing = await search.HandleAsync(new SearchStudentsRequest(new StudentFilter(IncludeRetired: true)), ct);
        if (!listing.IsSuccess)
        {
            return Result<StudentRowsListing>.Failure(listing.Error!);
        }

        var owed = (await charges.ListPendingAsync(ct))
            .Where(c => c.CountsAsDebt)
            .GroupBy(c => c.StudentId)
            .ToDictionary(g => g.Key, g => g.Aggregate(Money.Zero, (sum, c) => sum + c.Amount).Amount);
        var rows = listing.Value!.Rows
            .Select(r => new StudentListRow(
                r.Id, r.FirstName, r.LastName, r.LevelName, r.GroupName, r.IsRetired, r.LockerNumber,
                owed.ContainsKey(r.Id), owed.GetValueOrDefault(r.Id)))
            .ToList();
        return Result<StudentRowsListing>.Success(new StudentRowsListing(rows, listing.Value.Counters, listing.Value.EmptyState));
    }
}
