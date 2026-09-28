// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.SchoolYears;
using Arca.Domain.Common;
using Arca.Domain.Lockers;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;

namespace Arca.Application.Assignments.CheckAssignmentTarget;

/// <param name="StudentId">The student that would get the locker.</param>
/// <param name="LockerId">The locker that would be assigned.</param>
public sealed record CheckAssignmentTargetRequest(Guid StudentId, Guid LockerId);

/// <summary>What assigning a locker to a student would do, found out without doing it.</summary>
/// <param name="Blocker">Why it cannot be done (the locker is taken, broken, the student already has one…), or null if it can.</param>
/// <param name="Warnings">What the person would have to confirm first, such as debt of earlier years.</param>
public sealed record AssignmentTargetCheck(Error? Blocker, IReadOnlyList<Notice> Warnings)
{
    public bool IsValid => Blocker is null;
}

/// <summary>
/// Asks whether a locker could be assigned to a student, with exactly the validations and guards of the assignment itself
/// and saving nothing (ux-fonaments, D12): it is how dragging a student over a locker shows, before dropping, whether the
/// drop would work and why not. A locker that cannot be assigned is an answer, not a failure.
/// </summary>
public sealed class CheckAssignmentTargetHandler(AssignmentServices services, IClock clock)
{
    public async Task<Result<AssignmentTargetCheck>> HandleAsync(CheckAssignmentTargetRequest request, CancellationToken ct)
    {
        var year = await ActiveYear.RequireAsync(services.Years, YearOperation.OpenAssignment, ct);
        if (!year.IsSuccess)
        {
            return Refused(year.Error!);
        }

        var student = await services.Students.GetAsync(request.StudentId, ct);
        if (student is null)
        {
            return Refused(StudentErrors.NotFound);
        }

        var locker = await services.Lockers.GetAsync(request.LockerId, ct);
        if (locker is null)
        {
            return Refused(LockerErrors.NotFound);
        }

        var ready = await services.Flow.PrepareAsync(student, locker, year.Value, ignoring: null, clock.UtcNow, ct);
        return ready.IsSuccess
            ? Result<AssignmentTargetCheck>.Success(new AssignmentTargetCheck(null, ready.Value!.Warnings))
            : Refused(ready.Error!);
    }

    static Result<AssignmentTargetCheck> Refused(Error reason) => Result<AssignmentTargetCheck>.Success(new AssignmentTargetCheck(reason, []));
}
