// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;
using Arca.Domain.SchoolYears;

namespace Arca.Application.SchoolYears;

/// <summary>
/// The way every use case that writes enrolments or assignments gets the year to write in (alumnes-i-assignacions, D12):
/// the active year, checked through the guard for that kind of operation. Without an active year the error says that
/// one has to be created or activated first.
/// </summary>
public static class ActiveYear
{
    public static async Task<Result<AcademicYear>> RequireAsync(
        IAcademicYearRepository years, YearOperation operation, CancellationToken ct)
    {
        var active = await years.GetActiveAsync(ct);
        var refused = YearGuard.Check(active, operation);
        return refused is null ? Result<AcademicYear>.Success(active!) : Result<AcademicYear>.Failure(refused);
    }
}
