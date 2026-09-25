// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;
using Arca.Domain.SchoolYears;

namespace Arca.Application.SchoolYears.GetAcademicYear;

public sealed class GetAcademicYearHandler(IAcademicYearRepository years)
{
    public async Task<Result<AcademicYearSummary>> HandleAsync(GetAcademicYearRequest request, CancellationToken ct)
    {
        var year = await years.GetAsync(request.YearId, ct);
        return year is null
            ? Result<AcademicYearSummary>.Failure(SchoolYearErrors.NotFound)
            : Result<AcademicYearSummary>.Success(AcademicYearSummary.Of(year));
    }
}
