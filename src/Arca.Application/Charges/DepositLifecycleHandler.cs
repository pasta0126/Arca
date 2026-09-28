// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Common;
using Arca.Domain.Charges;
using Arca.Domain.Students;

namespace Arca.Application.Charges;

/// <summary>
/// Applies the deposit rules when a student leaves or comes back (pagaments, D3/D4), inside the same transaction as the
/// retirement or reactivation. On leaving: a paid deposit is due back, a pending one is voided with the retirement reason,
/// and an exempt or waived one, like the fees, stays as it is. On coming back: a deposit still due back is simply paid
/// again; a returned or voided one stays so, and a new one is generated on the next assignment.
/// </summary>
public sealed class DepositLifecycleHandler(IChargeRepository charges, IChargeEventRepository events, IClock clock) : IStudentLifecycleHandler
{
    public async Task OnRetiredAsync(Student student, OperationContext operation, CancellationToken ct)
    {
        var now = clock.UtcNow;
        foreach (var deposit in await CurrentDepositsAsync(student, ct))
        {
            var change = deposit.Status switch
            {
                ChargeStatus.Paid when deposit.Return == DepositReturnStatus.None => deposit.MarkReturnDue(now),
                ChargeStatus.Pending => deposit.Void(operation.Reason ?? student.RetirementReason, now),
                _ => null,
            };

            if (change is { IsSuccess: true })
            {
                await charges.UpdateAsync(deposit, ct);
                await events.AddAsync(change.Value!, ct);
            }
        }
    }

    public async Task OnReactivatedAsync(Student student, OperationContext operation, CancellationToken ct)
    {
        var now = clock.UtcNow;
        foreach (var deposit in (await CurrentDepositsAsync(student, ct)).Where(d => d.Return == DepositReturnStatus.ToReturn))
        {
            var cancelled = deposit.CancelReturnDue(now);
            await charges.UpdateAsync(deposit, ct);
            await events.AddAsync(cancelled.Value!, ct);
        }
    }

    async Task<IReadOnlyList<Charge>> CurrentDepositsAsync(Student student, CancellationToken ct) =>
        [.. (await charges.ListByStudentAsync(student.Id, ct)).Where(c => c.IsCurrentDeposit)];
}
