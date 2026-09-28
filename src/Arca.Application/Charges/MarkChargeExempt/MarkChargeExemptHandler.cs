// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.SchoolYears;
using Arca.Application.Students;
using Arca.Domain.Charges;
using Arca.Domain.Common;

namespace Arca.Application.Charges.MarkChargeExempt;

/// <summary>Marks a pending charge as exempt, with a reason. It stops counting as debt (pagaments, D1).</summary>
public sealed class MarkChargeExemptHandler(
    IChargeRepository charges, IChargeEventRepository events, IStudentRepository students, IAcademicYearRepository years, IUnitOfWork unit, IClock clock)
{
    public Task<Result<ChargeRow>> HandleAsync(MarkChargeExemptRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var charge = await charges.GetAsync(request.ChargeId, token);
            if (charge is null)
            {
                return Result<ChargeRow>.Failure(ChargeErrors.NotFound);
            }

            var marked = charge.MarkExempt(request.Reason, clock.UtcNow);
            if (!marked.IsSuccess)
            {
                return Result<ChargeRow>.Failure(marked.Error!);
            }

            await charges.UpdateAsync(charge, token);
            await events.AddAsync(marked.Value!, token);
            return Result<ChargeRow>.Success(await new ChargeViews(students, years).RowAsync(charge, token));
        }, ct);
}
