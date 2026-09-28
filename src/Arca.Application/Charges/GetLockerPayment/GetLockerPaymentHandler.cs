// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.SchoolYears;
using Arca.Domain.Charges;
using Arca.Domain.Common;

namespace Arca.Application.Charges.GetLockerPayment;

/// <param name="LockerId">The locker.</param>
public sealed record GetLockerPaymentRequest(Guid LockerId);

/// <summary>
/// The payment state of an occupied locker: that of its assigned student. <paramref name="Standing"/> is null when the
/// locker is free, because a free locker shows no payment state.
/// </summary>
public sealed record LockerPayment(PaymentStanding? Standing);

/// <summary>The payment state of a locker, taken from its student's standing (pagaments, D7).</summary>
public sealed class GetLockerPaymentHandler(IAssignmentRepository assignments, IChargeRepository charges, IAcademicYearRepository years)
{
    public async Task<Result<LockerPayment>> HandleAsync(GetLockerPaymentRequest request, CancellationToken ct)
    {
        var current = await assignments.GetCurrentOfLockerAsync(request.LockerId, ct);
        if (current is null)
        {
            return Result<LockerPayment>.Success(new LockerPayment(null));
        }

        var active = await years.GetActiveAsync(ct);
        return Result<LockerPayment>.Success(new LockerPayment(PaymentStanding.Of(await charges.ListByStudentAsync(current.StudentId, ct), active?.Id)));
    }
}
