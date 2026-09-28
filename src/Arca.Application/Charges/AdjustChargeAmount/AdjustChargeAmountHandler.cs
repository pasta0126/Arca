// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.SchoolYears;
using Arca.Application.Students;
using Arca.Domain.Charges;
using Arca.Domain.Common;

namespace Arca.Application.Charges.AdjustChargeAmount;

/// <summary>Changes the amount of a pending charge, with a reason and within the same limits as the year's amounts (pagaments, D1).</summary>
public sealed class AdjustChargeAmountHandler(
    IChargeRepository charges, IChargeEventRepository events, IStudentRepository students, IAcademicYearRepository years, IUnitOfWork unit, IClock clock)
{
    public Task<Result<ChargeRow>> HandleAsync(AdjustChargeAmountRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var charge = await charges.GetAsync(request.ChargeId, token);
            if (charge is null)
            {
                return Result<ChargeRow>.Failure(ChargeErrors.NotFound);
            }

            var adjusted = charge.AdjustAmount(request.Amount, request.Reason, clock.UtcNow);
            if (!adjusted.IsSuccess)
            {
                return Result<ChargeRow>.Failure(adjusted.Error!);
            }

            await charges.UpdateAsync(charge, token);
            await events.AddAsync(adjusted.Value!, token);
            return Result<ChargeRow>.Success(await new ChargeViews(students, years).RowAsync(charge, token));
        }, ct);
}
