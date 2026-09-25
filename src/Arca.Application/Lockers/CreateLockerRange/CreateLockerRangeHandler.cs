// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Application.Zones;
using Arca.Domain.Common;
using Arca.Domain.Lockers;
using Arca.Domain.Zones;

namespace Arca.Application.Lockers.CreateLockerRange;

/// <summary>
/// Creates a range of consecutive lockers in two phases (taquilles-i-zones, D6): <see cref="AnalyzeAsync"/> makes a
/// preview without saving anything, and <see cref="ApplyAsync"/> revalidates against the data as it is then and creates
/// them all in one transaction, or none. Both use the same validation, so the preview cannot disagree with the result.
/// </summary>
public sealed class CreateLockerRangeHandler(
    ILockerRepository lockers, IZoneRepository zones, ILockerEventRepository events, IUnitOfWork unit, IClock clock)
{
    public const int MaximumLockers = 1000;

    const int ProgressStep = 100;

    /// <summary>The preview. It can be cancelled: nothing has been saved.</summary>
    public async Task<Result<CreateLockerRangePlan>> AnalyzeAsync(
        CreateLockerRangeRequest request, IProgress<OperationProgress>? progress, CancellationToken ct) =>
        await PlanAsync(request, progress, ct);

    /// <summary>Confirms a plan. Once it starts saving it cannot be cancelled and reports it.</summary>
    public async Task<Result<CreateLockerRangeResult>> ApplyAsync(
        CreateLockerRangePlan plan, IProgress<OperationProgress>? progress, CancellationToken ct)
    {
        var analysis = await PlanAsync(plan.ToRequest(), progress, ct);
        if (!analysis.IsSuccess)
        {
            return Result<CreateLockerRangeResult>.Failure(analysis.Error!);
        }

        var fresh = analysis.Value!;

        // Only what the person saw is applied: if the data changed so the plan is no longer the one that was previewed
        // (a conflict appeared, or a conflict went away), nothing is created and the updated plan comes back to be
        // confirmed again. A plan that had conflicts is never applied by itself just because they cleared up.
        var changed = !fresh.ToCreate.SequenceEqual(plan.ToCreate) || !fresh.Conflicts.SequenceEqual(plan.Conflicts);
        if (fresh.HasConflicts || changed)
        {
            var notices = new List<Notice>();
            if (changed)
            {
                notices.Add(new Notice("Lockers.RangeChanged"));
            }

            if (fresh.HasConflicts)
            {
                notices.Add(new Notice("Lockers.RangeConflicts", [string.Join(", ", fresh.Conflicts)]));
            }

            return Result<CreateLockerRangeResult>.Success(new CreateLockerRangeResult(false, 0, fresh), [.. notices]);
        }

        progress?.Report(new OperationProgress(0, fresh.ToCreate.Count, CanCancel: false));
        return await unit.RunAsync(async token =>
        {
            var zone = await zones.GetAsync(fresh.ZoneId, token);
            var existing = (await lockers.ListAsync(includeRetired: false, token)).ToList();
            var now = clock.UtcNow;
            var done = 0;
            foreach (var number in fresh.ToCreate)
            {
                var created = Locker.Create(Guid.NewGuid(), number, zone!, null, existing, now);
                if (!created.IsSuccess)
                {
                    return Result<CreateLockerRangeResult>.Failure(created.Error!);
                }

                await lockers.AddAsync(created.Value!.Locker, token);
                await events.AddAsync(created.Value.Event, token);
                existing.Add(created.Value.Locker);
                done++;
                progress?.Report(new OperationProgress(done, fresh.ToCreate.Count, CanCancel: false));
            }

            return Result<CreateLockerRangeResult>.Success(new CreateLockerRangeResult(true, done, fresh));
        }, CancellationToken.None);
    }

    /// <summary>The single validation of a range, used by the preview and by the confirmation.</summary>
    async Task<Result<CreateLockerRangePlan>> PlanAsync(
        CreateLockerRangeRequest request, IProgress<OperationProgress>? progress, CancellationToken ct)
    {
        var first = LockerNumber.Validate(request.First);
        var last = LockerNumber.Validate(request.Last);
        if (!first.IsSuccess || !last.IsSuccess)
        {
            return Result<CreateLockerRangePlan>.Failure((first.Error ?? last.Error)!);
        }

        if (request.First > request.Last)
        {
            return Result<CreateLockerRangePlan>.Failure(LockerErrors.RangeInvalid);
        }

        var count = request.Last - request.First + 1;
        if (count > MaximumLockers)
        {
            return Result<CreateLockerRangePlan>.Failure(LockerErrors.RangeTooLarge(MaximumLockers));
        }

        var zone = await zones.GetAsync(request.ZoneId, ct);
        if (zone is null)
        {
            return Result<CreateLockerRangePlan>.Failure(ZoneErrors.NotFound);
        }

        if (!zone.IsActive)
        {
            return Result<CreateLockerRangePlan>.Failure(LockerErrors.ZoneUnavailable);
        }

        var taken = (await lockers.ListAsync(includeRetired: false, ct)).Select(l => l.Number).ToHashSet();
        var conflicts = new List<int>();
        var free = new List<int>(count);
        for (var number = request.First; number <= request.Last; number++)
        {
            ct.ThrowIfCancellationRequested();
            (taken.Contains(number) ? conflicts : free).Add(number);
            if ((number - request.First + 1) % ProgressStep == 0)
            {
                progress?.Report(new OperationProgress(number - request.First + 1, count));
            }
        }

        progress?.Report(new OperationProgress(count, count));
        var plan = new CreateLockerRangePlan(request.First, request.Last, zone.Id, zone.Name, conflicts.Count == 0 ? free : [], conflicts);
        return Result<CreateLockerRangePlan>.Success(plan);
    }
}
