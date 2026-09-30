// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.GlobalState;
using Arca.Application.Lockers;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Students;
using Arca.Application.Students.SearchStudents;
using Arca.Domain.Common;

namespace Arca.Application.Home;

/// <summary>
/// What the start screen shows (ui-llistats-i-detall, Inicio como pantalla registrable): the active year and how many lockers there are in each
/// status, how many students have no locker and how many have something pending. Only counts of things, never an amount and never a name.
/// </summary>
/// <param name="ActiveYearName">The name of the active year, or null when there is none.</param>
/// <param name="HasLockers">Whether there is any locker at all, to tell a centre that is not set up from one that is.</param>
/// <param name="HasStudents">Whether any student is enrolled in the active year.</param>
public sealed record HomeSummary(
    string? ActiveYearName, LockerCounters Lockers, int StudentsWithoutLocker, int StudentsWithPending, bool HasLockers, bool HasStudents);

/// <summary>
/// Builds the summary of the start screen from the queries of the lockers, the students and the global state. It computes no rule of its
/// own. Without an active year the students are not counted: there is nobody to assign yet.
/// </summary>
public sealed class GetHomeSummaryHandler(ListLockerRowsHandler lockers, SearchStudentsHandler students, GetGlobalStateHandler state)
{
    public async Task<Result<HomeSummary>> HandleAsync(CancellationToken ct)
    {
        var global = await state.HandleAsync(ct);
        if (!global.IsSuccess)
        {
            return Result<HomeSummary>.Failure(global.Error!);
        }

        var rows = await lockers.HandleAsync(ct);
        if (!rows.IsSuccess)
        {
            return Result<HomeSummary>.Failure(rows.Error!);
        }

        var counters = new StudentCounters(0, 0, 0);
        if (global.Value!.HasActiveYear)
        {
            var listing = await students.HandleAsync(new SearchStudentsRequest(), ct);
            if (!listing.IsSuccess)
            {
                return Result<HomeSummary>.Failure(listing.Error!);
            }

            counters = listing.Value!.Counters;
        }

        return Result<HomeSummary>.Success(new HomeSummary(
            global.Value.ActiveYear?.Name, rows.Value!.Counters, counters.WithoutLocker, global.Value.StudentsWithPending,
            rows.Value.Counters.Active > 0, counters.Active > 0));
    }
}
