// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.SchoolYears;
using Arca.Application.Students;
using Arca.Domain.Charges;
using Arca.Domain.Common;

namespace Arca.Application.Charges.MarkChargePaid;

/// <summary>
/// Marks a pending charge as paid, from any school year: the charges of past years are managed the same way as those
/// of the active one (pagaments, D1).
/// </summary>
public sealed class MarkChargePaidHandler(
    IChargeRepository charges, IChargeEventRepository events, IStudentRepository students, IAcademicYearRepository years, IUnitOfWork unit, IClock clock)
{
    public Task<Result<ChargeRow>> HandleAsync(MarkChargePaidRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var charge = await charges.GetAsync(request.ChargeId, token);
            if (charge is null)
            {
                return Result<ChargeRow>.Failure(ChargeErrors.NotFound);
            }

            var marked = charge.MarkPaid(request.PaidOn, clock.Today, clock.UtcNow);
            if (!marked.IsSuccess)
            {
                return Result<ChargeRow>.Failure(marked.Error!);
            }

            await charges.UpdateAsync(charge, token);
            await events.AddAsync(marked.Value!, token);
            return Result<ChargeRow>.Success(await new ChargeViews(students, years).RowAsync(charge, token));
        }, ct);
}
