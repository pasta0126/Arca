// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Catalog;
using Arca.Application.Enrollments;
using Arca.Application.Lockers;
using Arca.Application.SchoolYears;
using Arca.Application.Students;
using Arca.Application.Zones;
using Arca.Domain.Assignments;
using Arca.Domain.Common;
using Arca.Domain.Lockers;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;

namespace Arca.Application.Assignments;

/// <summary>
/// The steps every way of opening and closing an assignment shares (alumnes-i-assignacions, D7): the single validation,
/// the guards of other capabilities, saving with the two history events, consuming a reservation and calling the hooks
/// inside the same transaction. Assigning, changing a locker and reassigning after a breakdown are all composed from
/// these, so the validations and confirmations cannot diverge. It must run inside the unit of work.
/// </summary>
internal sealed class AssignmentFlow(
    IAssignmentRepository assignments, IStudentRepository students, ILockerRepository lockers, IZoneRepository zones,
    IEnrollmentRepository enrollments, IAcademicYearRepository years, IStudentEventRepository studentEvents,
    ILockerEventRepository lockerEvents, IEnumerable<IAssignmentGuard> guards, IEnumerable<IAssignmentOpenedHandler> opened,
    IEnumerable<IAssignmentClosedHandler> closed)
{
    /// <summary>An assignment that has passed the validation and the guards and is ready to be saved.</summary>
    internal sealed record Ready(AssignmentOpened Opened, Locker Locker, IReadOnlyList<Notice> Warnings);

    /// <summary>
    /// Validates opening an assignment and asks the guards, saving nothing. A blocker is an error; warnings come back with
    /// the plan so the caller can require confirmation.
    /// </summary>
    /// <param name="ignoring">A current assignment that is about to be closed and must not count, as when changing locker.</param>
    public async Task<Result<Ready>> PrepareAsync(
        Student student, Locker locker, AcademicYear? year, Assignment? ignoring, DateTimeOffset now, CancellationToken ct)
    {
        var enrollment = year is null ? null : await enrollments.GetAsync(student.Id, year.Id, ct);
        var current = (await assignments.ListCurrentAsync(ct)).Where(a => a.Id != ignoring?.Id).ToList();
        var open = Assignment.Open(Guid.NewGuid(), student, year, enrollment, locker, current, now);
        if (!open.IsSuccess)
        {
            return Result<Ready>.Failure(open.Error!);
        }

        var findings = new List<AssignmentFinding>();
        foreach (var guard in guards)
        {
            findings.AddRange(await guard.CheckAsync(new ProposedAssignment(student, locker, year!), ct));
        }

        var blocker = findings.FirstOrDefault(f => f.Kind == AssignmentFindingKind.Blocker);
        if (blocker is not null)
        {
            return Result<Ready>.Failure(new Error(blocker.Code, Args: blocker.Args ?? []));
        }

        IReadOnlyList<Notice> warnings = [.. findings.Select(f => new Notice(f.Code, f.Args ?? []))];
        return Result<Ready>.Success(new Ready(open.Value!, locker, warnings));
    }

    /// <summary>Saves a prepared assignment, consumes the reservation it used and calls the hooks.</summary>
    public async Task CommitOpenAsync(Ready ready, OperationContext operation, DateTimeOffset now, CancellationToken ct)
    {
        var (assignment, studentEvent, lockerEvent, consumes) = ready.Opened;
        await assignments.AddAsync(assignment, ct);
        if (consumes)
        {
            var consumed = ready.Locker.ConsumeReservation(now);
            await lockers.UpdateAsync(ready.Locker, ct);
            await lockerEvents.AddAsync(consumed.Value!, ct);
        }

        await studentEvents.AddAsync(studentEvent, ct);
        await lockerEvents.AddAsync(lockerEvent, ct);
        foreach (var hook in opened)
        {
            await hook.HandleAsync(new AssignmentHookContext(assignment, operation), ct);
        }
    }

    /// <summary>Closes an assignment, saves it with the two events and calls the hooks.</summary>
    public async Task<Result<bool>> CloseAsync(
        Assignment assignment, AssignmentCloseReason reason, OperationContext operation, DateTimeOffset now, CancellationToken ct)
    {
        var year = await years.GetAsync(assignment.YearId, ct);
        var done = assignment.Close(year, reason, operation.Reason, now);
        if (!done.IsSuccess)
        {
            return Result<bool>.Failure(done.Error!);
        }

        await assignments.UpdateAsync(assignment, ct);
        await studentEvents.AddAsync(done.Value!.StudentEvent, ct);
        await lockerEvents.AddAsync(done.Value.LockerEvent, ct);
        foreach (var hook in closed)
        {
            await hook.HandleAsync(new AssignmentHookContext(assignment, operation), ct);
        }

        return Result<bool>.Success(true);
    }

    /// <summary>The row of an assignment for results and histories.</summary>
    public async Task<AssignmentRow> RowAsync(Assignment assignment, CancellationToken ct)
    {
        var student = await students.GetAsync(assignment.StudentId, ct);
        var locker = await lockers.GetAsync(assignment.LockerId, ct);
        var zone = locker is null ? null : await zones.GetAsync(locker.ZoneId, ct);
        var year = await years.GetAsync(assignment.YearId, ct);
        return new AssignmentRow(
            assignment.Id, assignment.StudentId, student is null ? "?" : student.FirstName + " " + student.LastName, assignment.LockerId,
            locker?.Number ?? 0, zone?.Name ?? string.Empty, year?.Name ?? string.Empty, assignment.StartedAtUtc, assignment.EndedAtUtc,
            assignment.CloseReason, assignment.CloseNote);
    }
}
