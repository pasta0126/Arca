// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Domain.Charges;
using Arca.Domain.Common;

namespace Arca.Application.Charges;

/// <summary>
/// The two phases shared by the bulk operations on charges (pagaments, D6). <see cref="AnalyzeAsync"/> previews without
/// saving and can be cancelled; <see cref="ApplyAsync"/> revalidates, and applies every change in one transaction or none.
/// Only what the person saw is applied: if the data changed so the plan is no longer the previewed one, nothing changes
/// and the updated plan comes back to be confirmed again.
/// </summary>
internal sealed class BulkChargeEngine(IChargeRepository charges, IChargeEventRepository events, IUnitOfWork unit)
{
    const int ProgressStep = 10;

    public async Task<BulkChargePlan> AnalyzeAsync(
        IReadOnlyList<Guid> selected, Func<Charge, bool> isEligible, IProgress<OperationProgress>? progress, CancellationToken ct)
    {
        var distinct = selected.Distinct().ToList();
        var eligible = new List<Guid>(distinct.Count);
        var ineligible = new List<Guid>();
        var total = 0m;
        for (var i = 0; i < distinct.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var charge = await charges.GetAsync(distinct[i], ct);
            if (charge is not null && isEligible(charge))
            {
                eligible.Add(charge.Id);
                total += charge.Amount.Amount;
            }
            else
            {
                ineligible.Add(distinct[i]);
            }

            if ((i + 1) % ProgressStep == 0)
            {
                progress?.Report(new OperationProgress(i + 1, distinct.Count));
            }
        }

        progress?.Report(new OperationProgress(distinct.Count, distinct.Count));
        return new BulkChargePlan(ineligible.Count == 0 ? eligible : [], ineligible, total);
    }

    public async Task<Result<BulkChargeResult>> ApplyAsync(
        IReadOnlyList<Guid> selected, BulkChargePlan previewed, Func<Charge, bool> isEligible,
        Func<Charge, Result<HistoryEvent>> change, IProgress<OperationProgress>? progress, CancellationToken ct)
    {
        var fresh = await AnalyzeAsync(selected, isEligible, progress, ct);
        if (fresh.HasIneligible || !fresh.Eligible.SequenceEqual(previewed.Eligible) || previewed.HasIneligible)
        {
            var notices = new List<Notice>();
            if (fresh.HasIneligible)
            {
                notices.Add(new Notice("Charges.BulkIneligible", [fresh.Ineligible.Count]));
            }
            else
            {
                notices.Add(new Notice("Charges.BulkChanged"));
            }

            return Result<BulkChargeResult>.Success(new BulkChargeResult(false, 0, 0m, fresh), [.. notices]);
        }

        progress?.Report(new OperationProgress(0, fresh.Eligible.Count, CanCancel: false));
        return await unit.RunAsync(async token =>
        {
            var done = 0;
            foreach (var id in fresh.Eligible)
            {
                var charge = (await charges.GetAsync(id, token))!;
                var changed = change(charge);
                if (!changed.IsSuccess)
                {
                    return Result<BulkChargeResult>.Failure(changed.Error!);
                }

                await charges.UpdateAsync(charge, token);
                await events.AddAsync(changed.Value!, token);
                done++;
                progress?.Report(new OperationProgress(done, fresh.Eligible.Count, CanCancel: false));
            }

            return Result<BulkChargeResult>.Success(new BulkChargeResult(true, done, fresh.TotalAmount, fresh));
        }, CancellationToken.None);
    }
}
