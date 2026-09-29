// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Assignments.CheckAssignmentTarget;
using Arca.Application.Catalog.ListCatalog;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Application.Lockers;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Search;
using Arca.Application.Students;
using Arca.Application.Students.AddStudent;
using Arca.Application.Students.ChangeStudentEnrollment;
using Arca.Application.Students.EditStudent;
using Arca.Application.Students.GetStudentScreen;
using Arca.Application.Students.ListStudentRows;
using Arca.Application.Students.ReactivateStudent;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Actions;
using Arca.UI.Assigning;
using Arca.UI.Lists;
using Arca.UI.Screens;
using Arca.UI.Students;
using Xunit;

namespace Arca.UI.Tests;

public sealed class StudentsScreenTests
{
    const string Spec = "pantalles-de-domini/pantalles-alumnes-i-assignacions";

    sealed class FakeForms : IFormDialogs
    {
        public List<IFormModel> Shown { get; } = [];

        public IFormModel Last => Shown[^1];

        public Task<bool> ShowAsync(IFormModel form)
        {
            Shown.Add(form);
            return Task.FromResult(false);
        }
    }

    sealed class FakeChoices : IChoiceDialogs
    {
        public List<ChoiceRequest> Asked { get; } = [];

        public Task<string?> ChooseAsync(ChoiceRequest request)
        {
            Asked.Add(request);
            return Task.FromResult<string?>(null);
        }
    }

    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    readonly ManualDelay _delay = new();
    readonly ResxLocalizer _localizer = new();
    readonly FakeForms _forms = new();
    readonly FakeChoices _choices = new();
    readonly List<StudentListRow> _students = [];
    readonly List<LockerListRow> _lockers = [];
    readonly List<string> _calls = [];
    readonly Dictionary<Guid, StudentScreenDetail> _details = [];
    RecordingConfirmations _confirmations = new(true);
    bool _noYear;
    AssignmentTargetCheck _check = new(null, []);
    Result<AssignLockerResult>? _assignFirst;
    int _courseOpened;
    int _refreshes;

    StudentListRow AddStudent(string first, string last, string level = "1r ESO", string group = "A", int? locker = null, bool debt = false, bool retired = false)
    {
        var row = new StudentListRow(Guid.NewGuid(), first, last, level, group, retired, locker, debt, debt ? 70m : 0m);
        _students.Add(row);
        _details[row.Id] = Detail(row);
        return row;
    }

    static StudentScreenDetail Detail(StudentListRow row) => new(
        new StudentDetail(row.Id, row.FirstName, row.LastName, row.FirstName.ToLowerInvariant() + "@example.com", row.IsRetired, row.IsRetired ? "Ha marxat" : null, null, "2026-2027", row.LevelName, row.GroupName, row.LockerNumber),
        row.LockerNumber is null ? null : Guid.NewGuid(), row.HasDebt, row.PendingTotal,
        row.IsRetired ? new Error("Students.AlreadyRetired") : null,
        row.IsRetired ? null : new Error("Students.AlreadyActive"),
        row.IsRetired ? new Error("Assignments.StudentRetired") : row.LockerNumber is not null ? new Error("Assignments.StudentHasLocker") : null,
        row.LockerNumber is null ? new Error("Assignments.NoAssignment") : null,
        row.LockerNumber is null ? new Error("Assignments.NoAssignment") : null);

    LockerListRow AddLocker(int number, string zone, LockerStatusView status = LockerStatusView.Free)
    {
        var row = new LockerListRow(Guid.NewGuid(), number, ZoneId(zone), zone, status, null, false, null, null);
        _lockers.Add(row);
        return row;
    }

    readonly Dictionary<string, Guid> _zoneIds = [];

    Guid ZoneId(string zone) => _zoneIds.TryGetValue(zone, out var id) ? id : _zoneIds[zone] = Guid.NewGuid();

    StudentServices Services() => new(
        _ => Task.FromResult(_noYear
            ? Result<StudentRowsListing>.Failure(new Error("SchoolYears.NoActiveYear"))
            : Result<StudentRowsListing>.Success(new StudentRowsListing(
                [.. _students.OrderBy(s => s.LastName)],
                new StudentCounters(_students.Count(s => !s.IsRetired), _students.Count(s => !s.IsRetired && s.LockerNumber is not null), _students.Count(s => !s.IsRetired && s.LockerNumber is null)),
                _students.Count == 0 ? StudentEmptyState.NoStudents : StudentEmptyState.None))),
        _ => Task.FromResult(Result<CatalogListing>.Success(new CatalogListing(["1r ESO", "2n ESO"], ["A", "B"]))),
        (id, _) => Task.FromResult(Result<StudentScreenDetail>.Success(_details[id])),
        (_, _) => Task.FromResult(Result<IReadOnlyList<string>>.Success(["alta"])),
        (_, _) => Task.FromResult(Result<IReadOnlyList<string>>.Success(["Taquilla 5"])),
        (request, _) =>
        {
            if (string.IsNullOrWhiteSpace(request.FirstName))
            {
                return Task.FromResult(Result<StudentChangeResult>.Failure(new Error("Students.FirstNameRequired")));
            }

            if (request.Email == "repe@example.com")
            {
                return Task.FromResult(Result<StudentChangeResult>.Failure(new Error("Students.EmailInUse")));
            }

            if (request.GroupName == "Z" && !request.ConfirmNewValues)
            {
                return Task.FromResult(Result<StudentChangeResult>.Success(new StudentChangeResult(null, [new NewCatalogValue(CatalogKind.Group, "Z", "1r ESO")])));
            }

            _calls.Add($"add {request.FirstName} {request.LastName} {request.LevelName} {request.GroupName} {request.ConfirmNewValues}");
            var row = AddStudent(request.FirstName!, request.LastName!, request.LevelName!, request.GroupName ?? string.Empty);
            return Task.FromResult(StudentChangeResult.Done(_details[row.Id].Student));
        },
        (request, _) =>
        {
            _calls.Add($"edit {request.FirstName} {request.LastName} {request.Email}");
            return Task.FromResult(Result<StudentDetail>.Success(_details[request.StudentId].Student with { FirstName = request.FirstName! }));
        },
        (request, _) =>
        {
            _calls.Add($"enrollment {request.LevelName} {request.GroupName} {request.ConfirmNewValues}");
            return Task.FromResult(StudentChangeResult.Done(_details[request.StudentId].Student));
        },
        (id, reason, _) =>
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return Task.FromResult(Result<StudentDetail>.Failure(new Error("Students.ReasonRequired")));
            }

            _calls.Add($"retire {reason}");
            return Task.FromResult(Result<StudentDetail>.Success(_details[id].Student));
        },
        (request, _) =>
        {
            _calls.Add($"reactivate {request.LevelName}");
            return Task.FromResult(StudentChangeResult.Done(_details[request.StudentId].Student));
        },
        (id, _) =>
        {
            _calls.Add("release");
            return Task.FromResult(Result<string>.Success("Taquilla alliberada."));
        });

    AssignmentPickerServices Pickers() => new(
        _ => Task.FromResult(Result<LockerRowsListing>.Success(new LockerRowsListing([.. _lockers], new LockerCounters(0, 0, 0, 0, 0, 0), LockerEmptyState.None))),
        (_, _) =>
        {
            var free = _lockers.Where(l => l.Status == LockerStatusView.Free).OrderBy(l => l.ZoneName).ThenBy(l => l.Number).FirstOrDefault();
            return Task.FromResult(free is null
                ? Result<LockerSuggestion>.Failure(new Error("Assignments.NoFreeLockers"))
                : Result<LockerSuggestion>.Success(new LockerSuggestion(new LockerRow(free.Id, free.Number, free.ZoneId, free.ZoneName, Arca.Domain.Lockers.LockerStatus.Free, false, false, null, null, false), false)));
        },
        (_, _, _) => Task.FromResult(Result<AssignmentTargetCheck>.Success(_check)),
        (request, _) =>
        {
            _calls.Add($"assign {request.LockerId} {request.ConfirmWarnings}");
            if (_assignFirst is not null && !request.ConfirmWarnings)
            {
                return Task.FromResult(_assignFirst);
            }

            return Task.FromResult(Result<AssignLockerResult>.Success(new AssignLockerResult(
                new AssignmentRow(Guid.NewGuid(), request.StudentId, "Marta Puig", request.LockerId, 5, "Planta 1", "2026-2027", DateTimeOffset.UtcNow, null, null, null), [])));
        },
        (request, _) =>
        {
            _calls.Add($"change {request.LockerId}");
            return Task.FromResult(Result<AssignLockerResult>.Success(new AssignLockerResult(
                new AssignmentRow(Guid.NewGuid(), request.StudentId, "Marta Puig", request.LockerId, 9, "Planta 1", "2026-2027", DateTimeOffset.UtcNow, null, null, null), [])));
        },
        _ => Task.FromResult(Result<StudentRowsListing>.Success(new StudentRowsListing([.. _students], new StudentCounters(0, 0, 0), StudentEmptyState.None))),
        (_, _) => Task.FromResult<IReadOnlyList<string>>(["Quota del curs 2025-2026: 50,00 €"]));

    ScreenContext Context() => new(
        _localizer, _notifications, _log, _delay, _confirmations, _forms, () =>
        {
            _refreshes++;
            return Task.CompletedTask;
        }, _choices);

    StudentsViewModel Model() => new(
        Services(), Context(), new AssignmentDialogs(Pickers(), Context()), new ActionRegistry(_localizer, UiPlatform.Windows)[StandardActions.New],
        () =>
        {
            _courseOpened++;
            return Task.CompletedTask;
        },
        new Arca.UI.Charges.StudentChargesViewModel(new FakeChargeWorld().Services(), Context(), () => Task.CompletedTask));

    // --- The list ---

    [Fact]
    [Trait("spec", Spec + ": Lista de alumnos con búsqueda y filtros (Lista por defecto)")]
    public async Task The_list_shows_the_active_students_by_surname_with_the_counters_and_no_email_column()
    {
        AddStudent("Marta", "Puig", locker: 5);
        AddStudent("Pau", "Alsina");
        AddStudent("Oriol", "Zamora", retired: true);
        var model = Model();

        await model.LoadAsync();

        Assert.Equal(["Alsina", "Puig"], model.Students.List.Rows.Select(s => s.LastName));
        Assert.Contains("2 actius", model.CountersText, StringComparison.Ordinal);
        Assert.Contains("1 amb taquilla", model.CountersText, StringComparison.Ordinal);
        Assert.DoesNotContain(model.Students.List.Columns, c => c.Header.Contains("mail", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("spec", Spec + ": Lista de alumnos con búsqueda y filtros (Búsqueda)")]
    public async Task Searching_ignores_case_and_accents_and_finds_a_student_by_their_locker_number()
    {
        AddStudent("Núria", "García", locker: 12);
        AddStudent("Pau", "Alsina");
        var model = Model();
        await model.LoadAsync();

        model.Students.List.FilterText = "garcia";
        Assert.Equal(["García"], model.Students.List.Rows.Select(s => s.LastName));

        model.Students.List.FilterText = "12";
        Assert.Equal(["García"], model.Students.List.Rows.Select(s => s.LastName));
    }

    [Fact]
    [Trait("spec", Spec + ": Lista de alumnos con búsqueda y filtros (Alumnos sin taquilla)")]
    public async Task The_filters_narrow_by_level_group_and_assignment_and_including_the_retired_shows_them_with_text()
    {
        AddStudent("Marta", "Puig", "1r ESO", "A", locker: 5);
        AddStudent("Pau", "Alsina", "2n ESO", "B");
        AddStudent("Oriol", "Zamora", "1r ESO", "B", retired: true);
        var model = Model();
        await model.LoadAsync();

        model.LockerFilter = "without";
        Assert.Equal(["Alsina"], model.Students.List.Rows.Select(s => s.LastName));
        model.LockerFilter = string.Empty;
        model.LevelFilter = "1r ESO";
        Assert.Equal(["Puig"], model.Students.List.Rows.Select(s => s.LastName));
        model.IncludeRetired = true;
        Assert.Equal(["Puig", "Zamora"], model.Students.List.Rows.Select(s => s.LastName));
        var state = model.Students.List.Columns.Single(c => c.Id == "state");
        Assert.Equal("De baixa", state.Text(model.Students.List.Rows[1]));
    }

    [Fact]
    [Trait("spec", Spec + ": Lista de alumnos con búsqueda y filtros (Sin resultados)")]
    public async Task No_match_says_so_and_offers_to_clear_and_no_students_offers_to_add_and_mentions_the_import()
    {
        var model = Model();
        await model.LoadAsync();
        Assert.Equal(ListViewState.Empty, model.Students.State.State);
        Assert.Contains("importació", model.Students.State.Message, StringComparison.Ordinal);
        Assert.Equal(model.NewStudent.Label, model.Students.State.Actions.Single().Label);

        AddStudent("Marta", "Puig");
        await model.LoadAsync();
        model.Students.List.FilterText = "zzz";
        Assert.Equal(ListViewState.NoResults, model.Students.State.State);
        model.Students.State.Actions[0].Command.Execute(null);
        Assert.Equal(ListViewState.Content, model.Students.State.State);
    }

    [Fact]
    [Trait("spec", Spec + ": Alumnos sin curso activo (Sin curso activo)")]
    public async Task Without_an_active_year_the_section_says_so_offers_the_course_and_disables_adding_with_the_reason()
    {
        _noYear = true;
        var model = Model();

        await model.LoadAsync();

        Assert.True(model.NoActiveYear);
        Assert.Empty(_notifications.Published); // said on the screen, not as an error message
        Assert.False(model.NewStudent.IsAvailable);
        Assert.Contains("curs actiu", model.NewStudent.UnavailableReason, StringComparison.Ordinal);
        model.Students.State.Actions.Single().Command.Execute(null);
        Assert.Equal(1, _courseOpened);
    }

    // --- Adding ---

    [Fact]
    [Trait("spec", Spec + ": Alta manual de un alumno (Alta correcta)")]
    public async Task Adding_a_student_creates_them_lists_them_and_chooses_them()
    {
        var model = Model();
        await model.LoadAsync();
        await model.NewStudentAsync();
        var form = (FormViewModel<StudentChangeResult>)_forms.Last;
        form["FirstName"].Text = "Marta";
        form["LastName"].Text = "Puig";
        form["Email"].Text = "marta@example.com";
        form["Level"].Text = "1r ESO";
        form["Group"].Text = "A";

        await form.Save.RunAsync();

        Assert.Contains("add Marta Puig 1r ESO A False", _calls);
        Assert.Equal("Puig", model.Students.Current!.LastName);
        Assert.Single(_notifications.Published);
    }

    [Fact]
    [Trait("spec", Spec + ": Alta manual de un alumno (Datos obligatorios, Correo obligatorio y único)")]
    public async Task A_missing_name_and_a_repeated_email_are_marked_in_their_own_field_without_losing_what_was_written()
    {
        var model = Model();
        await model.NewStudentAsync();
        var form = (FormViewModel<StudentChangeResult>)_forms.Last;
        form["LastName"].Text = "Puig";
        form["Email"].Text = "repe@example.com";

        await form.Save.RunAsync();
        Assert.True(form["FirstName"].HasError);
        Assert.Equal("Puig", form["LastName"].Text);

        form["FirstName"].Text = "Marta";
        await form.Save.RunAsync();
        Assert.True(form["Email"].HasError);
        Assert.False(form["FirstName"].HasError);
    }

    [Fact]
    [Trait("spec", Spec + ": Alta manual de un alumno (Valor nuevo de nivel o grupo)")]
    public async Task A_new_group_is_confirmed_before_it_joins_the_catalogue()
    {
        var model = Model();
        await model.NewStudentAsync();
        var form = (FormViewModel<StudentChangeResult>)_forms.Last;
        form["FirstName"].Text = "Marta";
        form["LastName"].Text = "Puig";
        form["Email"].Text = "marta@example.com";
        form["Level"].Text = "1r ESO";
        form["Group"].Text = "Z";

        await form.Save.RunAsync();

        Assert.Contains("Z", _confirmations.Asked.Single().Details!.Single(), StringComparison.Ordinal);
        Assert.Contains("add Marta Puig 1r ESO Z True", _calls);
    }

    [Fact]
    [Trait("spec", Spec + ": Alta manual de un alumno (Posible duplicado)")]
    public async Task A_namesake_is_warned_about_and_only_added_when_the_person_confirms_it_is_someone_else()
    {
        AddStudent("Marta", "Puig", "2n ESO", "B");
        _confirmations = new RecordingConfirmations(false);
        var model = Model();
        await model.LoadAsync();
        await model.NewStudentAsync();
        var form = (FormViewModel<StudentChangeResult>)_forms.Last;
        form["FirstName"].Text = "marta";
        form["LastName"].Text = "PUIG";
        form["Email"].Text = "altra@example.com";
        form["Level"].Text = "1r ESO";

        await form.Save.RunAsync();

        Assert.Contains("2n ESO", _confirmations.Asked.Single().Consequence, StringComparison.Ordinal);
        Assert.DoesNotContain(_calls, c => c.StartsWith("add", StringComparison.Ordinal));
        Assert.Empty(_notifications.Published);
    }

    // --- The record ---

    [Fact]
    [Trait("spec", Spec + ": Asignar una taquilla desde el alumno; Cambiar de taquilla y liberar")]
    public async Task The_actions_of_a_student_are_enabled_or_disabled_with_the_reason_application_gives()
    {
        var without = AddStudent("Pau", "Alsina");
        var with = AddStudent("Marta", "Puig", locker: 5);
        var model = Model();
        await model.LoadAsync();

        await model.Detail.ShowAsync(true, without.Id);
        Assert.True(model.Detail.Actions.Single(a => a.Id == "Assign").IsAvailable);
        Assert.False(model.Detail.Actions.Single(a => a.Id == "Change").IsAvailable);
        Assert.False(model.Detail.Actions.Single(a => a.Id == "Release").IsAvailable);

        await model.Detail.ShowAsync(true, with.Id);
        var assign = model.Detail.Actions.Single(a => a.Id == "Assign");
        Assert.False(assign.IsAvailable);
        Assert.Equal(_localizer.Message(new Error("Assignments.StudentHasLocker")), assign.UnavailableReason);
        Assert.True(model.Detail.Actions.Single(a => a.Id == "Release").IsAvailable);
    }

    [Fact]
    [Trait("spec", Spec + ": Ficha del alumno (Pestaña Historial)")]
    public async Task The_history_and_the_lockers_load_only_when_their_tab_is_opened()
    {
        var student = AddStudent("Marta", "Puig", locker: 5);
        var model = Model();
        await model.LoadAsync();
        await model.Detail.ShowAsync(true, student.Id);
        Assert.False(model.Detail.HistoryLoaded);
        Assert.False(model.LockerLinesLoaded);

        model.Students.Select(model.Students.List.Rows[0]);
        await Task.Delay(50);
        await model.LoadLockerLinesAsync();
        await model.Detail.LoadHistoryAsync();

        Assert.Equal(["Taquilla 5"], model.LockerLines);
        Assert.Equal(["alta"], model.Detail.History);
    }

    [Fact]
    [Trait("spec", Spec + ": Editar los datos del alumno (Corregir un apellido, Cambio de grupo)")]
    public async Task Editing_calls_only_what_changed_and_a_new_group_goes_through_the_enrolment()
    {
        var student = AddStudent("Marta", "Puig");
        var model = Model();
        await model.LoadAsync();
        await model.Detail.ShowAsync(true, student.Id);

        model.Detail.Actions.Single(a => a.Id == "Edit").Execute(null);
        await Task.Delay(100);
        var form = (FormViewModel<StudentDetail>)_forms.Last;
        Assert.Equal("marta@example.com", form["Email"].Text);
        form["LastName"].Text = "Puig i Soler";
        await form.Save.RunAsync();
        Assert.Contains("edit Marta Puig i Soler marta@example.com", _calls);
        Assert.DoesNotContain(_calls, c => c.StartsWith("enrollment", StringComparison.Ordinal));

        form["Group"].Text = "B";
        await form.Save.RunAsync();
        Assert.Contains("enrollment 1r ESO B False", _calls);
    }

    [Fact]
    [Trait("spec", Spec + ": Dar de baja y reactivar (Baja con taquilla, Baja sin motivo)")]
    public async Task Retiring_names_the_locker_that_will_be_freed_and_will_not_go_without_a_reason()
    {
        var student = AddStudent("Marta", "Puig", locker: 5);
        var model = Model();
        await model.LoadAsync();
        await model.Detail.ShowAsync(true, student.Id);

        model.Detail.Actions.Single(a => a.Id == "Retire").Execute(null);
        await Task.Delay(100);
        var form = (FormViewModel<StudentDetail>)_forms.Last;
        Assert.Contains("5", form.Note, StringComparison.Ordinal);

        await form.Save.RunAsync();
        Assert.True(form["Reason"].HasError);
        Assert.DoesNotContain(_calls, c => c.StartsWith("retire", StringComparison.Ordinal));

        form["Reason"].Text = "Ha marxat";
        await form.Save.RunAsync();
        Assert.Contains("retire Ha marxat", _calls);
    }

    [Fact]
    [Trait("spec", Spec + ": Dar de baja y reactivar (Reactivar)")]
    public async Task A_retired_student_can_be_reactivated_and_an_active_one_says_why_not()
    {
        var retired = AddStudent("Oriol", "Zamora", retired: true);
        var active = AddStudent("Pau", "Alsina");
        var model = Model();
        await model.LoadAsync();

        await model.Detail.ShowAsync(true, active.Id);
        Assert.False(model.Detail.Actions.Single(a => a.Id == "Reactivate").IsAvailable);

        await model.Detail.ShowAsync(true, retired.Id);
        model.Detail.Actions.Single(a => a.Id == "Reactivate").Execute(null);
        await Task.Delay(100);
        await ((FormViewModel<StudentChangeResult>)_forms.Last).Save.RunAsync();

        Assert.Contains("reactivate 1r ESO", _calls);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambiar de taquilla y liberar (Liberar)")]
    public async Task Releasing_asks_first_and_only_releases_when_confirmed()
    {
        var student = AddStudent("Marta", "Puig", locker: 5);
        _confirmations = new RecordingConfirmations(false);
        var model = Model();
        await model.LoadAsync();
        await model.Detail.ShowAsync(true, student.Id);

        model.Detail.Actions.Single(a => a.Id == "Release").Execute(null);
        await Task.Delay(100);
        Assert.Contains("5", _confirmations.Asked.Single().Consequence, StringComparison.Ordinal);
        Assert.DoesNotContain("release", _calls);

        _confirmations = new RecordingConfirmations(true);
        var again = Model();
        await again.LoadAsync();
        await again.Detail.ShowAsync(true, student.Id);
        again.Detail.Actions.Single(a => a.Id == "Release").Execute(null);
        await Task.Delay(100);
        Assert.Contains("release", _calls);
    }

    // --- The selectors of assignment ---

    [Fact]
    [Trait("spec", Spec + ": Asignar una taquilla desde el alumno (Sugerencia)")]
    public async Task The_locker_selector_proposes_the_lowest_free_one_of_the_zone_and_changing_zone_proposes_again()
    {
        AddLocker(9, "Planta 1");
        AddLocker(5, "Planta 1");
        AddLocker(2, "Planta 2");
        var dialogs = new AssignmentDialogs(Pickers(), Context());

        await dialogs.ChooseLockerForAsync(Guid.NewGuid(), "Marta Puig", isChange: false);
        var form = (FormViewModel<AssignLockerResult>)_forms.Last;

        Assert.Equal(2, form["Zone"].Options!.Count);
        Assert.Equal(_lockers.Single(l => l.Number == 5).Id.ToString(), form["Locker"].Text);
        Assert.Equal(2, form["Locker"].Options!.Count); // and the other free one can be picked

        form["Zone"].Text = ZoneId("Planta 2").ToString();
        Assert.Equal(_lockers.Single(l => l.Number == 2).Id.ToString(), form["Locker"].Text);
    }

    [Fact]
    [Trait("spec", Spec + ": Asignar una taquilla desde el alumno (Sin taquillas libres)")]
    public async Task With_no_free_locker_the_selector_explains_it_and_offers_nothing_to_confirm()
    {
        AddLocker(1, "Planta 1", LockerStatusView.Occupied);
        var dialogs = new AssignmentDialogs(Pickers(), Context());

        await dialogs.ChooseLockerForAsync(Guid.NewGuid(), "Marta Puig", isChange: false);

        Assert.Empty(_forms.Shown);
        var asked = Assert.Single(_choices.Asked);
        Assert.Empty(asked.Options);
        Assert.Contains("lliure", asked.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Avisos e impedimentos al asignar (Aviso de deuda, Impedimento)")]
    public async Task An_impediment_is_shown_and_nothing_is_assigned_and_a_warning_needs_explicit_confirmation()
    {
        var locker = AddLocker(5, "Planta 1");
        var dialogs = new AssignmentDialogs(Pickers(), Context());
        _check = new AssignmentTargetCheck(new Error("Assignments.StudentHasLocker"), []);
        await dialogs.ChooseLockerForAsync(Guid.NewGuid(), "Marta Puig", isChange: false);
        var form = (FormViewModel<AssignLockerResult>)_forms.Last;
        await Task.Delay(50);
        Assert.Equal(_localizer.Message(new Error("Assignments.StudentHasLocker")), form.Note);

        _check = new AssignmentTargetCheck(null, [new Notice("Charges.PriorDebt", [2, 70m])]);
        _assignFirst = Result<AssignLockerResult>.Success(new AssignLockerResult(null, [new Notice("Charges.PriorDebt", [2, 70m])]));
        _confirmations = new RecordingConfirmations(false);
        var declining = new AssignmentDialogs(Pickers(), Context());
        await declining.ChooseLockerForAsync(Guid.NewGuid(), "Marta Puig", isChange: false);
        await ((FormViewModel<AssignLockerResult>)_forms.Last).Save.RunAsync();
        var details = _confirmations.Asked.Single().Details!;
        Assert.Contains("Càrrecs pendents", details[0], StringComparison.Ordinal);
        Assert.Contains("Quota del curs 2025-2026", details[1], StringComparison.Ordinal); // the breakdown of the debt by concept and year
        Assert.DoesNotContain($"assign {locker.Id} True", _calls);
        Assert.Empty(_notifications.Published); // declining is not an error

        _confirmations = new RecordingConfirmations(true);
        var confirmed = new AssignmentDialogs(Pickers(), Context());
        await confirmed.ChooseLockerForAsync(Guid.NewGuid(), "Marta Puig", isChange: false);
        await ((FormViewModel<AssignLockerResult>)_forms.Last).Save.RunAsync();
        Assert.Contains($"assign {locker.Id} True", _calls);
    }

    [Fact]
    [Trait("spec", Spec + ": Feedback y doble ejecución en Alumnos (Doble clic en asignar)")]
    public async Task Confirming_the_assignment_twice_at_once_assigns_once_and_notifies_once()
    {
        AddLocker(5, "Planta 1");
        var dialogs = new AssignmentDialogs(Pickers(), Context());
        await dialogs.ChooseLockerForAsync(Guid.NewGuid(), "Marta Puig", isChange: false);
        var form = (FormViewModel<AssignLockerResult>)_forms.Last;

        var first = form.Save.RunAsync();
        await form.Save.RunAsync();
        await first;

        Assert.Single(_calls, c => c.StartsWith("assign", StringComparison.Ordinal));
        Assert.Single(_notifications.Published);
    }

    [Fact]
    [Trait("spec", Spec + ": Asignar desde la taquilla y arrastrando (Desde la taquilla)")]
    public async Task The_student_selector_lists_the_students_without_a_locker_narrows_with_the_search_and_assigns()
    {
        AddStudent("Marta", "Puig");
        AddStudent("Pau", "Alsina");
        AddStudent("Núria", "Roca", locker: 3);
        var locker = AddLocker(5, "Planta 1");
        var dialogs = new AssignmentDialogs(Pickers(), Context());

        await dialogs.ChooseStudentForAsync(locker.Id, 5, "Planta 1");
        var form = (FormViewModel<AssignLockerResult>)_forms.Last;
        Assert.Equal(2, form["Student"].Options!.Count); // the one with a locker is not offered

        form["Search"].Text = "alsi";
        Assert.Single(form["Student"].Options!);
        await form.Save.RunAsync();

        Assert.Contains($"assign {locker.Id} False", _calls);
    }

    [Avalonia.Headless.XUnit.AvaloniaFact]
    public async Task Screenshot_of_the_students_section()
    {
        AddStudent("Marta", "Puig", locker: 5, debt: true);
        AddStudent("Pau", "Alsina");
        AddStudent("Núria", "García", "2n ESO", "B", locker: 12);
        var model = Model();
        var screen = StudentsView.Create(model, _localizer);
        var window = new Avalonia.Controls.Window { Content = screen, Width = 1200, Height = 700 };
        window.Show();
        await model.LoadAsync();
        model.Students.Select(model.Students.List.Rows[2]);
        await Task.Delay(200);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        ScreenshotTests.Take(window, "students");
        window.Close();
    }
}
