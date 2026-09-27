// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Students;
using Arca.Application.Students.ChangeStudentEnrollment;
using Arca.Application.Students.EditStudent;
using Arca.Application.Students.ReactivateStudent;
using Arca.Application.Students.RetireStudent;
using Arca.Application.Students.SearchStudents;
using Xunit;

namespace Arca.Application.Tests.Students;

public sealed class StudentFeedbackTests
{
    const string Spec = "alumnes-i-assignacions/alumnes";

    // --- Results with texts ---

    [Fact]
    [Trait("spec", Spec + ": Ficha de alumno persistente (Nombre y apellidos obligatorios)")]
    public async Task Every_change_to_a_student_has_a_visible_message_in_catalan()
    {
        var world = new StudentsWorld();
        var texts = new StudentResultTexts(world.Localizer);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");

        var edited = (await world.Edit.HandleAsync(new EditStudentRequest(student.Id, "Marta", "Puig Serra", student.Email), default)).Value!;
        var retired = (await world.Retire.HandleAsync(new RetireStudentRequest(student.Id, "Trasllat"), default)).Value!;
        var reactivated = (await world.Reactivate.HandleAsync(new ReactivateStudentRequest(student.Id, "1r ESO", "A", ConfirmNewValues: true), default))
            .Value!.Student!;
        var changed = (await world.ChangeEnrollment.HandleAsync(new ChangeStudentEnrollmentRequest(student.Id, "2n ESO", "B", ConfirmNewValues: true), default))
            .Value!.Student!;

        Assert.Equal("S'han actualitzat les dades de Marta Puig Serra.", texts.Edited(edited));
        Assert.Equal("S'ha donat de baixa Marta Puig Serra.", texts.Retired(retired));
        Assert.Equal("S'ha reactivat Marta Puig Serra.", texts.Reactivated(reactivated));
        Assert.Equal("S'ha canviat la matrícula de Marta Puig Serra.", texts.EnrollmentChanged(changed));
    }

    [Fact]
    [Trait("spec", Spec + ": Ficha de alumno persistente (Nombre y apellidos obligatorios)")]
    public async Task Adding_a_student_says_so_by_their_full_name()
    {
        var world = new StudentsWorld();
        var texts = new StudentResultTexts(world.Localizer);
        var student = await world.StudentAsync("Jordi", "Vidal", "jordi@example.com");

        Assert.Equal("S'ha donat d'alta Jordi Vidal.", texts.Added(student));
    }

    // --- Confirmations ---

    [Fact]
    [Trait("spec", Spec + ": Baja de un alumno (Baja con confirmación)")]
    public async Task Retiring_a_student_without_a_locker_asks_for_confirmation_with_no_mention_of_a_locker()
    {
        var world = new StudentsWorld();
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");

        var request = new StudentConfirmations(world.Localizer).ForRetire(student);

        Assert.Equal("Donar de baixa Marta Puig?", request.Title);
        Assert.Equal("Es donarà de baixa Marta Puig.", request.Consequence);
    }

    [Fact]
    [Trait("spec", Spec + ": Baja de un alumno (Baja con confirmación)")]
    public async Task Retiring_a_student_with_a_locker_asks_for_confirmation_saying_it_will_be_freed()
    {
        var world = new StudentsWorld();
        var assignments = new Arca.Application.Tests.Assignments.AssignmentsWorld();
        var zone = await assignments.ZoneAsync("Planta 1");
        var locker = await assignments.LockerAsync(1, zone);
        var studentDetail = await assignments.StudentAsync("Marta", "Puig", "marta@example.com");
        await assignments.AssignAsync(studentDetail.Id, locker);
        var withLocker = (await assignments.Students.Get.HandleAsync(new Arca.Application.Students.GetStudent.GetStudentRequest(studentDetail.Id), default)).Value!;

        var request = new StudentConfirmations(world.Localizer).ForRetire(withLocker);

        Assert.Contains("s'alliberarà la taquilla 1", request.Consequence, StringComparison.Ordinal);
    }

    // --- Empty states ---

    [Fact]
    [Trait("spec", Spec + ": Búsqueda y consulta de alumnos (Sin resultados)")]
    public async Task With_no_students_at_all_the_search_offers_adding_one()
    {
        var world = new StudentsWorld();
        await world.CreateYearAsync();

        var listing = (await world.Search.HandleAsync(new SearchStudentsRequest(), default)).Value!;
        var guide = StudentEmptyStates.Describe(listing.EmptyState, world.Localizer)!;

        Assert.Equal(StudentEmptyState.NoStudents, listing.EmptyState);
        Assert.Equal("Encara no hi ha cap alumne donat d'alta en aquest curs.", guide.Message);
        Assert.Equal([StudentSuggestedAction.AddStudent], guide.Actions);
        Assert.Equal("Dona d'alta un alumne", StudentEmptyStates.Label(StudentSuggestedAction.AddStudent, world.Localizer));
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda y consulta de alumnos (Sin resultados)")]
    public async Task With_no_match_the_search_offers_clearing_the_filters()
    {
        var world = new StudentsWorld();
        await world.StudentAsync("Marta", "Puig", "marta@example.com");

        var listing = (await world.Search.HandleAsync(new SearchStudentsRequest(new StudentFilter(Text: "Cap")), default)).Value!;
        var guide = StudentEmptyStates.Describe(listing.EmptyState, world.Localizer)!;

        Assert.Equal(StudentEmptyState.NoResults, listing.EmptyState);
        Assert.Equal([StudentSuggestedAction.ClearFilters], guide.Actions);
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda y consulta de alumnos (Alumnos sin taquilla)")]
    public async Task When_everyone_already_holds_a_locker_the_filter_says_so_instead_of_an_empty_list()
    {
        var assignments = new Arca.Application.Tests.Assignments.AssignmentsWorld();
        var zone = await assignments.ZoneAsync("Planta 1");
        var locker = await assignments.LockerAsync(1, zone);
        var student = await assignments.StudentAsync("Marta", "Puig", "marta@example.com");
        await assignments.AssignAsync(student.Id, locker);

        var listing = (await assignments.Students.Search.HandleAsync(
            new SearchStudentsRequest(new StudentFilter(LockerState: StudentLockerState.WithoutLocker)), default)).Value!;
        var guide = StudentEmptyStates.Describe(listing.EmptyState, assignments.Students.Localizer)!;

        Assert.Equal(StudentEmptyState.NoStudentsWithoutLocker, listing.EmptyState);
        Assert.Equal("Tots els alumnes actius ja tenen taquilla.", guide.Message);
        Assert.Empty(guide.Actions);
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda y consulta de alumnos (Sin resultados)")]
    public async Task Combining_without_locker_with_a_name_that_matches_nobody_offers_clearing_filters_not_a_false_all_assigned()
    {
        var assignments = new Arca.Application.Tests.Assignments.AssignmentsWorld();
        await assignments.StudentAsync("Marta", "Puig", "marta@example.com"); // active, without a locker

        var listing = (await assignments.Students.Search.HandleAsync(
            new SearchStudentsRequest(new StudentFilter(Text: "Cap", LockerState: StudentLockerState.WithoutLocker)), default)).Value!;

        Assert.Equal(StudentEmptyState.NoResults, listing.EmptyState);
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda y consulta de alumnos (Búsqueda por apellido)")]
    public async Task A_list_with_students_has_no_empty_state_and_no_guide()
    {
        var world = new StudentsWorld();
        await world.StudentAsync("Marta", "Puig", "marta@example.com");

        var listing = (await world.Search.HandleAsync(new SearchStudentsRequest(), default)).Value!;

        Assert.Equal(StudentEmptyState.None, listing.EmptyState);
        Assert.Null(StudentEmptyStates.Describe(listing.EmptyState, world.Localizer));
    }
}
