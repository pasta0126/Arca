// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.SchoolYears;
using Arca.Application.Students;
using Arca.Domain.Charges;
using Arca.Domain.Common;

namespace Arca.Application.Charges.RevertDepositReturn;

/// <summary>
/// Corrects a give-back marked by mistake: the deposit is due back again, and the history keeps both events. It is refused
/// if the student is back in the school or already has another current deposit.
/// </summary>
public sealed class RevertDepositReturnHandler(
    IChargeRepository charges, IChargeEventRepository events, IStudentRepository students, IAcademicYearRepository years, IUnitOfWork unit, IClock clock)
{
    public Task<Result<ChargeRow>> HandleAsync(RevertDepositReturnRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var charge = await charges.GetAsync(request.ChargeId, token);
            if (charge is null)
            {
                return Result<ChargeRow>.Failure(ChargeErrors.NotFound);
            }

            if (charge.Return == DepositReturnStatus.Returned)
            {
                // Keep the invariant that a deposit is due back only while its student has left, and that a student has at
                // most one current deposit: undoing the give-back would break either if they came back and got a new one.
                if (await students.GetAsync(charge.StudentId, token) is { IsRetired: false })
                {
                    return Result<ChargeRow>.Failure(ChargeErrors.ReturnStudentActive);
                }

                if ((await charges.ListByStudentAsync(charge.StudentId, token)).Any(c => c.Id != charge.Id && c.IsCurrentDeposit))
                {
                    return Result<ChargeRow>.Failure(ChargeErrors.CurrentDepositExists);
                }
            }

            var reverted = charge.RevertReturn(request.Reason, clock.UtcNow);
            if (!reverted.IsSuccess)
            {
                return Result<ChargeRow>.Failure(reverted.Error!);
            }

            await charges.UpdateAsync(charge, token);
            await events.AddAsync(reverted.Value!, token);
            return Result<ChargeRow>.Success(await new ChargeViews(students, years).RowAsync(charge, token));
        }, ct);
}
