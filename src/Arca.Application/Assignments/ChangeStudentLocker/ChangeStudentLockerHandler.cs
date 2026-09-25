// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Common;
using Arca.Application.SchoolYears;
using Arca.Domain.Assignments;
using Arca.Domain.Common;
using Arca.Domain.Lockers;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;

namespace Arca.Application.Assignments.ChangeStudentLocker;

/// <summary>
/// Moves a student to another locker in one indivisible operation: it validates the new locker as any assignment is
/// validated, and only then closes the current assignment and opens the new one, with the events in the histories of the
/// student and of both lockers. If the destination cannot be assigned, nothing changes and the original assignment stays.
/// </summary>
public sealed class ChangeStudentLockerHandler(AssignmentServices services, IUnitOfWork unit, IClock clock)
{
    public Task<Result<AssignLockerResult>> HandleAsync(ChangeStudentLockerRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var student = await services.Students.GetAsync(request.StudentId, token);
            if (student is null)
            {
                return Result<AssignLockerResult>.Failure(StudentErrors.NotFound);
            }

            var current = await services.Assignments.GetCurrentOfStudentAsync(student.Id, token);
            if (current is null)
            {
                return Result<AssignLockerResult>.Failure(AssignmentErrors.NoAssignment);
            }

            var currentYear = await services.Years.GetAsync(current.YearId, token);
            var refused = YearGuard.Check(currentYear, YearOperation.ChangeAssignment);
            if (refused is not null)
            {
                return Result<AssignLockerResult>.Failure(refused);
            }

            var locker = await services.Lockers.GetAsync(request.NewLockerId, token);
            if (locker is null)
            {
                return Result<AssignLockerResult>.Failure(LockerErrors.NotFound);
            }

            if (locker.Id == current.LockerId)
            {
                return Result<AssignLockerResult>.Failure(AssignmentErrors.SameLocker);
            }

            var now = clock.UtcNow;
            var ready = await services.Flow.PrepareAsync(student, locker, currentYear, ignoring: current, now, token);
            if (!ready.IsSuccess)
            {
                return Result<AssignLockerResult>.Failure(ready.Error!);
            }

            if (ready.Value!.Warnings.Count > 0 && !request.ConfirmWarnings)
            {
                return Result<AssignLockerResult>.Success(new AssignLockerResult(null, ready.Value.Warnings));
            }

            var operation = request.Operation ?? OperationContext.None;
            var closed = await services.Flow.CloseAsync(current, AssignmentCloseReason.Changed, operation, now, token);
            if (!closed.IsSuccess)
            {
                return Result<AssignLockerResult>.Failure(closed.Error!);
            }

            await services.Flow.CommitOpenAsync(ready.Value, operation, now, token);
            var row = await services.Flow.RowAsync(ready.Value.Opened.Assignment, token);
            return Result<AssignLockerResult>.Success(new AssignLockerResult(row, []));
        }, ct);
}
