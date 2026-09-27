// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Catalog;
using Arca.Domain.Enrollments;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;
using Xunit;

namespace Arca.Domain.Tests.Students;

public sealed class EnrollmentTests
{
    const string Spec = "alumnes-i-assignacions/alumnes: Matrícula por curso";

    static readonly DateTimeOffset _now = new(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);
    static readonly AcademicYear _active = AcademicYear.Restore(Guid.NewGuid(), new DateOnly(2026, 9, 1), new DateOnly(2027, 6, 30), true);
    static readonly AcademicYear _past = AcademicYear.Restore(Guid.NewGuid(), new DateOnly(2025, 9, 1), new DateOnly(2026, 6, 30), false);
    static readonly Level _first = Level.Create(Guid.NewGuid(), "1r ESO", []).Value!;
    static readonly Level _second = Level.Create(Guid.NewGuid(), "2n ESO", [_first]).Value!;
    static readonly Group _a = Group.Create(Guid.NewGuid(), _first, "A", []).Value!;
    static readonly Group _b = Group.Create(Guid.NewGuid(), _first, "B", []).Value!;

    static Student Student(string email = "nuria@test.cat") =>
        Arca.Domain.Students.Student.Create(Guid.NewGuid(), "Núria", "García", email, [], _now).Value!.Student;

    static Enrollment Enrol(Student student, AcademicYear year, Level level, Group? group) =>
        Enrollment.Create(Guid.NewGuid(), student, year, level, group, [], _now).Value!.Enrollment;

    [Fact]
    [Trait("spec", Spec + " (Alta con matrícula)")]
    public void Enrolling_in_the_active_year_records_the_level_and_group_and_an_event_in_the_student_history()
    {
        var student = Student();

        var result = Enrollment.Create(Guid.NewGuid(), student, _active, _first, _a, [], _now);

        var enrollment = result.Value!.Enrollment;
        Assert.Equal(student.Id, enrollment.StudentId);
        Assert.Equal(_active.Id, enrollment.YearId);
        Assert.Equal(_first.Id, enrollment.LevelId);
        Assert.Equal(_a.Id, enrollment.GroupId);
        Assert.Equal(StudentEventTypes.Enrolled, result.Value.Event.Type);
        Assert.Equal(student.Id, result.Value.Event.EntityId);
    }

    [Fact]
    [Trait("spec", Spec + " (Alumno sin grupo)")]
    public void A_student_can_be_enrolled_without_a_group()
    {
        var result = Enrollment.Create(Guid.NewGuid(), Student(), _active, _first, null, [], _now);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Enrollment.GroupId);
    }

    [Fact]
    [Trait("spec", Spec + " (Alumno sin nivel)")]
    public void A_student_cannot_be_enrolled_without_a_level()
    {
        Assert.Equal("Enrollments.LevelRequired", Enrollment.Create(Guid.NewGuid(), Student(), _active, null, null, [], _now).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + " (Alta con matrícula)")]
    public void A_group_of_another_level_is_refused()
    {
        var result = Enrollment.Create(Guid.NewGuid(), Student(), _active, _second, _a, [], _now);

        Assert.Equal("Enrollments.GroupNotInLevel", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Matrícula por curso (como máximo una)")]
    public void A_student_has_at_most_one_enrolment_per_year_but_one_in_each_year()
    {
        var student = Student();
        var existing = Enrol(student, _active, _first, _a);

        var again = Enrollment.Create(Guid.NewGuid(), student, _active, _second, null, [existing], _now);
        var otherYear = Enrollment.Create(Guid.NewGuid(), student, _past, _second, null, [existing], _now);

        Assert.Equal("Enrollments.AlreadyEnrolled", again.Error!.Code);
        Assert.Equal("SchoolYears.NotActive", otherYear.Error!.Code); // the past year is read-only
    }

    [Fact]
    [Trait("spec", Spec + " (Alta con matrícula)")]
    public void A_retired_student_cannot_be_enrolled()
    {
        var student = Student();
        student.Retire("Trasllat", _now);

        Assert.Equal("Enrollments.StudentRetired", Enrollment.Create(Guid.NewGuid(), student, _active, _first, null, [], _now).Error!.Code);
    }

    [Fact]
    [Trait("spec", "alumnes-i-assignacions/curs-escolar: Histórico de solo lectura (Modificación en un curso anterior)")]
    public void Enrolling_needs_an_active_year_and_a_past_year_refuses_every_change()
    {
        var student = Student();
        var enrollment = Enrol(student, _active, _first, _a);

        Assert.Equal("SchoolYears.NoActiveYear", Enrollment.Create(Guid.NewGuid(), student, null, _first, null, [], _now).Error!.Code);
        Assert.Equal("SchoolYears.NotActive", Enrollment.Create(Guid.NewGuid(), student, _past, _first, null, [], _now).Error!.Code);
        Assert.Equal("SchoolYears.NotActive", enrollment.Change(_past, _second, null, _now).Error!.Code);
        Assert.Equal("SchoolYears.NoActiveYear", enrollment.Change(null, _second, null, _now).Error!.Code);
        Assert.Equal(_first.Id, enrollment.LevelId);
    }

    [Fact]
    [Trait("spec", Spec + " (Cambio de nivel o de grupo)")]
    public void Changing_the_group_updates_the_enrolment_and_records_before_and_after()
    {
        var enrollment = Enrol(Student(), _active, _first, _a);

        var result = enrollment.Change(_active, _first, _b, _now);

        Assert.Equal(_b.Id, enrollment.GroupId);
        Assert.Equal(StudentEventTypes.EnrollmentChanged, result.Value!.Type);
        Assert.Equal(enrollment.StudentId, result.Value.EntityId);
        Assert.Contains(_a.Id.ToString(), result.Value.BeforeJson, StringComparison.Ordinal);
        Assert.Contains(_b.Id.ToString(), result.Value.AfterJson, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + " (Cambio de nivel o de grupo)")]
    public void Changing_the_level_or_dropping_the_group_works_and_an_unchanged_or_invalid_change_is_refused()
    {
        var enrollment = Enrol(Student(), _active, _first, _a);

        Assert.Equal("Enrollments.Unchanged", enrollment.Change(_active, _first, _a, _now).Error!.Code);
        Assert.Equal("Enrollments.LevelRequired", enrollment.Change(_active, null, null, _now).Error!.Code);
        Assert.Equal("Enrollments.GroupNotInLevel", enrollment.Change(_active, _second, _a, _now).Error!.Code);
        Assert.True(enrollment.Change(_active, _second, null, _now).IsSuccess);
        Assert.Equal(_second.Id, enrollment.LevelId);
        Assert.Null(enrollment.GroupId);
    }

    [Fact]
    [Trait("spec", Spec + ": Ficha de alumno persistente (Continuidad entre cursos)")]
    public void A_student_keeps_the_record_across_years_and_only_the_enrolment_changes()
    {
        var student = Student();
        var id = student.Id;
        var email = student.Email;
        var thisYear = Enrol(student, _active, _first, _a);
        var nextYear = AcademicYear.Restore(Guid.NewGuid(), new DateOnly(2027, 9, 1), new DateOnly(2028, 6, 30), true);

        var next = Enrollment.Create(Guid.NewGuid(), student, nextYear, _second, null, [thisYear], _now);

        Assert.True(next.IsSuccess);
        Assert.Equal(id, student.Id);
        Assert.Equal(email, student.Email);
        Assert.Equal(student.Id, next.Value!.Enrollment.StudentId);
        Assert.NotEqual(thisYear.Id, next.Value.Enrollment.Id);
        Assert.Equal(_first.Id, thisYear.LevelId); // last year's enrolment stays as it was
    }
}
