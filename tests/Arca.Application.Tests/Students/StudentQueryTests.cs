// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Students;
using Arca.Application.Students.ChangeStudentEnrollment;
using Arca.Application.Students.EditStudent;
using Arca.Application.Students.GetStudentHistory;
using Arca.Application.Students.ReactivateStudent;
using Arca.Application.Students.RetireStudent;
using Arca.Application.Students.SearchStudents;
using Arca.Domain.Students;
using Xunit;

namespace Arca.Application.Tests.Students;

public sealed class StudentQueryTests
{
    const string Spec = "alumnes-i-assignacions/alumnes";

    static SearchStudentsRequest Filter(string? text = null, Guid? level = null, Guid? group = null, int? locker = null,
        StudentLockerState state = StudentLockerState.Any, bool retired = false) =>
        new(new StudentFilter(text, level, group, locker, state, retired));

    static async Task<StudentsWorld> ClassAsync()
    {
        var world = new StudentsWorld();
        await world.StudentAsync("Núria", "García Puig", "nuria@test.cat", "1r ESO", "A");
        await world.StudentAsync("Pau", "Garcia Serra", "pau@test.cat", "1r ESO", "B");
        await world.StudentAsync("Àlex", "Martí Vila", "alex@test.cat", "2n ESO", "A");
        return world;
    }

    static string[] Names(Arca.Domain.Common.Result<StudentListing> result) => [.. result.Value!.Rows.Select(r => r.FirstName)];

    // --- Search ---

    [Fact]
    [Trait("spec", Spec + ": Búsqueda y consulta de alumnos (Búsqueda por apellido)")]
    public async Task Searching_garcia_finds_the_students_with_garcia_in_their_surnames_ignoring_case_and_accents()
    {
        var world = await ClassAsync();

        var found = await world.Search.HandleAsync(Filter("garcia"), default);
        var upper = await world.Search.HandleAsync(Filter("GARCÍA PUIG"), default);
        var byFirst = await world.Search.HandleAsync(Filter("alex"), default);

        Assert.Equal(["Núria", "Pau"], Names(found));
        Assert.Equal(["Núria"], Names(upper));
        Assert.Equal(["Àlex"], Names(byFirst));
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda y consulta de alumnos (Búsqueda por apellido)")]
    public async Task Every_word_typed_has_to_appear_in_the_name_and_the_order_is_by_surname_in_catalan()
    {
        var world = await ClassAsync();

        var both = await world.Search.HandleAsync(Filter("pau garcia"), default);
        var all = await world.Search.HandleAsync(Filter(), default);

        Assert.Equal(["Pau"], Names(both));
        Assert.Equal(["Núria", "Pau", "Àlex"], Names(all)); // García Puig, Garcia Serra, Martí Vila
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda y consulta de alumnos (Búsqueda por apellido)")]
    public async Task The_list_can_be_filtered_by_level_and_group()
    {
        var world = await ClassAsync();
        var first = world.Store.LevelList.Single(l => l.Name == "1r ESO");
        var groupB = world.Store.GroupList.Single(g => g.LevelId == first.Id && g.Name == "B");

        var byLevel = await world.Search.HandleAsync(Filter(level: first.Id), default);
        var byGroup = await world.Search.HandleAsync(Filter(level: first.Id, group: groupB.Id), default);

        Assert.Equal(["Núria", "Pau"], Names(byLevel));
        Assert.Equal(["Pau"], Names(byGroup));
        Assert.All(byLevel.Value!.Rows, r => Assert.Equal("1r ESO", r.LevelName));
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda y consulta de alumnos (Búsqueda por taquilla)")]
    public async Task Searching_the_number_of_an_occupied_locker_finds_the_student_that_holds_it()
    {
        var world = await ClassAsync();
        var pau = world.Store.StudentList.Single(s => s.FirstName == "Pau");
        world.Store.StudentLockers.Give(pau.Id, 15);

        var found = await world.Search.HandleAsync(Filter(locker: 15), default);
        var none = await world.Search.HandleAsync(Filter(locker: 99), default);

        Assert.Equal(["Pau"], Names(found));
        Assert.Equal(15, found.Value!.Rows.Single().LockerNumber);
        Assert.Empty(none.Value!.Rows);
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda y consulta de alumnos (Alumnos sin taquilla)")]
    public async Task The_students_without_a_locker_are_the_active_ones_that_hold_none_and_the_counters_add_up()
    {
        var world = await ClassAsync();
        var nuria = world.Store.StudentList.Single(s => s.FirstName == "Núria");
        world.Store.StudentLockers.Give(nuria.Id, 3);

        var without = await world.Search.HandleAsync(Filter(state: StudentLockerState.WithoutLocker), default);
        var with = await world.Search.HandleAsync(Filter(state: StudentLockerState.WithLocker), default);

        Assert.Equal(["Pau", "Àlex"], Names(without));
        Assert.Equal(["Núria"], Names(with));
        Assert.Equal(new StudentCounters(3, 1, 2), without.Value!.Counters);
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda y consulta de alumnos (Incluir bajas)")]
    public async Task Retired_students_are_hidden_by_default_and_shown_apart_when_asked()
    {
        var world = await ClassAsync();
        var pau = world.Store.StudentList.Single(s => s.FirstName == "Pau");
        await world.Retire.HandleAsync(new RetireStudentRequest(pau.Id, "Trasllat"), default);

        var byDefault = await world.Search.HandleAsync(Filter(), default);
        var withRetired = await world.Search.HandleAsync(Filter(retired: true), default);
        var withoutLockerIncludingRetired = await world.Search.HandleAsync(Filter(state: StudentLockerState.WithoutLocker, retired: true), default);

        Assert.Equal(["Núria", "Àlex"], Names(byDefault));
        Assert.Equal(["Núria", "Pau", "Àlex"], Names(withRetired));
        Assert.Equal([false, true, false], withRetired.Value!.Rows.Select(r => r.IsRetired));
        Assert.DoesNotContain("Pau", Names(withoutLockerIncludingRetired)); // a retired student is not "without a locker"
        Assert.Equal(2, byDefault.Value!.Counters.Active);
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda y consulta de alumnos (Sin resultados)")]
    public async Task No_match_gives_an_empty_list_and_no_error_and_a_missing_year_is_reported()
    {
        var world = await ClassAsync();
        var noYear = await new StudentsWorld().Search.HandleAsync(Filter(), default);

        var none = await world.Search.HandleAsync(Filter("zzz"), default);

        Assert.True(none.IsSuccess);
        Assert.Empty(none.Value!.Rows);
        Assert.Equal(3, none.Value.Counters.Active);
        Assert.Equal("SchoolYears.NoActiveYear", noYear.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda y consulta de alumnos (Búsqueda por apellido)")]
    public async Task Only_the_students_enrolled_in_the_active_year_are_listed()
    {
        var world = await ClassAsync();
        var orphan = Student.Create(Guid.NewGuid(), "Sense", "Matrícula", "sense@test.cat", [], world.Clock.UtcNow).Value!.Student;
        await world.Store.Students.AddAsync(orphan, default); // a student of a past year, not enrolled in this one

        var all = await world.Search.HandleAsync(Filter(), default);

        Assert.DoesNotContain("Sense", Names(all));
    }

    [Fact]
    [Trait("spec", Spec + ": Privacidad del correo (Listados y exportaciones)")]
    public async Task No_row_of_a_list_shows_any_email()
    {
        var world = await ClassAsync();

        var listing = (await world.Search.HandleAsync(Filter(), default)).Value!;

        var text = string.Join(" ", listing.Rows.Select(r => r.ToString()));
        Assert.DoesNotContain("@", text, StringComparison.Ordinal);
    }

    // --- History ---

    [Fact]
    [Trait("spec", Spec + ": Historial del alumno (Consulta del historial)")]
    public async Task The_history_is_most_recent_first_and_written_in_catalan_with_the_values_before_and_after()
    {
        var world = new StudentsWorld();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat", "1r ESO", "A");
        world.Clock.Advance(TimeSpan.FromMinutes(5));
        await world.Edit.HandleAsync(new EditStudentRequest(student.Id, "Núria", "Garcia", "nuria@test.cat"), default);
        world.Clock.Advance(TimeSpan.FromMinutes(5));
        await world.ChangeEnrollment.HandleAsync(new ChangeStudentEnrollmentRequest(student.Id, "2n ESO", null, true), default);
        world.Clock.Advance(TimeSpan.FromMinutes(5));
        await world.Retire.HandleAsync(new RetireStudentRequest(student.Id, "Trasllat"), default);
        world.Clock.Advance(TimeSpan.FromMinutes(5));
        await world.Reactivate.HandleAsync(new ReactivateStudentRequest(student.Id, "2n ESO"), default);

        var history = (await world.History.HandleAsync(new GetStudentHistoryRequest(student.Id), default)).Value!;

        Assert.Equal(
            [StudentEventTypes.Reactivated, StudentEventTypes.Retired, StudentEventTypes.EnrollmentChanged, StudentEventTypes.DataChanged, StudentEventTypes.Enrolled, StudentEventTypes.Created],
            history.Select(e => e.Type));
        Assert.Equal("Alumne reactivat.", history[0].Text);
        Assert.Equal("Baixa de l'alumne. Motiu: Trasllat", history[1].Text);
        Assert.Equal("Matrícula canviada de 1r ESO, grup A a 2n ESO.", history[2].Text);
        Assert.Equal("Dades corregides: cognoms: «García» → «Garcia».", history[3].Text);
        Assert.Equal("Matriculat al curs 2026-2027: 1r ESO, grup A.", history[4].Text);
        Assert.Equal("Alumne creat: Núria García.", history[5].Text);
    }

    [Fact]
    [Trait("spec", Spec + ": Historial del alumno (Historial inmutable)")]
    public async Task The_event_repository_offers_no_way_to_change_or_delete_and_an_unknown_student_is_reported()
    {
        var world = new StudentsWorld();

        var missing = await world.History.HandleAsync(new GetStudentHistoryRequest(Guid.NewGuid()), default);

        Assert.Equal(["AddAsync", "ListAsync"], typeof(IStudentEventRepository).GetMethods().Select(m => m.Name).Order());
        Assert.Equal("Students.NotFound", missing.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Historial del alumno (Consulta del historial)")]
    public async Task An_event_of_an_unknown_type_and_every_known_one_have_readable_text()
    {
        var world = new StudentsWorld();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        await world.Store.StudentEvents.AddAsync(
            new Arca.Domain.Common.HistoryEvent(student.Id, "Assignment.Opened", world.Clock.UtcNow.AddDays(1), null, null), default);
        var localizer = new ResxLocalizer();

        var history = (await world.History.HandleAsync(new GetStudentHistoryRequest(student.Id), default)).Value!;

        Assert.Equal("Esdeveniment no reconegut (Assignment.Opened).", history[0].Text);
        Assert.All(StudentEventTypes.All, type => Assert.NotEqual("History." + type, localizer.Get("History." + type)));
        Assert.NotEqual("History.Student.EnrolledWithGroup", localizer.Get("History.Student.EnrolledWithGroup"));
    }
}
