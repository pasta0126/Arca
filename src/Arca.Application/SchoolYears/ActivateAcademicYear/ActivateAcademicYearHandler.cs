// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Domain.Common;
using Arca.Domain.SchoolYears;

namespace Arca.Application.SchoolYears.ActivateAcademicYear;

/// <summary>Activates a year, only when no other is active. Closing the active one belongs to cursos-i-historial.</summary>
public sealed class ActivateAcademicYearHandler(IAcademicYearRepository years, IUnitOfWork unit)
{
    public Task<Result<AcademicYearSummary>> HandleAsync(ActivateAcademicYearRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var year = await years.GetAsync(request.YearId, token);
            if (year is null)
            {
                return Result<AcademicYearSummary>.Failure(SchoolYearErrors.NotFound);
            }

            var done = year.Activate(await years.ListAsync(token));
            if (!done.IsSuccess)
            {
                return Result<AcademicYearSummary>.Failure(done.Error!);
            }

            await years.UpdateAsync(year, token);
            return Result<AcademicYearSummary>.Success(AcademicYearSummary.Of(year));
        }, ct);
}
