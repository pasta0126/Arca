// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.SchoolYears.ListAcademicYears;

/// <summary>The school years, the most recent first, active or not: the past ones are consulted as history.</summary>
public sealed class ListAcademicYearsHandler(IAcademicYearRepository years)
{
    public async Task<Result<IReadOnlyList<AcademicYearSummary>>> HandleAsync(CancellationToken ct)
    {
        IReadOnlyList<AcademicYearSummary> listed =
            [.. (await years.ListAsync(ct)).OrderByDescending(y => y.StartDate).Select(AcademicYearSummary.Of)];
        return Result<IReadOnlyList<AcademicYearSummary>>.Success(listed);
    }
}
