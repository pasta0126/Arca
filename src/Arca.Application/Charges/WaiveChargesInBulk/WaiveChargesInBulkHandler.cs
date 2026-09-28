// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Domain.Charges;
using Arca.Domain.Common;

namespace Arca.Application.Charges.WaiveChargesInBulk;

/// <summary>
/// Waives several pending charges with a common reason, in two phases (pagaments, D6): the analysis shows how many and how
/// much would be waived, and the confirmation revalidates and waives them all in one transaction, or none.
/// </summary>
public sealed class WaiveChargesInBulkHandler(IChargeRepository charges, IChargeEventRepository events, IUnitOfWork unit, IClock clock)
{
    readonly BulkChargeEngine _engine = new(charges, events, unit);

    static bool IsEligible(Charge charge) => charge.Status == ChargeStatus.Pending;

    /// <summary>The preview. It can be cancelled: nothing has been saved.</summary>
    public async Task<Result<BulkChargePlan>> AnalyzeAsync(WaiveChargesInBulkRequest request, IProgress<OperationProgress>? progress, CancellationToken ct)
    {
        var reason = Charge.CleanReason(request.Reason);
        return reason.IsSuccess
            ? Result<BulkChargePlan>.Success(await _engine.AnalyzeAsync(request.ChargeIds, IsEligible, progress, ct))
            : Result<BulkChargePlan>.Failure(reason.Error!);
    }

    /// <summary>Confirms a plan. Once it starts saving it cannot be cancelled and reports it.</summary>
    public async Task<Result<BulkChargeResult>> ApplyAsync(
        WaiveChargesInBulkRequest request, BulkChargePlan plan, IProgress<OperationProgress>? progress, CancellationToken ct)
    {
        var reason = Charge.CleanReason(request.Reason);
        if (!reason.IsSuccess)
        {
            return Result<BulkChargeResult>.Failure(reason.Error!);
        }

        var now = clock.UtcNow;
        return await _engine.ApplyAsync(request.ChargeIds, plan, IsEligible, c => c.Waive(reason.Value, now), progress, ct);
    }
}
