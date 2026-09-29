// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Charges;
using Arca.Application.SchoolYears;
using Arca.Domain.Common;

namespace Arca.Application.GlobalState;

/// <summary>A school year as the header shows it.</summary>
public sealed record YearSummary(Guid Id, string Name);

/// <summary>
/// What the frame of the application needs to know at every moment (ui-shell, D3): which year is active and how many things
/// wait for attention. Only what is defined so far: later changes add the year that is closing, the keys and incidents, the
/// new version and the guided setup.
/// </summary>
/// <param name="ActiveYear">The active year, or null when there is none.</param>
/// <param name="PendingCharges">How many charges are pending, of any year: what the Payments section shows as needing attention.</param>
public sealed record GlobalState(YearSummary? ActiveYear, int PendingCharges)
{
    public bool HasActiveYear => ActiveYear is not null;
}

/// <summary>
/// The global state in a single query, built from the queries of the domain capabilities and computing no rules of its own.
/// It is asked at the start and after every write, never by polling.
/// </summary>
public sealed class GetGlobalStateHandler(IAcademicYearRepository years, IChargeRepository charges)
{
    public async Task<Result<GlobalState>> HandleAsync(CancellationToken ct)
    {
        var active = await years.GetActiveAsync(ct);
        var pending = await charges.ListPendingAsync(ct);
        return Result<GlobalState>.Success(new GlobalState(active is null ? null : new YearSummary(active.Id, active.Name), pending.Count));
    }
}
