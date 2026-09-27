// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.SchoolYears;
using Arca.Application.Students;
using Arca.Domain.Charges;
using Arca.Domain.Common;

namespace Arca.Application.Charges.WaiveCharge;

/// <summary>Waives a pending charge, with a reason. It stops counting as debt (pagaments, D1).</summary>
public sealed class WaiveChargeHandler(
    IChargeRepository charges, IChargeEventRepository events, IStudentRepository students, IAcademicYearRepository years, IUnitOfWork unit, IClock clock)
{
    public Task<Result<ChargeRow>> HandleAsync(WaiveChargeRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var charge = await charges.GetAsync(request.ChargeId, token);
            if (charge is null)
            {
                return Result<ChargeRow>.Failure(ChargeErrors.NotFound);
            }

            var waived = charge.Waive(request.Reason, clock.UtcNow);
            if (!waived.IsSuccess)
            {
                return Result<ChargeRow>.Failure(waived.Error!);
            }

            await charges.UpdateAsync(charge, token);
            await events.AddAsync(waived.Value!, token);
            return Result<ChargeRow>.Success(await new ChargeViews(students, years).RowAsync(charge, token));
        }, ct);
}
