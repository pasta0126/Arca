// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Assignments;
using Arca.Domain.Catalog;
using Arca.Domain.Enrollments;
using Arca.Domain.Lockers;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;
using Arca.Domain.Zones;
using Xunit;

namespace Arca.Domain.Tests.Assignments;

public sealed class AssignmentTests
{
    const string Spec = "alumnes-i-assignacions/assignacions";

    static readonly DateTimeOffset _now = new(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);
    static readonly AcademicYear _active = AcademicYear.Restore(Guid.NewGuid(), new DateOnly(2026, 9, 1), new DateOnly(2027, 6, 30), true);
    static readonly AcademicYear _past = AcademicYear.Restore(Guid.NewGuid(), new DateOnly(2025, 9, 1), new DateOnly(2026, 6, 30), false);
    static readonly Level _level = Level.Create(Guid.NewGuid(), "1r ESO", []).Value!;
    static readonly Zone _zone = Zone.Create(Guid.NewGuid(), "Planta 1", []).Value!;

    static Student Student(string email = "nuria@test.cat") =>
        Arca.Domain.Students.Student.Create(Guid.NewGuid(), "Núria", "García", email, [], _now).Value!.Student;

    static Locker Locker(int number = 15) => Arca.Domain.Lockers.Locker.Create(Guid.NewGuid(), number, _zone, null, [], _now).Value!.Locker;

    static Enrollment Enrol(Student student, AcademicYear year) =>
        Enrollment.Create(Guid.NewGuid(), student, year, _level, null, [], _now).Value!.Enrollment;

    /// <summary>An enrolment of a past year, which cannot be created any more, so it is built as a stored one.</summary>
    static Enrollment PastEnrollment(Student student) => new(Guid.NewGuid(), student.Id, _past.Id, _level.Id, null);

    static Assignment Held(Student student, Locker locker) =>
        new(Guid.NewGuid(), student.Id, locker.Id, _active.Id, _now, null, null, null);

    static Arca.Domain.Common.Result<AssignmentOpened> Open(Student student, Locker locker, params Assignment[] current) =>
        Assignment.Open(Guid.NewGuid(), student, _active, Enrol(student, _active), locker, current, _now);

    [Fact]
    [Trait("spec", Spec + ": Asignación uno a uno en el curso activo (Asignación correcta)")]
    public void A_free_locker_is_assigned_to_an_active_enrolled_student_with_an_event_in_each_history()
    {
        var student = Student();
        var locker = Locker();

        var result = Open(student, locker);

        var opened = result.Value!;
        Assert.True(opened.Assignment.IsCurrent);
        Assert.Equal(student.Id, opened.Assignment.StudentId);
        Assert.Equal(locker.Id, opened.Assignment.LockerId);
        Assert.Equal(_active.Id, opened.Assignment.YearId);
        Assert.Equal(_now, opened.Assignment.StartedAtUtc);
        Assert.Equal(StudentEventTypes.AssignmentOpened, opened.StudentEvent.Type);
        Assert.Equal(student.Id, opened.StudentEvent.EntityId);
        Assert.Equal(LockerEventTypes.Assigned, opened.LockerEvent.Type);
        Assert.Equal(locker.Id, opened.LockerEvent.EntityId);
        Assert.False(opened.ConsumesReservation);
    }

    [Fact]
    [Trait("spec", Spec + ": Asignación uno a uno en el curso activo (Alumno que ya tiene taquilla)")]
    public void A_student_that_already_holds_a_locker_is_refused_and_told_to_change_it()
    {
        var student = Student();
        var result = Open(student, Locker(20), Held(student, Locker(15)));

        Assert.Equal("Assignments.StudentHasLocker", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Asignación uno a uno en el curso activo (Taquilla ya ocupada)")]
    public void A_locker_held_by_another_student_is_refused()
    {
        var locker = Locker();
        var other = Student("otro@test.cat");

        var result = Open(Student(), locker, Held(other, locker));

        Assert.Equal("Assignments.LockerOccupied", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Asignación uno a uno en el curso activo (Alumno de baja)")]
    public void A_retired_student_is_refused()
    {
        var student = Student();
        var enrollment = Enrol(student, _active);
        student.Retire("Trasllat", _now);

        var result = Assignment.Open(Guid.NewGuid(), student, _active, enrollment, Locker(), [], _now);

        Assert.Equal("Assignments.StudentRetired", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Asignación uno a uno en el curso activo (Alumno sin matrícula del curso activo)")]
    public void A_student_without_an_enrolment_in_the_active_year_is_refused()
    {
        var student = Student();
        var withoutEnrollment = Assignment.Open(Guid.NewGuid(), student, _active, null, Locker(), [], _now);
        var pastEnrollment = Assignment.Open(Guid.NewGuid(), student, _active, PastEnrollment(student), Locker(), [], _now);
        var someoneElses = Assignment.Open(Guid.NewGuid(), student, _active, Enrol(Student("otro@test.cat"), _active), Locker(), [], _now);

        Assert.Equal("Assignments.StudentNotEnrolled", withoutEnrollment.Error!.Code);
        Assert.Equal("Assignments.StudentNotEnrolled", pastEnrollment.Error!.Code);
        Assert.Equal("Assignments.StudentNotEnrolled", someoneElses.Error!.Code);
    }

    [Fact]
    [Trait("spec", "alumnes-i-assignacions/curs-escolar: Un solo curso activo (Sin curso activo)")]
    public void Without_an_active_year_or_in_a_past_one_nothing_is_assigned()
    {
        var student = Student();

        Assert.Equal("SchoolYears.NoActiveYear", Assignment.Open(Guid.NewGuid(), student, null, null, Locker(), [], _now).Error!.Code);
        Assert.Equal("SchoolYears.NotActive", Assignment.Open(Guid.NewGuid(), student, _past, PastEnrollment(student), Locker(), [], _now).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Taquillas asignables (Taquilla averiada)")]
    public void A_broken_in_maintenance_or_retired_locker_is_not_available()
    {
        var broken = Locker(1);
        broken.MarkOutOfService(OutOfServiceKind.Broken, null, false, _now);
        var maintenance = Locker(2);
        maintenance.MarkOutOfService(OutOfServiceKind.Maintenance, null, false, _now);
        var retired = Locker(3);
        retired.Retire(false, _now);

        Assert.Equal("Assignments.LockerUnavailable", Open(Student(), broken).Error!.Code);
        Assert.Equal("Assignments.LockerUnavailable", Open(Student(), maintenance).Error!.Code);
        Assert.Equal("Assignments.LockerUnavailable", Open(Student(), retired).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Taquillas asignables (Taquilla reservada sin alumno)")]
    public void A_locker_reserved_with_no_student_has_to_have_the_reservation_removed_first()
    {
        var locker = Locker();
        locker.Reserve("Professorat", hasAssignment: false, _now);

        Assert.Equal("Assignments.LockerReserved", Open(Student(), locker).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Taquillas asignables (Taquilla reservada para otro alumno)")]
    public void A_locker_reserved_for_a_different_student_is_refused()
    {
        var locker = Locker();
        locker.ReserveForStudent(Student("otro@test.cat").Id, null, false, _now);

        Assert.Equal("Assignments.LockerReservedForOther", Open(Student(), locker).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Taquillas asignables (Taquilla reservada para el mismo alumno)")]
    public void A_locker_reserved_for_the_same_student_is_assigned_and_the_reservation_is_consumed()
    {
        var student = Student();
        var locker = Locker();
        locker.ReserveForStudent(student.Id, "Per a la Núria", false, _now);

        var result = Open(student, locker);

        Assert.True(result.Value!.ConsumesReservation);
        var consumed = locker.ConsumeReservation(_now);
        Assert.Equal(LockerEventTypes.ReservationConsumed, consumed.Value!.Type);
        Assert.False(locker.IsReserved);
        Assert.Null(locker.ReservedForStudentId);
    }

    [Fact]
    [Trait("spec", Spec + ": Liberación de una taquilla (Liberación manual)")]
    public void Closing_keeps_the_assignment_with_its_date_reason_and_note_and_records_both_events()
    {
        var student = Student();
        var locker = Locker();
        var assignment = Open(student, locker).Value!.Assignment;

        var result = assignment.Close(_active, AssignmentCloseReason.Released, "  Ha deixat el centre  ", _now.AddDays(10));

        Assert.False(assignment.IsCurrent);
        Assert.Equal(_now.AddDays(10), assignment.EndedAtUtc);
        Assert.Equal(AssignmentCloseReason.Released, assignment.CloseReason);
        Assert.Equal("Ha deixat el centre", assignment.CloseNote);
        Assert.Equal(StudentEventTypes.AssignmentClosed, result.Value!.StudentEvent.Type);
        Assert.Equal(LockerEventTypes.Released, result.Value.LockerEvent.Type);
        Assert.Contains("Released", result.Value.LockerEvent.AfterJson, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambio de taquilla (Cambio de curso activo)")]
    public void A_closed_assignment_cannot_be_closed_again_and_a_past_year_refuses_to_close()
    {
        var assignment = Open(Student(), Locker()).Value!.Assignment;

        var pastYear = assignment.Close(_past, AssignmentCloseReason.Released, null, _now);
        assignment.Close(_active, AssignmentCloseReason.Released, null, _now);
        var twice = assignment.Close(_active, AssignmentCloseReason.Released, null, _now);

        Assert.Equal("SchoolYears.NotActive", pastYear.Error!.Code);
        Assert.Equal("Assignments.AlreadyClosed", twice.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Liberación de una taquilla (Liberación manual)")]
    public void A_note_of_more_than_500_characters_is_refused_and_leaves_the_assignment_current()
    {
        var assignment = Open(Student(), Locker()).Value!.Assignment;

        var result = assignment.Close(_active, AssignmentCloseReason.Released, new string('m', 501), _now);

        Assert.Equal("Assignments.NoteTooLong", result.Error!.Code);
        Assert.True(assignment.IsCurrent);
    }

    [Fact]
    [Trait("spec", Spec + ": Liberación de una taquilla (Liberación de una taquilla averiada con alumno)")]
    public void A_closed_assignment_no_longer_counts_as_holding_the_locker()
    {
        var student = Student();
        var locker = Locker();
        var first = Open(student, locker).Value!.Assignment;
        first.Close(_active, AssignmentCloseReason.Released, null, _now);

        var again = Open(Student("otro@test.cat"), locker, first);

        Assert.True(again.IsSuccess);
    }

    [Fact]
    [Trait("spec", "alumnes-i-assignacions/assignacions: Reserva para un alumno (Reservar para un alumno)")]
    public void Reserving_for_a_student_records_the_student_and_a_removal_clears_it()
    {
        var student = Student();
        var locker = Locker();

        var reserved = locker.ReserveForStudent(student.Id, "nota", false, _now);
        Assert.Equal(student.Id, locker.ReservedForStudentId);
        Assert.Contains(student.Id.ToString(), reserved.Value!.AfterJson, StringComparison.Ordinal);
        Assert.Equal(LockerStatus.Reserved, locker.StateWith(false).Status);

        locker.RemoveReservation(_now);
        Assert.Null(locker.ReservedForStudentId);
        Assert.False(locker.IsReserved);
    }
}
