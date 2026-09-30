// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using Arca.Application.Assignments;
using Arca.Application.Home;
using Arca.Application.Catalog.ListCatalog;
using Arca.Application.Feedback;
using Arca.Application.Students;
using Arca.Application.Students.AddStudent;
using Arca.Application.Students.ChangeStudentEnrollment;
using Arca.Application.Students.EditStudent;
using Arca.Application.Students.GetStudentScreen;
using Arca.Application.Students.ListStudentRows;
using Arca.Application.Students.ReactivateStudent;
using Arca.Domain.Common;
using Arca.UI.Actions;
using Arca.UI.Assigning;
using Arca.UI.Charges;
using Arca.UI.Commands;
using Arca.UI.Common;
using Arca.UI.Lists;
using Arca.UI.Screens;

namespace Arca.UI.Students;

/// <summary>
/// The Students section (pantalles-de-domini, pantalles-alumnes-i-assignacions): the list of students of the active year with its
/// search and filters, the record of the one chosen (data, locker, history) with the actions that apply to them, and the forms to
/// add, edit, retire and reactivate. It has no window and no rule of its own: what can be done, and why not, is what Application
/// answers, and what a form is refused for comes back as the error of its field.
/// </summary>
public sealed class StudentsViewModel : ObservableObject
{
    readonly StudentServices _services;
    readonly Arca.UI.Home.HomeCardServices? _cards;
    readonly ScreenContext _context;
    readonly AssignmentDialogs _assign;
    readonly StudentResultTexts _texts;
    readonly OneAtATime _once = new();
    readonly Func<Task> _openCourse;
    StudentCounters _counters = new(0, 0, 0);
    StudentEmptyState _emptyState;
    CatalogListing _catalog = new([], []);
    bool _noActiveYear;
    string _levelFilter = string.Empty;
    string _groupFilter = string.Empty;
    string _lockerFilter = string.Empty;
    string _paymentFilter = string.Empty;
    bool _includeRetired;
    IReadOnlyList<StudentListRow> _all = [];

    public StudentsViewModel(
        StudentServices services, ScreenContext context, AssignmentDialogs assign, AppAction standardNew, Func<Task> openCourse, StudentChargesViewModel charges,
        Arca.UI.Home.HomeCardServices? cards = null)
    {
        _cards = cards;
        Charges = charges;
        charges.Changed += (_, _) => _ = RefreshAfterChargesAsync();
        _services = services;
        _context = context;
        _assign = assign;
        _openCourse = openCourse;
        _texts = new StudentResultTexts(context.Localizer);
        StandardNew = standardNew;
        var text = context.Localizer;
        NewStudent = new AppAction("NewStudent", text.Get("Students.Action.New"), standardNew.Shortcut, standardNew.ShortcutText);
        NewStudent.Attach(() => _ = NewStudentAsync(), () => NoActiveYear ? Availability.Unavailable(text.Get("Students.Reason.NoYear")) : Availability.Available);
        Students = new ScreenListViewModel<StudentListRow, Guid>(
            [
                new ListColumn<StudentListRow>("last", text.Get("Students.Label.LastName"), s => s.LastName, Width: 3),
                new ListColumn<StudentListRow>("first", text.Get("Students.Label.FirstName"), s => s.FirstName, s => s.FirstName, Width: 2),
                new ListColumn<StudentListRow>("level", text.Get("Students.Label.Level"), s => s.LevelName ?? string.Empty, Width: 2),
                new ListColumn<StudentListRow>("group", text.Get("Students.Label.Group"), s => s.GroupName ?? string.Empty, Width: 1),
                new ListColumn<StudentListRow>("locker", text.Get("Students.Label.Locker"), s => s.LockerNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, s => s.LockerNumber ?? int.MaxValue, Width: 1),
                new ListColumn<StudentListRow>("payment", text.Get("Students.Label.Payment"), s => PaymentOf(s), s => s.PendingTotal, Width: 3),
                new ListColumn<StudentListRow>("state", text.Get("Students.Label.State"), s => text.Get(s.IsRetired ? "Students.State.Retired" : "Students.State.Active"), Width: 1),
            ],
            s => s.Id, LoadRowsAsync, text, context.Notifications, context.Log,
            () => NoActiveYear ? text.Get("Students.Empty.NoYear")
                : _emptyState == StudentEmptyState.NoStudents ? text.Get("Students.Empty.NoStudentsList") : StudentEmptyStates.Describe(_emptyState, text)?.Message, emptyActions: EmptyActions);
        Students.List.SortBy("last");
        SaveAsCard = new AppAction("SaveAsCard", text.Get("Shell.Home.Action.SaveAsCard"));
        SaveAsCard.Attach(() => _ = SaveCardAsync(), () => CurrentCardCriteria.Count > 0 ? Availability.Available : Availability.Unavailable(text.Get("Shell.Home.Reason.NeedFilter")));
        Students.UseFilters(ActiveFilterTags, ResetFilterFields);
        Students.NoResultsMessage = () => PaymentFilter == "pending" && Students.List.FilterText.Length == 0 && LevelFilter.Length == 0 && GroupFilter.Length == 0 && LockerFilter.Length == 0
            ? text.Get("Students.Empty.NobodyPending") : null;
        ApplyFilters();
        Detail = new DetailViewModel<Guid, StudentScreenDetail>(LoadDetailAsync, BuildActions, text, context.Notifications, context.Log, services.History);
        Students.CurrentChanged += (_, _) => _ = ShowCurrentAsync();
    }

    public ScreenListViewModel<StudentListRow, Guid> Students { get; }

    public DetailViewModel<Guid, StudentScreenDetail> Detail { get; }

    /// <summary>The action of the student form, with the shortcut of the standard New.</summary>
    public AppAction NewStudent { get; }

    public AppAction StandardNew { get; }

    /// <summary>The charges of the student chosen, shown in the record as the pending ones and the history of payments.</summary>
    public StudentChargesViewModel Charges { get; }

    async Task RefreshAfterChargesAsync()
    {
        if (Students.TryGetSelectedKey(out var id))
        {
            await RefreshAsync(id); // the header of the record shows the state of payment
        }
    }


    public IReadOnlyList<AppAction> MainActions => _cards is null ? [NewStudent] : [NewStudent, SaveAsCard];

    /// <summary>True while no school year is active: the students cannot be listed, added or assigned, and the screen says how to fix it.</summary>
    public bool NoActiveYear
    {
        get => _noActiveYear;
        private set
        {
            if (Set(ref _noActiveYear, value))
            {
                NewStudent.Refresh();
                Raise(nameof(NoYearNotice));
            }
        }
    }

    public string NoYearNotice => NoActiveYear ? _context.Localizer.Get("Students.Empty.NoYear") : string.Empty;

    /// <summary>Takes the person to the Course section, where a year is activated.</summary>
    public Task OpenCourseAsync() => _openCourse();

    // --- Filters ---

    public IReadOnlyList<FormOption> LevelOptions => [new(string.Empty, _context.Localizer.Get("Students.Label.AllLevels")), .. _catalog.Levels.Select(l => new FormOption(l, l))];

    public IReadOnlyList<FormOption> GroupOptions => [new(string.Empty, _context.Localizer.Get("Students.Label.AllGroups")), .. _catalog.Groups.Select(g => new FormOption(g, g))];

    public IReadOnlyList<FormOption> LockerOptions =>
    [
        new(string.Empty, _context.Localizer.Get("Students.Label.AnyLocker")),
        new("with", _context.Localizer.Get("Students.Label.WithLocker")),
        new("without", _context.Localizer.Get("Students.Label.WithoutLocker")),
    ];

    public IReadOnlyList<FormOption> PaymentOptions =>
    [
        new(string.Empty, _context.Localizer.Get("Students.Label.AnyPayment")),
        new("pending", _context.Localizer.Get("Students.Label.WithPendingPayments")),
        new("upToDate", _context.Localizer.Get("Students.Label.PaymentsUpToDate")),
    ];

    /// <summary>"pending" for the students with something to pay, "upToDate" for those with nothing, or empty for any.</summary>
    public string PaymentFilter
    {
        get => _paymentFilter;
        set
        {
            if (Set(ref _paymentFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public string LevelFilter
    {
        get => _levelFilter;
        set
        {
            if (Set(ref _levelFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public string GroupFilter
    {
        get => _groupFilter;
        set
        {
            if (Set(ref _groupFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    /// <summary>"with", "without" or empty for any: the state of assignment.</summary>
    public string LockerFilter
    {
        get => _lockerFilter;
        set
        {
            if (Set(ref _lockerFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public bool IncludeRetired
    {
        get => _includeRetired;
        set
        {
            if (Set(ref _includeRetired, value))
            {
                ApplyFilters();
            }
        }
    }

    /// <summary>How many active students there are and how many have a locker, whatever the filters hide.</summary>
    public string CountersText => _context.Localizer.Get("Students.Label.Counters", _counters.Active, _counters.WithLocker, _counters.WithoutLocker);

    void ApplyFilters()
    {
        var filter = CurrentFilter;
        Students.List.SetPredicate(filter.Matches);
        Students.RefreshFilters();
        SaveAsCard?.Refresh();
    }

    /// <summary>The filters that are on as the one rule that also counts the cards of the start screen.</summary>
    StudentCardFilter CurrentFilter => new(
        NullIfEmpty(LockerFilter), NullIfEmpty(PaymentFilter), NullIfEmpty(LevelFilter), NullIfEmpty(GroupFilter), IncludeRetired);

    /// <summary>
    /// What the filters on come to as the criteria of a card (targetes-d-inici), or nothing when none is on: the inverse of
    /// <see cref="ApplyRequest"/>, which is what lets a screen already filtered be saved as a card.
    /// </summary>
    public IReadOnlyDictionary<string, string> CurrentCardCriteria => CurrentFilter.ToCriteria();

    /// <summary>Saves the filters that are on as a card of the start screen (targetes-d-inici): the form asks only for the title.</summary>
    public AppAction SaveAsCard { get; private set; } = null!;

    async Task SaveCardAsync()
    {
        if (_cards is null)
        {
            return;
        }

        var text = _context.Localizer;
        var ids = new[] { "level", "group", "locker", "payment", "retired" };
        var summary = string.Join(" · ", Students.ActiveFilters.Where(t => ids.Contains(t.Id)).Select(t => t.Text));
        await _context.Forms.ShowAsync(Arca.UI.Home.HomeCardForms.FromScreen(
            _context, _cards, Arca.Application.Home.HomeCardTargetView.Students, CurrentCardCriteria, summary,
            Arca.UI.Home.HomeCardForms.SuggestedTitle(text.Get("Shell.Section.Students"), summary)));
    }

    static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;

    /// <summary>The filters that are on, each with what removes it, for the labels under the search.</summary>
    IReadOnlyList<ListFilterTag> ActiveFilterTags()
    {
        var text = _context.Localizer;
        var tags = new List<ListFilterTag>();
        if (LevelFilter.Length > 0)
        {
            tags.Add(new ListFilterTag("level", text.Get("Common.Label.FilterTag", text.Get("Students.Label.Level"), LevelFilter), () => LevelFilter = string.Empty));
        }

        if (GroupFilter.Length > 0)
        {
            tags.Add(new ListFilterTag("group", text.Get("Common.Label.FilterTag", text.Get("Students.Label.Group"), GroupFilter), () => GroupFilter = string.Empty));
        }

        if (LockerFilter.Length > 0)
        {
            var label = LockerOptions.FirstOrDefault(o => o.Id == LockerFilter)?.Label ?? LockerFilter;
            tags.Add(new ListFilterTag("locker", label, () => LockerFilter = string.Empty));
        }

        if (PaymentFilter.Length > 0)
        {
            tags.Add(new ListFilterTag("payment", PaymentOptions.First(o => o.Id == PaymentFilter).Label, () => PaymentFilter = string.Empty));
        }

        if (IncludeRetired)
        {
            tags.Add(new ListFilterTag("retired", text.Get("Students.Label.IncludeRetired"), () => IncludeRetired = false));
        }

        return tags;
    }

    /// <summary>Removes the search and every filter, which is what the state of a list without results and the Reset button offer.</summary>
    public void ClearFilters() => Students.ClearFilter();

    /// <summary>
    /// Puts on the filters a card of the start screen asks for (Locker=with|without, Payment=pending|upToDate), after taking off the search
    /// and every other filter, so the list shows exactly that and nothing else.
    /// </summary>
    public void ApplyRequest(IReadOnlyDictionary<string, string> filters)
    {
        Students.ClearFilter();
        if (filters.TryGetValue("Locker", out var locker))
        {
            LockerFilter = locker;
        }

        if (filters.TryGetValue("Payment", out var payment))
        {
            PaymentFilter = payment;
        }

        if (filters.TryGetValue("Level", out var level))
        {
            LevelFilter = level;
        }

        if (filters.TryGetValue("Group", out var group))
        {
            GroupFilter = group;
        }

        IncludeRetired = filters.TryGetValue("IncludeRetired", out var retired) && retired == "true";
    }

    void ResetFilterFields()
    {
        _levelFilter = _groupFilter = _lockerFilter = _paymentFilter = string.Empty;
        _includeRetired = false;
        Raise(nameof(PaymentFilter));
        Raise(nameof(LevelFilter));
        Raise(nameof(GroupFilter));
        Raise(nameof(LockerFilter));
        Raise(nameof(IncludeRetired));
        ApplyFilters();
    }

    string PaymentOf(StudentListRow s) => _context.Localizer.Get(s.HasDebt ? "Students.Label.PaymentDebt" : "Students.Label.PaymentUpToDate"); // never an amount: that is in the record

    // --- Loading ---

    async Task<Result<IReadOnlyList<StudentListRow>>> LoadRowsAsync(CancellationToken ct)
    {
        var catalog = await _services.ListCatalog(ct);
        if (catalog.IsSuccess)
        {
            _catalog = catalog.Value!;
            Raise(nameof(LevelOptions));
            Raise(nameof(GroupOptions));
        }

        var listing = await _services.ListStudents(ct);
        if (!listing.IsSuccess && listing.Error!.Code == "SchoolYears.NoActiveYear")
        {
            NoActiveYear = true; // said on the screen with a way to fix it, not as an error message
            _counters = new StudentCounters(0, 0, 0);
            Raise(nameof(CountersText));
            return Result<IReadOnlyList<StudentListRow>>.Success([]);
        }

        NoActiveYear = false;
        if (!listing.IsSuccess)
        {
            return Result<IReadOnlyList<StudentListRow>>.Failure(listing.Error!);
        }

        _counters = listing.Value!.Counters;
        _all = listing.Value.Rows;
        _emptyState = listing.Value.EmptyState;
        Raise(nameof(CountersText));
        return Result<IReadOnlyList<StudentListRow>>.Success(listing.Value.Rows);
    }

    IReadOnlyList<EmptyStateAction> EmptyActions()
    {
        var text = _context.Localizer;
        if (NoActiveYear)
        {
            var toCourse = new AppAction("GoToCourse", text.Get("Students.Action.GoToCourse"));
            toCourse.Attach(() => _ = _openCourse());
            return [new(toCourse.Label, toCourse)];
        }

        return _emptyState == StudentEmptyState.NoStudents ? [new(NewStudent.Label, NewStudent)] : [];
    }

    public async Task LoadAsync(CancellationToken ct = default) => await Students.LoadAsync(ct);

    async Task<Result<StudentScreenDetail?>> LoadDetailAsync(Guid id, CancellationToken ct)
    {
        var detail = await _services.Detail(id, ct);
        return detail.IsSuccess ? Result<StudentScreenDetail?>.Success(detail.Value) : Result<StudentScreenDetail?>.Failure(detail.Error!);
    }

    async Task ShowCurrentAsync()
    {
        var has = Students.TryGetSelectedKey(out var id);
        var charges = Charges.ShowAsync(has ? id : null); // the pending charges are open in the record, so they load with it
        await Detail.ShowAsync(has, id);
        await charges;
    }

    async Task RefreshAsync(Guid? select = null)
    {
        await LoadAsync();
        if (select is { } id && Students.List.Rows.FirstOrDefault(s => s.Id == id) is { } row)
        {
            Students.Select(row);
        }

        if (Students.TryGetSelectedKey(out _))
        {
            await ShowCurrentAsync();
        }

        await _context.AfterWrite();
    }

    // --- Actions of the record ---

    IReadOnlyList<AppAction> BuildActions(StudentScreenDetail detail)
    {
        var actions = new ActionSet(_context.Localizer);
        var student = detail.Student;
        var name = student.FirstName + " " + student.LastName;
        actions.Add("Edit", "Students.Action.Edit", () => _ = EditAsync(detail));
        actions.Add("Assign", "Students.Action.Assign", () => _ = ChooseLockerAsync(student.Id, name, isChange: false), () => detail.AssignBlocked);
        actions.Add("Change", "Students.Action.Change", () => _ = ChooseLockerAsync(student.Id, name, isChange: true), () => detail.ChangeBlocked);
        actions.Add("Release", "Students.Action.Release", () => _ = ReleaseAsync(detail), () => detail.ReleaseBlocked);
        actions.Add("Retire", "Students.Action.Retire", () => _ = RetireAsync(detail), () => detail.RetireBlocked);
        actions.Add("Reactivate", "Students.Action.Reactivate", () => _ = ReactivateAsync(detail), () => detail.ReactivateBlocked);
        return actions.Actions;
    }

    async Task ChooseLockerAsync(Guid studentId, string name, bool isChange)
    {
        await _assign.ChooseLockerForAsync(studentId, name, isChange);
        await RefreshAsync(studentId); // the record shows the locker the student has now
    }

    async Task ReleaseAsync(StudentScreenDetail detail)
    {
        var student = detail.Student;
        var request = new AssignmentConfirmations(_context.Localizer).ForRelease(student.FirstName + " " + student.LastName, student.LockerNumber ?? 0);
        if (!await _context.Confirmations.ConfirmAsync(request))
        {
            return;
        }

        await _once.RunAsync("Release", () => new RunOnceCommand<string>(
            (ct, _) => _services.Release(student.Id, ct), sentence => sentence, "ReleaseStudentLocker", _context.Notifications, _context.Localizer,
            _context.Log, _context.Delay, () => RefreshAsync(student.Id)).RunAsync());
    }

    // --- Forms ---

    static string? FieldOf(Error error) => error.Code switch
    {
        "Students.FirstNameRequired" => "FirstName",
        "Students.LastNameRequired" => "LastName",
        "Students.EmailInvalid" or "Students.EmailInUse" => "Email",
        "Students.ReasonRequired" or "Students.ReasonTooLong" => "Reason",
        "Enrollments.LevelRequired" or "Catalog.NameRequired" or "Catalog.NameTooLong" => "Level",
        "Enrollments.GroupNotInLevel" => "Group",
        _ => null,
    };

    string CatalogNote() => _context.Localizer.Get("Students.Note.Catalog", string.Join(", ", _catalog.Levels), string.Join(", ", _catalog.Groups));

    /// <summary>Asks the person to confirm the new levels or groups a save would add to the catalogue. False when they decline.</summary>
    async Task<bool> ConfirmNewValuesAsync(IReadOnlyList<NewCatalogValue> values, CancellationToken ct)
    {
        var text = _context.Localizer;
        var request = new ConfirmationRequest(
            text.Get("Students.Label.NewValuesTitle"), text.Get("Students.Label.NewValuesConsequence"), text.Get("Students.Label.NewValuesConfirm"),
            Details: [.. values.Select(v => text.Get(v.Kind == CatalogKind.Level ? "Students.Label.NewLevel" : "Students.Label.NewGroup", v.Name))]);
        return await _context.Confirmations.ConfirmAsync(request, ct);
    }

    /// <summary>Opens the form of a new student. A namesake is warned about and the person confirms it is someone else; a new level or group is confirmed too.</summary>
    public async Task NewStudentAsync()
    {
        var text = _context.Localizer;
        var first = new FormFieldModel("FirstName", text.Get("Students.Label.FirstName"));
        var last = new FormFieldModel("LastName", text.Get("Students.Label.LastName"));
        var email = new FormFieldModel("Email", text.Get("Students.Label.Email"));
        var level = new FormFieldModel("Level", text.Get("Students.Label.Level"));
        var group = new FormFieldModel("Group", text.Get("Students.Label.Group"));
        StudentDetail? added = null;
        var form = new FormViewModel<StudentChangeResult>(
            [first, last, email, level, group],
            async ct =>
            {
                if (Namesake(first.Text, last.Text) is { } same && !await ConfirmNamesakeAsync(same, ct))
                {
                    return Result<StudentChangeResult>.Failure(FormViewModel<StudentChangeResult>.Cancelled);
                }

                var request = new AddStudentRequest(first.Text, last.Text, email.Text, level.Text, group.Text.Length == 0 ? null : group.Text);
                var result = await _services.Add(request, ct);
                if (result.IsSuccess && result.Value!.NeedsConfirmation)
                {
                    if (!await ConfirmNewValuesAsync(result.Value.NewValues, ct))
                    {
                        return Result<StudentChangeResult>.Failure(FormViewModel<StudentChangeResult>.Cancelled);
                    }

                    result = await _services.Add(request with { ConfirmNewValues = true }, ct);
                }

                return result;
            },
            FieldOf, done => _texts.Added(done.Student!), "AddStudent", _context.Notifications, text, _context.Log, _context.Delay,
            text.Get("Students.Action.New"), text.Get("Common.Action.Save"), done => added = done.Student, () => RefreshAsync(added?.Id));
        form.Note = CatalogNote();
        await _context.Forms.ShowAsync(form);
    }

    StudentListRow? Namesake(string first, string last)
    {
        var key = TextComparer.Key(first + " " + last);
        return key.Trim().Length == 0 ? null : _all.FirstOrDefault(s => TextComparer.Key(s.FirstName + " " + s.LastName) == key);
    }

    async Task<bool> ConfirmNamesakeAsync(StudentListRow existing, CancellationToken ct)
    {
        var text = _context.Localizer;
        var request = new ConfirmationRequest(
            text.Get("Students.Label.NamesakeTitle"),
            text.Get("Students.Label.NamesakeConsequence", existing.FirstName + " " + existing.LastName, existing.LevelName ?? string.Empty, existing.GroupName ?? string.Empty),
            text.Get("Students.Label.NamesakeConfirm"));
        return await _context.Confirmations.ConfirmAsync(request, ct);
    }

    async Task EditAsync(StudentScreenDetail detail)
    {
        var text = _context.Localizer;
        var student = detail.Student;
        var first = new FormFieldModel("FirstName", text.Get("Students.Label.FirstName")) { Text = student.FirstName };
        var last = new FormFieldModel("LastName", text.Get("Students.Label.LastName")) { Text = student.LastName };
        var email = new FormFieldModel("Email", text.Get("Students.Label.Email")) { Text = student.Email };
        var level = new FormFieldModel("Level", text.Get("Students.Label.Level")) { Text = student.LevelName ?? string.Empty };
        var group = new FormFieldModel("Group", text.Get("Students.Label.Group")) { Text = student.GroupName ?? string.Empty };
        var form = new FormViewModel<StudentDetail>(
            [first, last, email, level, group],
            async ct =>
            {
                StudentDetail? latest = null;
                if (first.Text != student.FirstName || last.Text != student.LastName || email.Text != student.Email)
                {
                    var edited = await _services.Edit(new EditStudentRequest(student.Id, first.Text, last.Text, email.Text), ct);
                    if (!edited.IsSuccess)
                    {
                        return edited;
                    }

                    latest = edited.Value;
                }

                if (level.Text != (student.LevelName ?? string.Empty) || group.Text != (student.GroupName ?? string.Empty))
                {
                    var request = new ChangeStudentEnrollmentRequest(student.Id, level.Text, group.Text.Length == 0 ? null : group.Text);
                    var changed = await _services.ChangeEnrollment(request, ct);
                    if (changed.IsSuccess && changed.Value!.NeedsConfirmation)
                    {
                        if (!await ConfirmNewValuesAsync(changed.Value.NewValues, ct))
                        {
                            return Result<StudentDetail>.Failure(FormViewModel<StudentDetail>.Cancelled);
                        }

                        changed = await _services.ChangeEnrollment(request with { ConfirmNewValues = true }, ct);
                    }

                    if (!changed.IsSuccess)
                    {
                        return Result<StudentDetail>.Failure(changed.Error!);
                    }

                    latest = changed.Value!.Student;
                }

                return latest is null ? Result<StudentDetail>.Failure(new Error("Students.Unchanged")) : Result<StudentDetail>.Success(latest);
            },
            FieldOf, done => _texts.Edited(done), "EditStudent", _context.Notifications, text, _context.Log, _context.Delay,
            text.Get("Students.Action.Edit"), text.Get("Common.Action.Save"), null, () => RefreshAsync(student.Id));
        form.Note = CatalogNote();
        await _context.Forms.ShowAsync(form);
    }

    async Task RetireAsync(StudentScreenDetail detail)
    {
        var text = _context.Localizer;
        var student = detail.Student;
        var reason = new FormFieldModel("Reason", text.Get("Students.Label.Reason"));
        var form = new FormViewModel<StudentDetail>(
            [reason], ct => _services.Retire(student.Id, reason.Text, ct), FieldOf, done => _texts.Retired(done), "RetireStudent",
            _context.Notifications, text, _context.Log, _context.Delay, text.Get("Students.Action.Retire"), text.Get("Students.Action.Retire"),
            null, () => RefreshAsync(student.Id));
        form.Note = new StudentConfirmations(text).ForRetire(student).Consequence; // says which locker is freed
        await _context.Forms.ShowAsync(form);
    }

    async Task ReactivateAsync(StudentScreenDetail detail)
    {
        var text = _context.Localizer;
        var student = detail.Student;
        var level = new FormFieldModel("Level", text.Get("Students.Label.Level")) { Text = student.LevelName ?? string.Empty };
        var group = new FormFieldModel("Group", text.Get("Students.Label.Group")) { Text = student.GroupName ?? string.Empty };
        var form = new FormViewModel<StudentChangeResult>(
            [level, group],
            async ct =>
            {
                var request = new ReactivateStudentRequest(student.Id, level.Text, group.Text.Length == 0 ? null : group.Text);
                var result = await _services.Reactivate(request, ct);
                if (result.IsSuccess && result.Value!.NeedsConfirmation)
                {
                    if (!await ConfirmNewValuesAsync(result.Value.NewValues, ct))
                    {
                        return Result<StudentChangeResult>.Failure(FormViewModel<StudentChangeResult>.Cancelled);
                    }

                    result = await _services.Reactivate(request with { ConfirmNewValues = true }, ct);
                }

                return result;
            },
            FieldOf, done => _texts.Reactivated(done.Student!), "ReactivateStudent", _context.Notifications, text, _context.Log, _context.Delay,
            text.Get("Students.Action.Reactivate"), text.Get("Students.Action.Reactivate"), null, () => RefreshAsync(student.Id));
        form.Note = CatalogNote();
        await _context.Forms.ShowAsync(form);
    }
}
