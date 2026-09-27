// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Domain.Common;
using Arca.Domain.SchoolYears;

namespace Arca.Application.SchoolYears.CreateAcademicYear;

/// <summary>Creates a school year. The first one of the system is created active.</summary>
public sealed class CreateAcademicYearHandler(IAcademicYearRepository years, IUnitOfWork unit)
{
    public Task<Result<AcademicYearSummary>> HandleAsync(CreateAcademicYearRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var created = AcademicYear.Create(Guid.NewGuid(), request.StartDate, request.EndDate, await years.ListAsync(token));
            if (!created.IsSuccess)
            {
                return Result<AcademicYearSummary>.Failure(created.Error!);
            }

            await years.AddAsync(created.Value!, token);
            return Result<AcademicYearSummary>.Success(AcademicYearSummary.Of(created.Value!));
        }, ct);
}
