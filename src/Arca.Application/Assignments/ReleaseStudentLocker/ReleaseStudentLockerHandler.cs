// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Domain.Assignments;
using Arca.Domain.Common;
using Arca.Domain.Students;

namespace Arca.Application.Assignments.ReleaseStudentLocker;

/// <summary>
/// Releases the locker of a student, with an optional reason: the assignment is closed with its date and reason and kept in
/// the history. The locker is left free, or stays broken if it was out of service, because its status is derived.
/// </summary>
public sealed class ReleaseStudentLockerHandler(AssignmentServices services, IUnitOfWork unit, IClock clock)
{
    public Task<Result<AssignmentRow>> HandleAsync(ReleaseStudentLockerRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            if (await services.Students.GetAsync(request.StudentId, token) is null)
            {
                return Result<AssignmentRow>.Failure(StudentErrors.NotFound);
            }

            var current = await services.Assignments.GetCurrentOfStudentAsync(request.StudentId, token);
            if (current is null)
            {
                return Result<AssignmentRow>.Failure(AssignmentErrors.NoAssignment);
            }

            var closed = await services.Flow.CloseAsync(
                current, AssignmentCloseReason.Released, request.Operation ?? OperationContext.None, clock.UtcNow, token);
            return closed.IsSuccess
                ? Result<AssignmentRow>.Success(await services.Flow.RowAsync(current, token))
                : Result<AssignmentRow>.Failure(closed.Error!);
        }, ct);
}
