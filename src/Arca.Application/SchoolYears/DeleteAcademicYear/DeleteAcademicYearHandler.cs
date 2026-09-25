// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Domain.Common;
using Arca.Domain.SchoolYears;

namespace Arca.Application.SchoolYears.DeleteAcademicYear;

/// <summary>Deletes a year that has no enrolments and no assignments. Returns the identity of the year removed.</summary>
public sealed class DeleteAcademicYearHandler(IAcademicYearRepository years, IUnitOfWork unit)
{
    public Task<Result<Guid>> HandleAsync(DeleteAcademicYearRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var year = await years.GetAsync(request.YearId, token);
            if (year is null)
            {
                return Result<Guid>.Failure(SchoolYearErrors.NotFound);
            }

            var allowed = year.CheckCanDelete(await years.HasDataAsync(year.Id, token));
            if (!allowed.IsSuccess)
            {
                return Result<Guid>.Failure(allowed.Error!);
            }

            await years.RemoveAsync(year, token);
            return Result<Guid>.Success(year.Id);
        }, ct);
}
