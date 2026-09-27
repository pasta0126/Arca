// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Lockers;
using Arca.Domain.Assignments;
using Arca.Domain.Common;
using Arca.Domain.Lockers;
using Arca.Domain.Students;

namespace Arca.Application.Assignments.ReserveLockerForStudent;

/// <summary>
/// Reserves a free locker for a given student (alumnes-i-assignacions): the student must be active, must not hold a locker or
/// have another reservation, and the locker must be free. The reservation becomes an assignment when it is formalised, and
/// is removed if the student is retired. The event goes into the histories of the locker and of the student.
/// </summary>
public sealed class ReserveLockerForStudentHandler(AssignmentServices assignments, IUnitOfWork unit, IClock clock)
{
    public Task<Result<LockerRow>> HandleAsync(ReserveLockerForStudentRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var student = await assignments.Students.GetAsync(request.StudentId, token);
            if (student is null)
            {
                return Result<LockerRow>.Failure(StudentErrors.NotFound);
            }

            var locker = await assignments.Lockers.GetAsync(request.LockerId, token);
            if (locker is null)
            {
                return Result<LockerRow>.Failure(LockerErrors.NotFound);
            }

            if (student.IsRetired)
            {
                return Result<LockerRow>.Failure(AssignmentErrors.StudentRetired);
            }

            if (await assignments.Assignments.GetCurrentOfStudentAsync(student.Id, token) is not null)
            {
                return Result<LockerRow>.Failure(AssignmentErrors.StudentHasLocker);
            }

            var others = await assignments.Lockers.ListAsync(includeRetired: false, token);
            if (others.Any(l => l.ReservedForStudentId == student.Id))
            {
                return Result<LockerRow>.Failure(AssignmentErrors.StudentHasReservation);
            }

            var occupied = await assignments.Assignments.GetCurrentOfLockerAsync(locker.Id, token) is not null;
            var now = clock.UtcNow;
            var reserved = locker.ReserveForStudent(student.Id, request.Note, occupied, now);
            if (!reserved.IsSuccess)
            {
                return Result<LockerRow>.Failure(reserved.Error!);
            }

            await assignments.Lockers.UpdateAsync(locker, token);
            await assignments.LockerEvents.AddAsync(reserved.Value!, token);
            await assignments.StudentEvents.AddAsync(
                new HistoryEvent(student.Id, StudentEventTypes.LockerReserved, now, null, System.Text.Json.JsonSerializer.Serialize(new { lockerId = locker.Id })), token);
            return Result<LockerRow>.Success(await assignments.Flow.LockerRowAsync(locker, occupied, token));
        }, ct);
}
