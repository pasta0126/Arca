// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Domain.Charges;
using Arca.Domain.Common;

namespace Arca.Application.Charges.ReturnDepositsInBulk;

/// <summary>
/// Marks several deposits due back as given back with a common date and note, in two phases (pagaments, D6): the analysis
/// shows how many and how much, and the confirmation revalidates and marks them all in one transaction, or none.
/// </summary>
public sealed class ReturnDepositsInBulkHandler(IChargeRepository charges, IChargeEventRepository events, IUnitOfWork unit, IClock clock)
{
    readonly BulkChargeEngine _engine = new(charges, events, unit);

    static bool IsEligible(Charge charge) => charge.IsDueBack;

    /// <summary>The preview. It can be cancelled: nothing has been saved.</summary>
    public async Task<Result<BulkChargePlan>> AnalyzeAsync(ReturnDepositsInBulkRequest request, IProgress<OperationProgress>? progress, CancellationToken ct)
    {
        var check = Charge.CheckReturn(request.ReturnedOn, request.Note, clock.Today);
        return check.IsSuccess
            ? Result<BulkChargePlan>.Success(await _engine.AnalyzeAsync(request.ChargeIds, IsEligible, progress, ct))
            : Result<BulkChargePlan>.Failure(check.Error!);
    }

    /// <summary>Confirms a plan. Once it starts saving it cannot be cancelled and reports it.</summary>
    public async Task<Result<BulkChargeResult>> ApplyAsync(
        ReturnDepositsInBulkRequest request, BulkChargePlan plan, IProgress<OperationProgress>? progress, CancellationToken ct)
    {
        var check = Charge.CheckReturn(request.ReturnedOn, request.Note, clock.Today);
        if (!check.IsSuccess)
        {
            return Result<BulkChargeResult>.Failure(check.Error!);
        }

        var (today, now) = (clock.Today, clock.UtcNow);
        return await _engine.ApplyAsync(request.ChargeIds, plan, IsEligible, c => c.MarkReturned(request.ReturnedOn, request.Note, today, now), progress, ct);
    }
}
