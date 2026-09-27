// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.SchoolYears;
using Arca.Application.Students;
using Arca.Domain.Charges;
using Arca.Domain.Common;

namespace Arca.Application.Charges.VoidCharge;

/// <summary>
/// Voids a pending charge, with a reason: a final state that no longer counts as debt (pagaments, D1). A charge that
/// is not pending has to be reverted first.
/// </summary>
public sealed class VoidChargeHandler(
    IChargeRepository charges, IChargeEventRepository events, IStudentRepository students, IAcademicYearRepository years, IUnitOfWork unit, IClock clock)
{
    public Task<Result<ChargeRow>> HandleAsync(VoidChargeRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var charge = await charges.GetAsync(request.ChargeId, token);
            if (charge is null)
            {
                return Result<ChargeRow>.Failure(ChargeErrors.NotFound);
            }

            var voided = charge.Void(request.Reason, clock.UtcNow);
            if (!voided.IsSuccess)
            {
                return Result<ChargeRow>.Failure(voided.Error!);
            }

            await charges.UpdateAsync(charge, token);
            await events.AddAsync(voided.Value!, token);
            return Result<ChargeRow>.Success(await new ChargeViews(students, years).RowAsync(charge, token));
        }, ct);
}
