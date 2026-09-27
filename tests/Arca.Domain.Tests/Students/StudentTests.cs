// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Students;
using Xunit;

namespace Arca.Domain.Tests.Students;

public sealed class StudentTests
{
    const string Spec = "alumnes-i-assignacions/alumnes";

    static readonly DateTimeOffset _now = new(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);

    static Student Make(string first = "Núria", string last = "García Puig", string email = "nuria.garcia@test.cat", params Student[] others) =>
        Student.Create(Guid.NewGuid(), first, last, email, others, _now).Value!.Student;

    [Fact]
    [Trait("spec", Spec + ": Alta con matrícula (Alta con matrícula)")]
    public void A_new_student_is_active_with_a_normalised_email_and_a_creation_event()
    {
        var result = Student.Create(Guid.NewGuid(), "  Núria ", " García Puig", "  Nuria.Garcia@TEST.cat ", [], _now);

        var student = result.Value!.Student;
        Assert.False(student.IsRetired);
        Assert.Equal("Núria", student.FirstName);
        Assert.Equal("García Puig", student.LastName);
        Assert.Equal("nuria.garcia@test.cat", student.Email);
        Assert.Equal("nuria garcia puig", student.NameKey);
        Assert.Equal(StudentEventTypes.Created, result.Value.Event.Type);
        Assert.Equal(student.Id, result.Value.Event.EntityId);
        Assert.Null(result.Value.Event.BeforeJson);
    }

    [Theory]
    [InlineData(null, "García", "Students.FirstNameRequired")]
    [InlineData("  ", "García", "Students.FirstNameRequired")]
    [InlineData("Núria", null, "Students.LastNameRequired")]
    [InlineData("Núria", "", "Students.LastNameRequired")]
    [Trait("spec", Spec + ": Ficha de alumno persistente (Nombre y apellidos obligatorios)")]
    public void The_name_and_the_surnames_are_required(string? first, string? last, string code)
    {
        Assert.Equal(code, Student.Create(Guid.NewGuid(), first, last, "a@test.cat", [], _now).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Ficha de alumno persistente (Longitud máxima)")]
    public void More_than_100_characters_are_refused_and_100_are_accepted()
    {
        var long101 = new string('a', 101);
        var exact = new string('a', 100);

        var first = Student.Create(Guid.NewGuid(), long101, "García", "a@test.cat", [], _now);
        var last = Student.Create(Guid.NewGuid(), "Núria", long101, "a@test.cat", [], _now);

        Assert.Equal("Students.NameTooLong", first.Error!.Code);
        Assert.Equal(100, Assert.Single(first.Error.Args));
        Assert.Equal("Students.NameTooLong", last.Error!.Code);
        Assert.True(Student.Create(Guid.NewGuid(), exact, exact, "a@test.cat", [], _now).IsSuccess);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sense-arrova.cat")]
    [InlineData("dues@@test.cat")]
    [InlineData("a@b@test.cat")]
    [InlineData("@test.cat")]
    [InlineData("nuria@")]
    [InlineData("nuria@test")]
    [InlineData("nuria@.cat")]
    [InlineData("nuria@test..cat")]
    [InlineData("nuria garcia@test.cat")]
    [Trait("spec", Spec + ": Ficha de alumno persistente (Correo obligatorio)")]
    public void A_missing_or_malformed_email_is_refused(string? email)
    {
        Assert.Equal("Students.EmailInvalid", Student.Create(Guid.NewGuid(), "Núria", "García", email, [], _now).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Ficha de alumno persistente (Correo único)")]
    public void An_email_of_another_student_is_refused_ignoring_case_even_if_that_student_is_retired()
    {
        var active = Make(email: "nuria@test.cat");
        var retired = Make("Pau", "Serra", "pau@test.cat");
        retired.Retire("Trasllat", _now);

        var sameAsActive = Student.Create(Guid.NewGuid(), "Altra", "Persona", "NURIA@Test.CAT", [active, retired], _now);
        var sameAsRetired = Student.Create(Guid.NewGuid(), "Altra", "Persona", "pau@test.cat", [active, retired], _now);

        Assert.Equal("Students.EmailInUse", sameAsActive.Error!.Code);
        Assert.Equal("Students.EmailInUse", sameAsRetired.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Ficha de alumno persistente (Homónimos)")]
    public void Two_students_with_the_same_name_and_different_emails_are_different_people()
    {
        var first = Make("Núria", "García Puig", "nuria1@test.cat");

        var second = Student.Create(Guid.NewGuid(), "Núria", "García Puig", "nuria2@test.cat", [first], _now);

        Assert.True(second.IsSuccess);
        Assert.Equal(first.NameKey, second.Value!.Student.NameKey);
        Assert.NotEqual(first.Id, second.Value.Student.Id);
    }

    [Fact]
    [Trait("spec", Spec + ": Edición de datos del alumno (Corrección de un apellido)")]
    public void Correcting_a_surname_keeps_the_old_and_the_new_value_in_the_event()
    {
        var student = Make();

        var result = student.UpdateDetails("Núria", "Garcia Puig", "nuria.garcia@test.cat", [student], _now);

        Assert.Equal("Garcia Puig", student.LastName);
        Assert.Equal("nuria garcia puig", student.NameKey);
        Assert.Equal(StudentEventTypes.DataChanged, result.Value!.Type);
        Assert.Equal("{\"lastName\":\"García Puig\"}", result.Value.BeforeJson);
        Assert.Equal("{\"lastName\":\"Garcia Puig\"}", result.Value.AfterJson);
    }

    [Fact]
    [Trait("spec", Spec + ": Edición de datos del alumno (Corrección del correo)")]
    public void Correcting_the_email_to_a_free_one_keeps_both_values_in_the_event()
    {
        var student = Make(email: "vell@test.cat");

        var result = student.UpdateDetails("Núria", "García Puig", "Nou@Test.cat", [student], _now);

        Assert.Equal("nou@test.cat", student.Email);
        Assert.Contains("vell@test.cat", result.Value!.BeforeJson, StringComparison.Ordinal);
        Assert.Contains("nou@test.cat", result.Value.AfterJson, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Edición de datos del alumno (Correo ya registrado)")]
    public void Correcting_the_email_to_one_of_another_student_is_refused_and_changes_nothing()
    {
        var other = Make("Pau", "Serra", "pau@test.cat");
        var student = Make(email: "nuria@test.cat");

        var result = student.UpdateDetails("Núria", "García Puig", "pau@test.cat", [student, other], _now);

        Assert.Equal("Students.EmailInUse", result.Error!.Code);
        Assert.Equal("nuria@test.cat", student.Email);
    }

    [Fact]
    [Trait("spec", Spec + ": Edición de datos del alumno (Corrección de un apellido)")]
    public void Sending_the_same_data_or_invalid_data_changes_nothing()
    {
        var student = Make();

        Assert.Equal("Students.Unchanged", student.UpdateDetails("Núria", "García Puig", "NURIA.garcia@test.cat", [student], _now).Error!.Code);
        Assert.Equal("Students.LastNameRequired", student.UpdateDetails("Núria", " ", "nuria.garcia@test.cat", [student], _now).Error!.Code);
        Assert.Equal("Students.EmailInvalid", student.UpdateDetails("Núria", "García Puig", "x", [student], _now).Error!.Code);
        Assert.Equal("García Puig", student.LastName);
    }

    [Fact]
    [Trait("spec", Spec + ": Baja de un alumno (Baja sin taquilla)")]
    public void Retiring_records_the_reason_and_the_instant_and_keeps_the_record()
    {
        var student = Make();

        var result = student.Retire("  Trasllat a un altre centre ", _now);

        Assert.True(student.IsRetired);
        Assert.Equal(_now, student.RetiredAtUtc);
        Assert.Equal("Trasllat a un altre centre", student.RetirementReason);
        Assert.Equal(StudentEventTypes.Retired, result.Value!.Type);
        Assert.Contains("Trasllat", result.Value.AfterJson, StringComparison.Ordinal);
        Assert.Equal("nuria.garcia@test.cat", student.Email); // the record is kept
    }

    [Fact]
    [Trait("spec", Spec + ": Baja de un alumno (Baja ya registrada)")]
    public void Retiring_a_student_that_is_already_retired_is_refused()
    {
        var student = Make();
        student.Retire("Trasllat", _now);

        Assert.Equal("Students.AlreadyRetired", student.Retire("Un altre motiu", _now).Error!.Code);
        Assert.Equal("Trasllat", student.RetirementReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    [Trait("spec", Spec + ": Baja de un alumno (Baja sin taquilla)")]
    public void Retiring_needs_a_reason(string? reason)
    {
        var student = Make();

        Assert.Equal("Students.ReasonRequired", student.Retire(reason, _now).Error!.Code);
        Assert.False(student.IsRetired);
    }

    [Fact]
    [Trait("spec", Spec + ": Baja de un alumno (Baja sin taquilla)")]
    public void A_reason_of_more_than_500_characters_is_refused()
    {
        Assert.Equal("Students.ReasonTooLong", Make().Retire(new string('m', 501), _now).Error!.Code);
        Assert.True(Make().Retire(new string('m', 500), _now).IsSuccess);
    }

    [Fact]
    [Trait("spec", Spec + ": Reactivación de un alumno (Reactivación)")]
    public void Reactivating_gives_back_the_same_record_and_keeps_the_history()
    {
        var student = Make();
        var id = student.Id;
        student.Retire("Trasllat", _now);

        var result = student.Reactivate(_now.AddDays(30));

        Assert.False(student.IsRetired);
        Assert.Null(student.RetirementReason);
        Assert.Equal(id, student.Id);
        Assert.Equal("nuria.garcia@test.cat", student.Email);
        Assert.Equal(StudentEventTypes.Reactivated, result.Value!.Type);
        Assert.Contains("Trasllat", result.Value.BeforeJson, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Reactivación de un alumno (Alumno que no está de baja)")]
    public void Reactivating_an_active_student_is_refused()
    {
        Assert.Equal("Students.AlreadyActive", Make().Reactivate(_now).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Historial del alumno (Historial inmutable)")]
    public void The_student_event_types_are_stable_codes_and_unique()
    {
        Assert.All(StudentEventTypes.All, type => Assert.Matches("^Student\\.[A-Z][A-Za-z]+$", type));
        Assert.Equal(StudentEventTypes.All.Count, StudentEventTypes.All.Distinct().Count());
    }
}
