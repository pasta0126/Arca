// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.Json;
using Arca.Domain.Catalog;
using Arca.Domain.Common;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;

namespace Arca.Domain.Enrollments;

/// <summary>
/// The enrolment of a student in a school year, with a level and, optionally, a group (alumnes-i-assignacions). A student has
/// at most one per year, and the year guard decides whether it may be changed: only the active year accepts changes
/// (D12), so the enrolments of past years stay as they were. A change returns the event for the history of the student.
/// </summary>
public sealed class Enrollment
{
    static readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Rebuilds a stored enrolment. Used by persistence, which has already validated it.</summary>
    public Enrollment(Guid id, Guid studentId, Guid yearId, Guid levelId, Guid? groupId)
    {
        Id = id;
        StudentId = studentId;
        YearId = yearId;
        LevelId = levelId;
        GroupId = groupId;
    }

    public Guid Id { get; }

    public Guid StudentId { get; }

    public Guid YearId { get; }

    public Guid LevelId { get; private set; }

    public Guid? GroupId { get; private set; }

    /// <summary>Enrols an active student in a year, which must accept changes. The group, if any, must belong to the level.</summary>
    public static Result<EnrollmentCreated> Create(
        Guid id, Student student, AcademicYear? year, Level? level, Group? group, IEnumerable<Enrollment> existing, DateTimeOffset now)
    {
        var refused = YearGuard.Check(year, YearOperation.AddEnrollment);
        if (refused is not null)
        {
            return Result<EnrollmentCreated>.Failure(refused);
        }

        if (student.IsRetired)
        {
            return Result<EnrollmentCreated>.Failure(EnrollmentErrors.StudentRetired);
        }

        var placement = CheckPlacement(level, group);
        if (placement is not null)
        {
            return Result<EnrollmentCreated>.Failure(placement);
        }

        if (existing.Any(e => e.StudentId == student.Id && e.YearId == year!.Id))
        {
            return Result<EnrollmentCreated>.Failure(EnrollmentErrors.AlreadyEnrolled);
        }

        var enrollment = new Enrollment(id, student.Id, year!.Id, level!.Id, group?.Id);
        var enrolled = new HistoryEvent(
            student.Id, StudentEventTypes.Enrolled, now, null,
            JsonSerializer.Serialize(new { yearId = year.Id, levelId = level.Id, groupId = group?.Id }, _json));
        return Result<EnrollmentCreated>.Success(new EnrollmentCreated(enrollment, enrolled));
    }

    /// <summary>Changes the level or the group of the enrolment of the active year, and records the change in the history of the student.</summary>
    public Result<HistoryEvent> Change(AcademicYear? year, Level? level, Group? group, DateTimeOffset now)
    {
        var refused = YearGuard.Check(year, YearOperation.ChangeEnrollment);
        if (refused is not null)
        {
            return Result<HistoryEvent>.Failure(refused);
        }

        var placement = CheckPlacement(level, group);
        if (placement is not null)
        {
            return Result<HistoryEvent>.Failure(placement);
        }

        if (level!.Id == LevelId && group?.Id == GroupId)
        {
            return Result<HistoryEvent>.Failure(EnrollmentErrors.Unchanged);
        }

        var before = new { yearId = YearId, levelId = LevelId, groupId = GroupId };
        (LevelId, GroupId) = (level.Id, group?.Id);
        var after = new { yearId = YearId, levelId = LevelId, groupId = GroupId };
        return Result<HistoryEvent>.Success(new HistoryEvent(
            StudentId, StudentEventTypes.EnrollmentChanged, now, JsonSerializer.Serialize(before, _json), JsonSerializer.Serialize(after, _json)));
    }

    static Error? CheckPlacement(Level? level, Group? group)
    {
        if (level is null)
        {
            return EnrollmentErrors.LevelRequired;
        }

        return group is not null && group.LevelId != level.Id ? EnrollmentErrors.GroupNotInLevel : null;
    }
}
