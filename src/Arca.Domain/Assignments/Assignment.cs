// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.Encodings.Web;
using System.Text.Json;
using Arca.Domain.Common;
using Arca.Domain.Enrollments;
using Arca.Domain.Lockers;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;

namespace Arca.Domain.Assignments;

/// <summary>
/// A locker given to a student in a school year (alumnes-i-assignacions). It is current until it has an end, and then it stays
/// in the history, closed, with its date and reason. There is at most one current assignment per student and one per locker.
/// The occupancy of a locker is derived from its current assignment. The rules are given what they need, so they are tested
/// without a database, and they return the history events of the student and of the locker that the change originates.
/// </summary>
public sealed class Assignment
{
    public const int MaximumNoteLength = 500;

    static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Rebuilds a stored assignment. Used by persistence, which has already validated it.</summary>
    public Assignment(
        Guid id, Guid studentId, Guid lockerId, Guid yearId, DateTimeOffset startedAtUtc, DateTimeOffset? endedAtUtc,
        AssignmentCloseReason? closeReason, string? closeNote)
    {
        Id = id;
        StudentId = studentId;
        LockerId = lockerId;
        YearId = yearId;
        StartedAtUtc = startedAtUtc;
        EndedAtUtc = endedAtUtc;
        CloseReason = closeReason;
        CloseNote = closeNote;
    }

    public Guid Id { get; }

    public Guid StudentId { get; }

    public Guid LockerId { get; }

    public Guid YearId { get; }

    public DateTimeOffset StartedAtUtc { get; }

    public DateTimeOffset? EndedAtUtc { get; private set; }

    public AssignmentCloseReason? CloseReason { get; private set; }

    public string? CloseNote { get; private set; }

    public bool IsCurrent => EndedAtUtc is null;

    /// <summary>
    /// Opens the assignment of a locker to a student in the active year, or says why not. The student must be active and
    /// enrolled in the year, must not hold a locker, and the locker must be free or reserved for that same student.
    /// </summary>
    /// <param name="enrollment">The enrolment of the student in the active year, if any.</param>
    /// <param name="current">Every current assignment: it decides who holds what.</param>
    public static Result<AssignmentOpened> Open(
        Guid id, Student student, AcademicYear? year, Enrollment? enrollment, Locker locker, IEnumerable<Assignment> current, DateTimeOffset now)
    {
        var refused = YearGuard.Check(year, YearOperation.OpenAssignment);
        if (refused is not null)
        {
            return Result<AssignmentOpened>.Failure(refused);
        }

        if (student.IsRetired)
        {
            return Result<AssignmentOpened>.Failure(AssignmentErrors.StudentRetired);
        }

        if (enrollment is null || enrollment.StudentId != student.Id || enrollment.YearId != year!.Id)
        {
            return Result<AssignmentOpened>.Failure(AssignmentErrors.StudentNotEnrolled);
        }

        var held = current.Where(a => a.IsCurrent).ToList();
        if (held.Any(a => a.StudentId == student.Id))
        {
            return Result<AssignmentOpened>.Failure(AssignmentErrors.StudentHasLocker);
        }

        if (locker.IsRetired || locker.OutOfService is not null)
        {
            return Result<AssignmentOpened>.Failure(AssignmentErrors.LockerUnavailable);
        }

        if (held.Any(a => a.LockerId == locker.Id))
        {
            return Result<AssignmentOpened>.Failure(AssignmentErrors.LockerOccupied);
        }

        if (locker.IsReserved && locker.ReservedForStudentId is null)
        {
            return Result<AssignmentOpened>.Failure(AssignmentErrors.LockerReserved);
        }

        if (locker.IsReserved && locker.ReservedForStudentId != student.Id)
        {
            return Result<AssignmentOpened>.Failure(AssignmentErrors.LockerReservedForOther);
        }

        var assignment = new Assignment(id, student.Id, locker.Id, year.Id, now, null, null, null);
        var studentEvent = new HistoryEvent(
            student.Id, StudentEventTypes.AssignmentOpened, now, null, Json(new { lockerId = locker.Id, yearId = year.Id }));
        var lockerEvent = new HistoryEvent(
            locker.Id, LockerEventTypes.Assigned, now, null, Json(new { studentId = student.Id, yearId = year.Id }));
        return Result<AssignmentOpened>.Success(new AssignmentOpened(assignment, studentEvent, lockerEvent, ConsumesReservation: locker.IsReserved));
    }

    /// <summary>
    /// Closes the assignment with a reason and an optional note, keeping it in the history. Only an assignment of the active
    /// year can be closed this way (D12), and one that is already closed cannot be closed again.
    /// </summary>
    /// <param name="year">The year the assignment belongs to.</param>
    public Result<AssignmentClosed> Close(AcademicYear? year, AssignmentCloseReason reason, string? note, DateTimeOffset now)
    {
        if (!IsCurrent)
        {
            return Result<AssignmentClosed>.Failure(AssignmentErrors.AlreadyClosed);
        }

        var refused = YearGuard.Check(year, YearOperation.CloseAssignment);
        if (refused is not null)
        {
            return Result<AssignmentClosed>.Failure(refused);
        }

        var clean = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (clean?.Length > MaximumNoteLength)
        {
            return Result<AssignmentClosed>.Failure(AssignmentErrors.NoteTooLong(MaximumNoteLength));
        }

        (EndedAtUtc, CloseReason, CloseNote) = (now, reason, clean);
        var payload = new { reason = reason.ToString(), note = clean };
        var studentEvent = new HistoryEvent(
            StudentId, StudentEventTypes.AssignmentClosed, now, Json(new { lockerId = LockerId, yearId = YearId }), Json(payload), clean);
        var lockerEvent = new HistoryEvent(
            LockerId, LockerEventTypes.Released, now, Json(new { studentId = StudentId, yearId = YearId }), Json(payload), clean);
        return Result<AssignmentClosed>.Success(new AssignmentClosed(studentEvent, lockerEvent));
    }

    static string Json(object value) => JsonSerializer.Serialize(value, _json);
}
