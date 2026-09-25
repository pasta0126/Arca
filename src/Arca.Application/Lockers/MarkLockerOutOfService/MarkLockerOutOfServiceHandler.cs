// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Common;
using Arca.Application.SchoolYears;
using Arca.Application.Zones;
using Arca.Domain.Assignments;
using Arca.Domain.Common;
using Arca.Domain.Lockers;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;

namespace Arca.Application.Lockers.MarkLockerOutOfService;

/// <summary>
/// Puts a locker out of service as broken or in maintenance (taquilles-i-zones, D3, completed by alumnes-i-assignacions). An
/// occupied locker asks for a decision first and is not changed without one: keep the student, reassign them to another
/// locker (validated like any assignment, all in one operation) or free them. If the destination of a reassignment cannot
/// be assigned nothing changes and the locker does not become out of service.
/// </summary>
public sealed class MarkLockerOutOfServiceHandler(
    ILockerRepository lockers, IZoneRepository zones, ILockerEventRepository events, ILockerOccupancy occupancy,
    AssignmentServices assignments, IUnitOfWork unit, IClock clock)
{
    public Task<Result<MarkLockerOutOfServiceResult>> HandleAsync(MarkLockerOutOfServiceRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var changes = new LockerChanges(lockers, zones, events, occupancy);
            var locker = await lockers.GetAsync(request.LockerId, token);
            var current = locker is null ? null : await assignments.Assignments.GetCurrentOfLockerAsync(locker.Id, token);
            var carriesOutDecision = locker is { IsRetired: false, OutOfService: null } && current is not null
                && request.Decision is OutOfServiceDecision.Reassign or OutOfServiceDecision.Release;

            AssignmentFlow.Ready? destination = null;
            if (carriesOutDecision && request.Decision == OutOfServiceDecision.Reassign)
            {
                var prepared = await PrepareReassignmentAsync(request, locker!, current!, token);
                if (!prepared.IsSuccess)
                {
                    return Result<MarkLockerOutOfServiceResult>.Failure(prepared.Error!);
                }

                destination = prepared.Value!;
                if (destination.Warnings.Count > 0 && !request.ConfirmWarnings)
                {
                    var unchanged = await changes.RowAsync(locker!, occupied: true, token);
                    return Result<MarkLockerOutOfServiceResult>.Success(new MarkLockerOutOfServiceResult(unchanged, false, [], destination.Warnings));
                }
            }

            var now = clock.UtcNow;
            OutOfServiceOutcome? outcome = null;
            var applied = await changes.ApplyAsync(
                request.LockerId,
                (target, occupied) =>
                {
                    var marked = target.MarkOutOfService(request.Kind, request.Decision, occupied, now);
                    outcome = marked.Value;
                    return marked.IsSuccess ? Result<HistoryEvent?>.Success(marked.Value!.Event) : Result<HistoryEvent?>.Failure(marked.Error!);
                },
                token,
                async _ => carriesOutDecision ? await CarryOutDecisionAsync(request, current!, destination, now, token) : null);
            return applied.IsSuccess
                ? Result<MarkLockerOutOfServiceResult>.Success(new MarkLockerOutOfServiceResult(applied.Value!, outcome!.NeedsDecision, outcome.DecisionsOffered))
                : Result<MarkLockerOutOfServiceResult>.Failure(applied.Error!);
        }, ct);

    async Task<Result<AssignmentFlow.Ready>> PrepareReassignmentAsync(
        MarkLockerOutOfServiceRequest request, Locker locker, Assignment current, CancellationToken ct)
    {
        if (request.ReassignToLockerId is not { } targetId)
        {
            return Result<AssignmentFlow.Ready>.Failure(AssignmentErrors.TargetRequired);
        }

        if (targetId == locker.Id)
        {
            return Result<AssignmentFlow.Ready>.Failure(AssignmentErrors.SameLocker);
        }

        var target = await lockers.GetAsync(targetId, ct);
        var student = await assignments.Students.GetAsync(current.StudentId, ct);
        if (target is null)
        {
            return Result<AssignmentFlow.Ready>.Failure(LockerErrors.NotFound);
        }

        if (student is null)
        {
            return Result<AssignmentFlow.Ready>.Failure(StudentErrors.NotFound);
        }

        var year = await ActiveYear.RequireAsync(assignments.Years, YearOperation.OpenAssignment, ct);
        return year.IsSuccess
            ? await assignments.Flow.PrepareAsync(student, target, year.Value, ignoring: current, clock.UtcNow, ct)
            : Result<AssignmentFlow.Ready>.Failure(year.Error!);
    }

    async Task<Error?> CarryOutDecisionAsync(
        MarkLockerOutOfServiceRequest request, Assignment current, AssignmentFlow.Ready? destination, DateTimeOffset now, CancellationToken ct)
    {
        var operation = new OperationContext(Data: new Dictionary<string, object?> { ["outOfService"] = request.Kind.ToString() });
        var reassigning = request.Decision == OutOfServiceDecision.Reassign;
        var closed = await assignments.Flow.CloseAsync(
            current, reassigning ? AssignmentCloseReason.OutOfServiceReassigned : AssignmentCloseReason.OutOfServiceReleased, operation, now, ct);
        if (!closed.IsSuccess)
        {
            return closed.Error;
        }

        if (reassigning)
        {
            await assignments.Flow.CommitOpenAsync(destination!, operation, now, ct);
        }

        return null;
    }
}
