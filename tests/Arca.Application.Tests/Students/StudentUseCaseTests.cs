// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Students;
using Arca.Application.Students.AddStudent;
using Arca.Application.Students.ChangeStudentEnrollment;
using Arca.Application.Students.EditStudent;
using Arca.Application.Students.GetStudent;
using Arca.Application.Students.ReactivateStudent;
using Arca.Application.Students.RetireStudent;
using Arca.Domain.Students;
using Xunit;

namespace Arca.Application.Tests.Students;

public sealed class StudentUseCaseTests
{
    const string Spec = "alumnes-i-assignacions/alumnes";

    static List<string> EventTypes(StudentsWorld world, Guid studentId) =>
        [.. world.Store.StudentEventList.Where(e => e.EntityId == studentId).OrderBy(e => e.OccurredAtUtc).Select(e => e.Type)];

    // --- Sign-up ---

    [Fact]
    [Trait("spec", Spec + ": Matrícula por curso (Alta con matrícula)")]
    public async Task A_student_is_signed_up_with_the_enrolment_of_the_active_year_and_two_events()
    {
        var world = new StudentsWorld();
        await world.CreateYearAsync();

        var result = await world.Add.HandleAsync(new AddStudentRequest("Núria", "García Puig", "nuria@test.cat", "1r ESO", "A", ConfirmNewValues: true), default);

        var student = result.Value!.Student!;
        Assert.False(student.IsRetired);
        Assert.Equal("2026-2027", student.YearName);
        Assert.Equal("1r ESO", student.LevelName);
        Assert.Equal("A", student.GroupName);
        Assert.Equal("nuria@test.cat", student.Email);
        Assert.Equal([StudentEventTypes.Created, StudentEventTypes.Enrolled], EventTypes(world, student.Id));
        Assert.Single(world.Store.EnrollmentList);
    }

    [Fact]
    [Trait("spec", Spec + ": Catálogo de niveles y grupos (Valor nuevo en un alta manual)")]
    public async Task A_new_level_or_group_asks_for_confirmation_and_saves_nothing_until_confirmed()
    {
        var world = new StudentsWorld();
        await world.CreateYearAsync();
        var request = new AddStudentRequest("Núria", "García", "nuria@test.cat", "1r ESO", "A");

        var asked = await world.Add.HandleAsync(request, default);

        Assert.True(asked.Value!.NeedsConfirmation);
        Assert.Equal(
            [new NewCatalogValue(CatalogKind.Level, "1r ESO"), new NewCatalogValue(CatalogKind.Group, "A", "1r ESO")], asked.Value.NewValues);
        Assert.Empty(world.Store.StudentList);
        Assert.Empty(world.Store.LevelList);
        Assert.Empty(world.Store.GroupList);
        Assert.Empty(world.Store.StudentEventList);

        var confirmed = await world.Add.HandleAsync(request with { ConfirmNewValues = true }, default);

        Assert.False(confirmed.Value!.NeedsConfirmation);
        Assert.Single(world.Store.LevelList);
        Assert.Single(world.Store.GroupList);
        Assert.Single(world.Store.StudentList);
    }

    [Fact]
    [Trait("spec", Spec + ": Catálogo de niveles y grupos (Equivalencia de grafías)")]
    public async Task An_existing_level_and_group_are_reused_whatever_their_case_and_need_no_confirmation()
    {
        var world = new StudentsWorld();
        await world.StudentAsync("Núria", "García", "nuria@test.cat", "1r ESO", "A");

        var result = await world.Add.HandleAsync(new AddStudentRequest("Pau", "Serra", "pau@test.cat", "1R eso", "a"), default);

        Assert.False(result.Value!.NeedsConfirmation);
        Assert.Equal("1r ESO", result.Value.Student!.LevelName);
        Assert.Single(world.Store.LevelList);
        Assert.Single(world.Store.GroupList);
    }

    [Fact]
    [Trait("spec", Spec + ": Catálogo de niveles y grupos (Grupo por nivel)")]
    public async Task Only_the_group_that_is_new_needs_confirming_when_the_level_exists()
    {
        var world = new StudentsWorld();
        await world.StudentAsync("Núria", "García", "nuria@test.cat", "1r ESO", "A");

        var asked = await world.Add.HandleAsync(new AddStudentRequest("Pau", "Serra", "pau@test.cat", "1r ESO", "B"), default);

        Assert.Equal([new NewCatalogValue(CatalogKind.Group, "B", "1r ESO")], asked.Value!.NewValues);
    }

    [Fact]
    [Trait("spec", Spec + ": Matrícula por curso (Alumno sin grupo)")]
    public async Task A_student_can_be_signed_up_without_a_group_but_not_without_a_level()
    {
        var world = new StudentsWorld();
        await world.CreateYearAsync();

        var withoutGroup = await world.Add.HandleAsync(new AddStudentRequest("Núria", "García", "nuria@test.cat", "1r ESO", null, true), default);
        var withoutLevel = await world.Add.HandleAsync(new AddStudentRequest("Pau", "Serra", "pau@test.cat", null, "A", true), default);

        Assert.Null(withoutGroup.Value!.Student!.GroupName);
        Assert.Equal("Enrollments.LevelRequired", withoutLevel.Error!.Code);
        Assert.Single(world.Store.StudentList);
    }

    [Fact]
    [Trait("spec", "alumnes-i-assignacions/curs-escolar: Un solo curso activo (Sin curso activo)")]
    public async Task Without_an_active_year_a_student_cannot_be_signed_up()
    {
        var world = new StudentsWorld();

        var result = await world.Add.HandleAsync(new AddStudentRequest("Núria", "García", "nuria@test.cat", "1r ESO", "A", true), default);

        Assert.Equal("SchoolYears.NoActiveYear", result.Error!.Code);
        Assert.Empty(world.Store.StudentList);
    }

    [Theory]
    [InlineData("", "García", "a@test.cat", "Students.FirstNameRequired")]
    [InlineData("Núria", "", "a@test.cat", "Students.LastNameRequired")]
    [InlineData("Núria", "García", "no-es-un-correu", "Students.EmailInvalid")]
    [Trait("spec", Spec + ": Ficha de alumno persistente (Nombre y apellidos obligatorios)")]
    public async Task Invalid_data_saves_nothing_not_even_a_catalogue_value(string first, string last, string email, string code)
    {
        var world = new StudentsWorld();
        await world.CreateYearAsync();

        var result = await world.Add.HandleAsync(new AddStudentRequest(first, last, email, "1r ESO", "A", true), default);

        Assert.Equal(code, result.Error!.Code);
        Assert.Empty(world.Store.StudentList);
        Assert.Empty(world.Store.LevelList);
    }

    [Fact]
    [Trait("spec", Spec + ": Ficha de alumno persistente (Correo único)")]
    public async Task A_repeated_email_is_refused_even_with_other_case_and_namesakes_are_accepted()
    {
        var world = new StudentsWorld();
        await world.StudentAsync("Núria", "García", "nuria@test.cat");

        var repeated = await world.Add.HandleAsync(new AddStudentRequest("Altra", "Persona", "NURIA@test.cat", "1r ESO", "A"), default);
        var namesake = await world.Add.HandleAsync(new AddStudentRequest("Núria", "García", "nuria2@test.cat", "1r ESO", "A"), default);

        Assert.Equal("Students.EmailInUse", repeated.Error!.Code);
        Assert.True(namesake.IsSuccess);
        Assert.Equal(2, world.Store.StudentList.Count);
    }

    // --- Edit ---

    [Fact]
    [Trait("spec", Spec + ": Edición de datos del alumno (Corrección de un apellido)")]
    public async Task Correcting_a_surname_saves_it_and_records_the_change()
    {
        var world = new StudentsWorld();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");

        var result = await world.Edit.HandleAsync(new EditStudentRequest(student.Id, "Núria", "Garcia", "nuria@test.cat"), default);

        Assert.Equal("Garcia", result.Value!.LastName);
        Assert.Equal(StudentEventTypes.DataChanged, EventTypes(world, student.Id).Last());
    }

    [Fact]
    [Trait("spec", Spec + ": Edición de datos del alumno (Correo ya registrado)")]
    public async Task Correcting_the_email_to_one_that_is_taken_is_refused_and_an_unknown_student_is_reported()
    {
        var world = new StudentsWorld();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        await world.StudentAsync("Pau", "Serra", "pau@test.cat");

        var taken = await world.Edit.HandleAsync(new EditStudentRequest(student.Id, "Núria", "García", "PAU@test.cat"), default);
        var missing = await world.Edit.HandleAsync(new EditStudentRequest(Guid.NewGuid(), "A", "B", "a@test.cat"), default);

        Assert.Equal("Students.EmailInUse", taken.Error!.Code);
        Assert.Equal("Students.NotFound", missing.Error!.Code);
        Assert.Equal("nuria@test.cat", world.Store.StudentList.First(s => s.Id == student.Id).Email);
    }

    // --- Enrolment ---

    [Fact]
    [Trait("spec", Spec + ": Matrícula por curso (Cambio de nivel o de grupo)")]
    public async Task Changing_the_group_updates_the_enrolment_of_the_active_year_and_the_history()
    {
        var world = new StudentsWorld();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat", "1r ESO", "A");

        var asked = await world.ChangeEnrollment.HandleAsync(new ChangeStudentEnrollmentRequest(student.Id, "1r ESO", "B"), default);
        var done = await world.ChangeEnrollment.HandleAsync(new ChangeStudentEnrollmentRequest(student.Id, "1r ESO", "B", true), default);

        Assert.True(asked.Value!.NeedsConfirmation); // the group B is new
        Assert.Equal("B", done.Value!.Student!.GroupName);
        Assert.Equal(StudentEventTypes.EnrollmentChanged, EventTypes(world, student.Id).Last());
    }

    [Fact]
    [Trait("spec", Spec + ": Matrícula por curso (Cambio de nivel o de grupo)")]
    public async Task Changing_to_the_same_placement_or_for_an_unknown_student_is_refused()
    {
        var world = new StudentsWorld();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat", "1r ESO", "A");

        var same = await world.ChangeEnrollment.HandleAsync(new ChangeStudentEnrollmentRequest(student.Id, "1R ESO", "a"), default);
        var missing = await world.ChangeEnrollment.HandleAsync(new ChangeStudentEnrollmentRequest(Guid.NewGuid(), "1r ESO"), default);

        Assert.Equal("Enrollments.Unchanged", same.Error!.Code);
        Assert.Equal("Students.NotFound", missing.Error!.Code);
    }

    // --- Retirement and reactivation ---

    [Fact]
    [Trait("spec", Spec + ": Baja de un alumno (Baja sin taquilla)")]
    public async Task Retiring_keeps_the_record_and_the_enrolment_and_records_the_reason()
    {
        var world = new StudentsWorld();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");

        var result = await world.Retire.HandleAsync(new RetireStudentRequest(student.Id, "Trasllat"), default);

        Assert.True(result.Value!.IsRetired);
        Assert.Equal("Trasllat", result.Value.RetirementReason);
        Assert.Single(world.Store.EnrollmentList);
        Assert.Equal(StudentEventTypes.Retired, EventTypes(world, student.Id).Last());
    }

    [Fact]
    [Trait("spec", Spec + ": Baja de un alumno (Baja ya registrada)")]
    public async Task Retiring_twice_or_without_a_reason_is_refused()
    {
        var world = new StudentsWorld();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");

        var noReason = await world.Retire.HandleAsync(new RetireStudentRequest(student.Id, " "), default);
        await world.Retire.HandleAsync(new RetireStudentRequest(student.Id, "Trasllat"), default);
        var twice = await world.Retire.HandleAsync(new RetireStudentRequest(student.Id, "Un altre"), default);

        Assert.Equal("Students.ReasonRequired", noReason.Error!.Code);
        Assert.Equal("Students.AlreadyRetired", twice.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Reactivación de un alumno (Reactivación)")]
    public async Task Reactivating_gives_the_same_record_back_with_an_enrolment_in_the_active_year()
    {
        var world = new StudentsWorld();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat", "1r ESO", "A");
        await world.Retire.HandleAsync(new RetireStudentRequest(student.Id, "Trasllat"), default);

        var result = await world.Reactivate.HandleAsync(new ReactivateStudentRequest(student.Id, "2n ESO", "B", ConfirmNewValues: true), default);

        Assert.False(result.Value!.Student!.IsRetired);
        Assert.Equal(student.Id, result.Value.Student.Id);
        Assert.Equal("2n ESO", result.Value.Student.LevelName);
        Assert.Equal("B", result.Value.Student.GroupName);
        Assert.Single(world.Store.EnrollmentList); // updated, not duplicated
        Assert.Equal(
            [StudentEventTypes.Created, StudentEventTypes.Enrolled, StudentEventTypes.Retired, StudentEventTypes.Reactivated, StudentEventTypes.EnrollmentChanged],
            EventTypes(world, student.Id));
    }

    [Fact]
    [Trait("spec", Spec + ": Reactivación de un alumno (Reactivación)")]
    public async Task Reactivating_with_the_same_placement_changes_no_enrolment_and_new_values_need_confirming()
    {
        var world = new StudentsWorld();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat", "1r ESO", "A");
        await world.Retire.HandleAsync(new RetireStudentRequest(student.Id, "Trasllat"), default);

        var asked = await world.Reactivate.HandleAsync(new ReactivateStudentRequest(student.Id, "3r ESO"), default);
        Assert.True(asked.Value!.NeedsConfirmation);
        Assert.True(world.Store.StudentList.Single().IsRetired); // nothing changed yet

        var done = await world.Reactivate.HandleAsync(new ReactivateStudentRequest(student.Id, "1r ESO", "A"), default);

        Assert.False(done.Value!.Student!.IsRetired);
        Assert.DoesNotContain(StudentEventTypes.EnrollmentChanged, EventTypes(world, student.Id));
    }

    [Fact]
    [Trait("spec", Spec + ": Reactivación de un alumno (Alumno que no está de baja)")]
    public async Task Reactivating_an_active_student_is_refused()
    {
        var world = new StudentsWorld();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");

        var result = await world.Reactivate.HandleAsync(new ReactivateStudentRequest(student.Id, "1r ESO", "A"), default);

        Assert.Equal("Students.AlreadyActive", result.Error!.Code);
    }

    // --- Detail and privacy ---

    [Fact]
    [Trait("spec", Spec + ": Privacidad del correo (Consulta de la ficha)")]
    public async Task Only_the_detail_carries_the_email_and_the_lists_carry_no_email_at_all()
    {
        var world = new StudentsWorld();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");

        var detail = await world.Get.HandleAsync(new GetStudentRequest(student.Id), default);
        var missing = await world.Get.HandleAsync(new GetStudentRequest(Guid.NewGuid()), default);

        Assert.Equal("nuria@test.cat", detail.Value!.Email);
        Assert.Equal("Students.NotFound", missing.Error!.Code);
        Assert.DoesNotContain(typeof(StudentRow).GetProperties(), p => p.Name.Contains("Email", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(StudentListing).GetProperties(), p => p.Name.Contains("Email", StringComparison.OrdinalIgnoreCase));
    }
}
