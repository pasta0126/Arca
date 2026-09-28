// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.SchoolYears;
using Arca.Application.Students;
using Arca.Domain.Charges;
using Arca.Domain.Common;

namespace Arca.Application.Charges.RevertCharge;

/// <summary>Reverts a paid, exempt or waived charge back to pending, with a reason (pagaments, D1).</summary>
public sealed class RevertChargeHandler(
    IChargeRepository charges, IChargeEventRepository events, IStudentRepository students, IAcademicYearRepository years, IUnitOfWork unit, IClock clock)
{
    public Task<Result<ChargeRow>> HandleAsync(RevertChargeRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var charge = await charges.GetAsync(request.ChargeId, token);
            if (charge is null)
            {
                return Result<ChargeRow>.Failure(ChargeErrors.NotFound);
            }

            var reverted = charge.Revert(request.Reason, clock.UtcNow);
            if (!reverted.IsSuccess)
            {
                return Result<ChargeRow>.Failure(reverted.Error!);
            }

            await charges.UpdateAsync(charge, token);
            await events.AddAsync(reverted.Value!, token);
            return Result<ChargeRow>.Success(await new ChargeViews(students, years).RowAsync(charge, token));
        }, ct);
}
