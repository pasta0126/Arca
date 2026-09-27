// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Catalog;
using Arca.Application.Common;
using Arca.Application.Enrollments;
using Arca.Application.SchoolYears;
using Arca.Domain.Common;
using Arca.Domain.Assignments;
using Arca.Domain.Students;

namespace Arca.Application.Students.RetireStudent;

/// <summary>
/// Retires a student with a reason, keeping the record and the history. In the same transaction it frees their locker (the
/// assignment is closed and kept), removes a locker reserved in their name, and calls the lifecycle hooks of other capabilities.
/// </summary>
public sealed class RetireStudentHandler(
    IStudentRepository students, IEnrollmentRepository enrollments, ICatalogRepository catalog, IAcademicYearRepository years,
    IStudentEventRepository events, IStudentLockers lockers, AssignmentServices assignments,
    IEnumerable<IStudentLifecycleHandler> lifecycle, IUnitOfWork unit, IClock clock)
{
    public Task<Result<StudentDetail>> HandleAsync(RetireStudentRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var student = await students.GetAsync(request.StudentId, token);
            if (student is null)
            {
                return Result<StudentDetail>.Failure(StudentErrors.NotFound);
            }

            var retired = student.Retire(request.Reason, clock.UtcNow);
            if (!retired.IsSuccess)
            {
                return Result<StudentDetail>.Failure(retired.Error!);
            }

            await students.UpdateAsync(student, token);
            await events.AddAsync(retired.Value!, token);

            var now = clock.UtcNow;
            var operation = new OperationContext(request.Reason);
            if (await assignments.Assignments.GetCurrentOfStudentAsync(student.Id, token) is { } current)
            {
                var closed = await assignments.Flow.CloseAsync(current, AssignmentCloseReason.StudentRetired, operation, now, token);
                if (!closed.IsSuccess)
                {
                    return Result<StudentDetail>.Failure(closed.Error!);
                }
            }

            foreach (var reserved in (await assignments.Lockers.ListAsync(includeRetired: false, token)).Where(l => l.ReservedForStudentId == student.Id))
            {
                var removed = reserved.RemoveReservation(now);
                await assignments.Lockers.UpdateAsync(reserved, token);
                await assignments.LockerEvents.AddAsync(removed.Value!, token);
                await events.AddAsync(new HistoryEvent(student.Id, StudentEventTypes.LockerReservationRemoved, now, null, null), token);
            }

            foreach (var hook in lifecycle)
            {
                await hook.OnRetiredAsync(student, operation, token);
            }

            return Result<StudentDetail>.Success(await new StudentViews(years, enrollments, catalog, lockers).DetailAsync(student, null, token));
        }, ct);
}
