// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.SchoolYears;
using Arca.Application.Students;
using Arca.Domain.Charges;
using Arca.Domain.Common;

namespace Arca.Application.Charges.MarkDepositReturned;

/// <summary>
/// Marks a deposit that is due back as given back, with a date and an optional note (pagaments, D3). A student still in the
/// school never gets it back: it is only given back when they leave.
/// </summary>
public sealed class MarkDepositReturnedHandler(
    IChargeRepository charges, IChargeEventRepository events, IStudentRepository students, IAcademicYearRepository years, IUnitOfWork unit, IClock clock)
{
    public Task<Result<ChargeRow>> HandleAsync(MarkDepositReturnedRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var charge = await charges.GetAsync(request.ChargeId, token);
            if (charge is null)
            {
                return Result<ChargeRow>.Failure(ChargeErrors.NotFound);
            }

            var student = await students.GetAsync(charge.StudentId, token);
            if (student is { IsRetired: false })
            {
                return Result<ChargeRow>.Failure(ChargeErrors.StudentStillActive);
            }

            var returned = charge.MarkReturned(request.ReturnedOn, request.Note, clock.Today, clock.UtcNow);
            if (!returned.IsSuccess)
            {
                return Result<ChargeRow>.Failure(returned.Error!);
            }

            await charges.UpdateAsync(charge, token);
            await events.AddAsync(returned.Value!, token);
            return Result<ChargeRow>.Success(await new ChargeViews(students, years).RowAsync(charge, token));
        }, ct);
}
