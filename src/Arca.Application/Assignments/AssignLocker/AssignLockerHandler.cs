// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.SchoolYears;
using Arca.Domain.Common;
using Arca.Domain.Lockers;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;

namespace Arca.Application.Assignments.AssignLocker;

/// <summary>
/// The only way to assign a locker to a student (alumnes-i-assignacions, D7): from the student, from the locker and by
/// dragging all call this, so their validations and confirmations are the same. If another capability raises a warning,
/// nothing is assigned until the person confirms it; a blocker refuses the assignment with its reason.
/// </summary>
public sealed class AssignLockerHandler(AssignmentServices services, IUnitOfWork unit, IClock clock)
{
    public Task<Result<AssignLockerResult>> HandleAsync(AssignLockerRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var year = await ActiveYear.RequireAsync(services.Years, YearOperation.OpenAssignment, token);
            if (!year.IsSuccess)
            {
                return Result<AssignLockerResult>.Failure(year.Error!);
            }

            var student = await services.Students.GetAsync(request.StudentId, token);
            if (student is null)
            {
                return Result<AssignLockerResult>.Failure(StudentErrors.NotFound);
            }

            var locker = await services.Lockers.GetAsync(request.LockerId, token);
            if (locker is null)
            {
                return Result<AssignLockerResult>.Failure(LockerErrors.NotFound);
            }

            var now = clock.UtcNow;
            var ready = await services.Flow.PrepareAsync(student, locker, year.Value, ignoring: null, now, token);
            if (!ready.IsSuccess)
            {
                return Result<AssignLockerResult>.Failure(ready.Error!);
            }

            if (ready.Value!.Warnings.Count > 0 && !request.ConfirmWarnings)
            {
                return Result<AssignLockerResult>.Success(new AssignLockerResult(null, ready.Value.Warnings));
            }

            await services.Flow.CommitOpenAsync(ready.Value, request.Operation ?? OperationContext.None, now, token);
            var row = await services.Flow.RowAsync(ready.Value.Opened.Assignment, token);
            return Result<AssignLockerResult>.Success(new AssignLockerResult(row, []));
        }, ct);
}
