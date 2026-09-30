// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using Arca.Application.Assignments;
using Arca.Application.Common;
using Arca.Application.Lockers;
using Arca.Application.Lockers.AddLocker;
using Arca.Application.Lockers.CreateLockerRange;
using Arca.Application.Lockers.GetLockerScreen;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Search;
using Arca.Application.Zones.ListZoneRows;
using Arca.Domain.Common;
using Arca.UI.Actions;
using Arca.UI.Assigning;
using Arca.UI.Commands;
using Arca.UI.Common;
using Arca.UI.Lists;
using Arca.UI.Screens;
using Arca.UI.Shell;

namespace Arca.UI.Lockers;

/// <summary>
/// The Lockers view of the Lockers section (pantalles-de-domini, pantalles-taquilles-i-zones): the list of lockers with its filters
/// and counters, the detail of the one chosen with the actions that apply to it and its history, and the forms to add, edit,
/// reserve and take out of service. It has no window and no rule of its own: what can be done, and why not, is what Application
/// answers, and what a form is refused for comes back as the error of its field.
/// </summary>
public sealed class LockersViewModel : ObservableObject
{
    readonly LockerServices _services;
    readonly ScreenContext _context;
    readonly AssignmentDialogs _assign;
    readonly OneAtATime _once = new();
    LockerCounters _counters = new(0, 0, 0, 0, 0, 0);
    LockerEmptyState _emptyState;
    IReadOnlyList<FormOption> _zoneOptions = [];
    string _zoneFilter = string.Empty;
    string _statusFilter = string.Empty;
    string _numberFilter = string.Empty;
    bool _includeRetired;
    Guid? _highlighted;
    (Guid StudentId, string StudentName)? _picking;
    Guid? _pickFrom;
    (Guid StudentId, Guid FromLocker)? _changing;

    public LockersViewModel(
        LockerServices services, ScreenContext context, AppAction standardNew, Func<Task> openNewZone, AssignmentDialogs assign, Func<Task>? openMap = null)
    {
        OpenMap = openMap;
        _services = services;
        _assign = assign;
        _context = context;
        OpenNewZone = openNewZone;
        StandardNew = standardNew;
        var text = context.Localizer;
        NewLocker = new AppAction("NewLocker", text.Get("Lockers.Action.New"), standardNew.Shortcut, standardNew.ShortcutText);
        NewLocker.Attach(() => _ = NewLockerAsync());
        NewRange = new AppAction("NewRange", text.Get("Lockers.Action.NewRange"));
        NewRange.Attach(() => _ = NewRangeAsync());
        Lockers = new ScreenListViewModel<LockerListRow, Guid>(
            [
                new ListColumn<LockerListRow>("number", text.Get("Lockers.Label.Number"), r => r.Number.ToString(CultureInfo.InvariantCulture), r => r.Number, Width: 1),
                new ListColumn<LockerListRow>("zone", text.Get("Lockers.Label.Zone"), r => r.ZoneName, Width: 2),
                new ListColumn<LockerListRow>("status", text.Get("Lockers.Label.Status"), r => LockerStatusPresentation.Text(r.Status, text), r => r.Status.ToString(), Width: 2),
                new ListColumn<LockerListRow>("student", text.Get("Lockers.Label.Student"), r => r.StudentName ?? string.Empty, Width: 3),
                new ListColumn<LockerListRow>("debt", text.Get("Lockers.Label.Debt"), r => r.HasDebt ? text.Get("Lockers.Label.HasDebt") : string.Empty, Width: 3),
                new ListColumn<LockerListRow>("note", text.Get("Lockers.Label.Note"), r => r.ReservationNote ?? r.Note ?? string.Empty, Width: 3),
            ],
            r => r.Id, LoadRowsAsync, text, context.Notifications, context.Log,
            () => LockerEmptyStates.Describe(_emptyState, text)?.Message, emptyActions: EmptyActions);
        Lockers.List.SortBy("number");
        Lockers.UseFilters(ActiveFilterTags, ResetFilterFields);
        ApplyFilters();
        Detail = new DetailViewModel<Guid, LockerScreenDetail>(LoadDetailAsync, BuildActions, text, context.Notifications, context.Log, services.History);
        Lockers.CurrentChanged += (_, _) =>
        {
            if (_picking is not null)
            {
                CancelPick(); // choosing something else means the person moved on from changing a locker
            }

            _ = ShowCurrentAsync();
        };
    }

    public ScreenListViewModel<LockerListRow, Guid> Lockers { get; }

    public DetailViewModel<Guid, LockerScreenDetail> Detail { get; }

    /// <summary>Takes the person to the map, where a locker is chosen for a student whose locker is being changed. Null when there is no map.</summary>
    public Func<Task>? OpenMap { get; }

    /// <summary>The student chosen in the panel of students without a locker, if the screen has one: Assign puts them in the locker.</summary>
    public Func<Guid?>? ChosenStudent { get; set; }

    /// <summary>Puts a student in a locker with the checks, warnings and feedback of every other way of assigning.</summary>
    public Func<AssignmentIntent, Task>? AssignStudent { get; set; }

    /// <summary>What is done when the locker for the change is chosen: the same request the student's record sends.</summary>
    public Func<AssignmentIntent, Task>? OnPicked { get; set; }

    /// <summary>Runs after anything that assigns or releases, so the panel of students reads again.</summary>
    public Func<Task>? AfterAssignmentChange { get; set; }

    /// <summary>The locker the search chose, outlined on the map, or null.</summary>
    public Guid? HighlightedLockerId
    {
        get => _highlighted;
        private set => Set(ref _highlighted, value);
    }

    /// <summary>The student whose locker is being changed while the person chooses the new one on the map, or null.</summary>
    public (Guid StudentId, string StudentName)? Picking => _picking;

    /// <summary>How many active lockers there are in each status, whatever the filters hide.</summary>
    public LockerCounters Counters => _counters;

    public LockerListRow? Find(Guid lockerId) => Lockers.List.AllRows.FirstOrDefault(r => r.Id == lockerId);

    /// <summary>Starts changing the locker of a student: the next free locker chosen on the map becomes theirs.</summary>
    public void BeginPick(Guid studentId, string studentName, Guid? fromLocker = null)
    {
        _picking = (studentId, studentName);
        _pickFrom = fromLocker;
        Raise(nameof(Picking));
    }

    public void CancelPick()
    {
        _picking = null;
        _pickFrom = null;
        Raise(nameof(Picking));
    }

    /// <summary>
    /// What choosing a locker on the map does: it chooses it, and while a locker is being changed a free one is taken as the new one
    /// and anything else is ignored.
    /// </summary>
    public void SelectLocker(Guid lockerId)
    {
        if (_picking is { } picking)
        {
            if (Find(lockerId) is { Status: LockerStatusView.Free })
            {
                _changing = _pickFrom is { } from ? (picking.StudentId, from) : null; // the old locker changes too, and will be read again
                CancelPick();
                _ = PickedAsync(new AssignmentIntent(picking.StudentId, lockerId));
            }

            return;
        }

        if (Lockers.List.Rows.FirstOrDefault(r => r.Id == lockerId) is { } row)
        {
            Lockers.Select(row);
        }
    }

    async Task PickedAsync(AssignmentIntent intent)
    {
        try
        {
            if (OnPicked is { } change)
            {
                await change(intent);
            }
        }
        catch (Exception e)
        {
            new Arca.UI.Notifications.ResultNotifier(_context.Notifications, _context.Localizer, _context.Log).Unexpected(e, "ChangeLocker");
        }
    }

    /// <summary>
    /// Shows a locker the search chose: what hides it is taken off, it is outlined and chosen, and its detail opens. A locker that is
    /// not here changes nothing.
    /// </summary>
    public void Reveal(Guid lockerId)
    {
        if (Lockers.List.AllRows.All(r => r.Id != lockerId))
        {
            return;
        }

        if (_picking is not null)
        {
            CancelPick(); // choosing something in the search means the person moved on from changing a locker
        }

        if (Lockers.List.Rows.All(r => r.Id != lockerId))
        {
            Lockers.ClearFilter();
        }

        HighlightedLockerId = lockerId;
        if (Lockers.List.Rows.FirstOrDefault(r => r.Id == lockerId) is { } row)
        {
            Lockers.Select(row);
        }
    }

    /// <summary>The action of the locker form, with the shortcut of the standard New.</summary>
    public AppAction NewLocker { get; }

    public AppAction NewRange { get; }

    public AppAction StandardNew { get; }

    /// <summary>The main actions of the view, in order.</summary>
    public IReadOnlyList<AppAction> MainActions => [NewLocker, NewRange];

    Func<Task> OpenNewZone { get; }

    // --- Filters and counters ---

    /// <summary>The zones the filter offers: the ones the lockers are in. The first choice is every zone.</summary>
    public IReadOnlyList<FormOption> ZoneOptions
    {
        get => _zoneOptions;
        private set => Set(ref _zoneOptions, value);
    }

    /// <summary>The statuses the filter offers, the first being every status.</summary>
    public IReadOnlyList<FormOption> StatusOptions =>
    [
        new(string.Empty, _context.Localizer.Get("Lockers.Label.AllStatuses")),
        .. new[] { LockerStatusView.Free, LockerStatusView.Occupied, LockerStatusView.Reserved, LockerStatusView.Broken, LockerStatusView.Maintenance }
            .Select(s => new FormOption(s.ToString(), LockerStatusPresentation.Text(s, _context.Localizer))),
    ];

    public string ZoneFilter
    {
        get => _zoneFilter;
        set
        {
            if (Set(ref _zoneFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (Set(ref _statusFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    /// <summary>The number to find. Typed into the search too, but this one is an exact match.</summary>
    public string NumberFilter
    {
        get => _numberFilter;
        set
        {
            if (Set(ref _numberFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    /// <summary>Whether retired lockers are shown: they come told apart with text, not only with colour.</summary>
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

    /// <summary>How many lockers there are in each status, whatever the filters hide.</summary>
    public string CountersText => _context.Localizer.Get(
        "Lockers.Label.Counters", _counters.Active, _counters.Free, _counters.Occupied, _counters.Reserved, _counters.Broken, _counters.Maintenance);

    void ApplyFilters()
    {
        Lockers.List.SetPredicate(row =>
            (IncludeRetired || row.Status != LockerStatusView.Retired)
            && (ZoneFilter.Length == 0 || row.ZoneId.ToString() == ZoneFilter)
            && (StatusFilter.Length == 0 || row.Status.ToString() == StatusFilter)
            && (NumberFilter.Length == 0 || (int.TryParse(NumberFilter, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && row.Number == number)));
        Lockers.RefreshFilters();
    }

    /// <summary>The filters that are on, each with what removes it, for the labels under the search.</summary>
    IReadOnlyList<ListFilterTag> ActiveFilterTags()
    {
        var text = _context.Localizer;
        var tags = new List<ListFilterTag>();
        if (ZoneFilter.Length > 0)
        {
            var zone = ZoneOptions.FirstOrDefault(o => o.Id == ZoneFilter)?.Label ?? ZoneFilter;
            tags.Add(new ListFilterTag("zone", text.Get("Common.Label.FilterTag", text.Get("Lockers.Label.Zone"), zone), () => ZoneFilter = string.Empty));
        }

        if (StatusFilter.Length > 0)
        {
            var status = StatusOptions.FirstOrDefault(o => o.Id == StatusFilter)?.Label ?? StatusFilter;
            tags.Add(new ListFilterTag("status", text.Get("Common.Label.FilterTag", text.Get("Lockers.Label.Status"), status), () => StatusFilter = string.Empty));
        }

        if (NumberFilter.Length > 0)
        {
            tags.Add(new ListFilterTag("number", text.Get("Common.Label.FilterTag", text.Get("Lockers.Label.Number"), NumberFilter), () => NumberFilter = string.Empty));
        }

        if (IncludeRetired)
        {
            tags.Add(new ListFilterTag("retired", text.Get("Lockers.Label.IncludeRetired"), () => IncludeRetired = false));
        }

        return tags;
    }

    /// <summary>Removes every filter, the search and the number, which is what the state of a list without results and the Reset button offer.</summary>
    public void ClearFilters() => Lockers.ClearFilter();

    void ResetFilterFields()
    {
        _zoneFilter = _statusFilter = _numberFilter = string.Empty;
        _includeRetired = false;
        Raise(nameof(ZoneFilter));
        Raise(nameof(StatusFilter));
        Raise(nameof(NumberFilter));
        Raise(nameof(IncludeRetired));
        ApplyFilters();
    }

    // --- Loading ---

    async Task<Result<IReadOnlyList<LockerListRow>>> LoadRowsAsync(CancellationToken ct)
    {
        var listing = await _services.ListLockers(ct);
        if (!listing.IsSuccess)
        {
            return Result<IReadOnlyList<LockerListRow>>.Failure(listing.Error!);
        }

        _counters = listing.Value!.Counters;
        _emptyState = listing.Value.EmptyState;
        Raise(nameof(CountersText));
        Raise(nameof(Counters));
        ZoneOptions = ZoneOptionsOf(listing.Value.Rows);
        return Result<IReadOnlyList<LockerListRow>>.Success(listing.Value.Rows);
    }

    async Task<Result<LockerScreenDetail?>> LoadDetailAsync(Guid id, CancellationToken ct)
    {
        var detail = await _services.Detail(id, ct);
        return detail.IsSuccess ? Result<LockerScreenDetail?>.Success(detail.Value) : Result<LockerScreenDetail?>.Failure(detail.Error!);
    }

    IReadOnlyList<EmptyStateAction> EmptyActions()
    {
        var text = _context.Localizer;
        var newZone = new AppAction("EmptyNewZone", text.Get("Lockers.Label.CreateZone"));
        newZone.Attach(() => _ = OpenNewZone());
        return _emptyState switch
        {
            LockerEmptyState.NoZones => [new(newZone.Label, newZone)],
            LockerEmptyState.NoLockers => [new(NewRange.Label, NewRange), new(NewLocker.Label, NewLocker)],
            _ => [],
        };
    }

    /// <summary>Fetches the lockers and, if one is chosen, its detail again.</summary>
    public async Task LoadAsync(CancellationToken ct = default) => await Lockers.LoadAsync(ct);

    async Task ShowCurrentAsync()
    {
        var has = Lockers.TryGetSelectedKey(out var id);
        await Detail.ShowAsync(has, id);
    }

    /// <summary>
    /// Reads again what a change touched and nothing more: the locker it was done to (one query), so the list, the map and their
    /// counters follow without loading everything; everything only when there is no one locker to read, as after adding lockers.
    /// </summary>
    async Task RefreshAsync(Guid? select = null)
    {
        if (select is { } id && Lockers.List.AllRows.Any(r => r.Id == id))
        {
            await RefreshLockerAsync(id);
            if (Lockers.List.AllRows.FirstOrDefault(r => r.Id == id) is { } row && !(Lockers.TryGetSelectedKey(out var chosen) && chosen == id))
            {
                Lockers.Select(row);
            }
            else if (Lockers.TryGetSelectedKey(out _))
            {
                await ShowCurrentAsync();
            }
        }
        else
        {
            await LoadAsync();
            if (select is { } wanted && Lockers.List.Rows.FirstOrDefault(r => r.Id == wanted) is { } row)
            {
                Lockers.Select(row);
            }
            else if (Lockers.TryGetSelectedKey(out _))
            {
                await ShowCurrentAsync();
            }
        }

        if (AfterAssignmentChange is { } changed)
        {
            await changed();
        }

        await _context.AfterWrite();
    }

    /// <summary>Reads one locker again and puts it in place, and counts the lockers again without asking for them all.</summary>
    public async Task RefreshLockerAsync(Guid lockerId, CancellationToken ct = default)
    {
        var notifier = new Arca.UI.Notifications.ResultNotifier(_context.Notifications, _context.Localizer, _context.Log);
        try
        {
            var read = await _services.Detail(lockerId, ct);
            if (!read.IsSuccess)
            {
                notifier.Error(read.Error!);
                return;
            }

            Lockers.ReplaceRow(read.Value!.Row);
            Recount();
            ZoneOptions = ZoneOptionsOf(Lockers.List.AllRows);
        }
        catch (OperationCanceledException)
        {
            // Closed meanwhile.
        }
        catch (Exception e)
        {
            notifier.Unexpected(e, "RefreshLocker");
        }
    }

    /// <summary>After an assignment or a change: the new locker and, if the student had one, the old one, which changed too, and the list of students.</summary>
    public async Task RefreshAfterAssignmentAsync(AssignmentIntent intent)
    {
        var old = _changing is { } change && change.StudentId == intent.StudentId ? change.FromLocker : (Guid?)null;
        _changing = null;
        await RefreshAsync(intent.LockerId);
        if (old is { } previous && previous != intent.LockerId)
        {
            await RefreshLockerAsync(previous);
        }
    }

    void Recount()
    {
        var active = Lockers.List.AllRows.Where(r => r.Status != LockerStatusView.Retired).ToList();
        _counters = new LockerCounters(
            active.Count,
            active.Count(r => r.Status == LockerStatusView.Free),
            active.Count(r => r.Status == LockerStatusView.Occupied),
            active.Count(r => r.Status == LockerStatusView.Broken),
            active.Count(r => r.Status == LockerStatusView.Maintenance),
            active.Count(r => r.Status == LockerStatusView.Reserved));
        Raise(nameof(CountersText));
        Raise(nameof(Counters));
    }

    IReadOnlyList<FormOption> ZoneOptionsOf(IEnumerable<LockerListRow> rows) =>
    [
        new(string.Empty, _context.Localizer.Get("Lockers.Label.AllZones")),
        .. rows.GroupBy(r => r.ZoneId).Select(g => new FormOption(g.Key.ToString(), g.First().ZoneName)).OrderBy(o => o.Label, TextComparer.Comparer),
    ];

    // --- Actions of the detail ---

    IReadOnlyList<AppAction> BuildActions(LockerScreenDetail detail)
    {
        var actions = new ActionSet(_context.Localizer);
        var row = detail.Row;
        if (row.Status == LockerStatusView.Retired)
        {
            return [];
        }

        actions.Add("Assign", "Lockers.Action.Assign", () => _ = AssignAsync(row), () => detail.ReserveBlocked); // it is free exactly when it can be reserved
        actions.Add("Change", "Shell.Action.Change", () => _ = ChangeAsync(detail), () => detail.StudentId is null ? new Error("Assignments.NoAssignment") : null);
        actions.Add("Release", "Shell.Action.Release", () => _ = ReleaseAsync(detail), () => detail.StudentId is null ? new Error("Assignments.NoAssignment") : null);
        actions.Add("Edit", "Lockers.Action.Edit", () => _ = EditAsync(detail), () => detail.EditBlocked);
        actions.Add("Reserve", "Lockers.Action.Reserve", () => _ = ReserveAsync(row), () => detail.ReserveBlocked);
        actions.Add("RemoveReservation", "Lockers.Action.RemoveReservation",
            () => _ = RunAsync(ct => _services.RemoveReservation(row.Id, ct), "RemoveReservation", row.Id), () => detail.RemoveReservationBlocked);
        actions.Add("MarkBroken", "Lockers.Action.MarkBroken", () => _ = OutOfServiceAsync(detail, OutOfServiceKindView.Broken), () => detail.BrokenBlocked);
        actions.Add("MarkMaintenance", "Lockers.Action.MarkMaintenance", () => _ = OutOfServiceAsync(detail, OutOfServiceKindView.Maintenance), () => detail.MaintenanceBlocked);
        actions.Add("Restore", "Lockers.Action.Restore", () => _ = RunAsync(ct => _services.Restore(row.Id, ct), "RestoreLocker", row.Id), () => detail.RestoreBlocked);
        actions.Add("Retire", "Lockers.Action.Retire", () => _ = RetireAsync(row), () => detail.RetireBlocked);
        return actions.Actions;
    }

    async Task AssignAsync(LockerListRow row)
    {
        if (ChosenStudent?.Invoke() is { } student && AssignStudent is { } assign)
        {
            await assign(new AssignmentIntent(student, row.Id)); // the student chosen in the panel, as dragging them here does
            return;
        }

        await _assign.ChooseStudentForAsync(row.Id, row.Number, row.ZoneName);
        await RefreshAsync(row.Id); // the detail shows the student the locker has now
    }

    /// <summary>Changing the locker of the student: the new one is chosen on the map, which opens if it was not.</summary>
    async Task ChangeAsync(LockerScreenDetail detail)
    {
        if (detail.StudentId is not { } student)
        {
            return;
        }

        BeginPick(student, detail.Row.StudentName ?? string.Empty, detail.Row.Id);
        if (OpenMap is { } open)
        {
            await open();
        }
    }

    async Task ReleaseAsync(LockerScreenDetail detail)
    {
        var row = detail.Row;
        if (detail.StudentId is not { } student)
        {
            return;
        }

        var request = new AssignmentConfirmations(_context.Localizer).ForRelease(row.StudentName ?? string.Empty, row.Number);
        if (await _context.Confirmations.ConfirmAsync(request))
        {
            await RunAsync(ct => _services.Release(student, ct), "ReleaseLocker", row.Id);
        }
    }

    Task RunAsync(Func<CancellationToken, Task<Result<string>>> operation, string name, Guid? select) =>
        _once.RunAsync(name, () => new RunOnceCommand<string>((ct, _) => operation(ct), sentence => sentence, name, _context.Notifications, _context.Localizer, _context.Log, _context.Delay, () => RefreshAsync(select)).RunAsync());

    async Task RetireAsync(LockerListRow row)
    {
        if (await _context.Confirmations.ConfirmAsync(new LockerConfirmations(_context.Localizer).ForRetire(row.Number, row.ZoneName)))
        {
            await RunAsync(ct => _services.Retire(row.Id, ct), "RetireLocker", null);
        }
    }

    // --- Forms ---

    async Task<IReadOnlyList<FormOption>?> ActiveZonesAsync()
    {
        var zones = await _services.ListZones(default);
        return zones.IsSuccess ? [.. zones.Value!.Where(z => z.IsActive).Select(z => new FormOption(z.Id.ToString(), z.Name))] : null;
    }

    /// <summary>Tells the person a zone is needed first and offers to create it, when there is none active to put lockers in.</summary>
    async Task<bool> RequireZoneAsync(IReadOnlyList<FormOption> zones)
    {
        if (zones.Count > 0)
        {
            return true;
        }

        var text = _context.Localizer;
        var choice = _context.Choices is null ? null : await _context.Choices.ChooseAsync(new ChoiceRequest(
            text.Get("Lockers.Label.NoZoneTitle"), text.Get("Lockers.Empty.NoZones"), [new("zone", text.Get("Lockers.Label.CreateZone"))], text.Get("Common.Label.Cancel")));
        if (choice == "zone")
        {
            await OpenNewZone();
        }

        return false;
    }

    static string? FieldOfLocker(Error error) => error.Code switch
    {
        "Lockers.NumberInvalid" or "Lockers.NumberInUse" or "Lockers.RangeInvalid" or "Lockers.RangeTooLarge" => "Number",
        "Lockers.ZoneUnavailable" => "Zone",
        "Lockers.NoteTooLong" => "Note",
        "Common.NumberInvalid" or "Common.DateInvalid" => error.Args.Count > 0 ? error.Args[0]?.ToString() : null,
        _ => null,
    };

    static bool TryNumber(FormFieldModel field, out int number) =>
        int.TryParse(field.Text, NumberStyles.None, CultureInfo.InvariantCulture, out number);

    int NextNumber() => Lockers.List.Rows.Where(r => r.Status != LockerStatusView.Retired).Select(r => r.Number).DefaultIfEmpty(0).Max() + 1;

    /// <summary>Opens the form of a new locker, which stays open with the next number proposed so several can be added in a row.</summary>
    public async Task NewLockerAsync()
    {
        var text = _context.Localizer;
        if (await ActiveZonesAsync() is not { } zones || !await RequireZoneAsync(zones))
        {
            return;
        }

        var number = new FormFieldModel("Number", text.Get("Lockers.Label.Number")) { Text = NextNumber().ToString(CultureInfo.InvariantCulture) };
        var zone = new FormFieldModel("Zone", text.Get("Lockers.Label.Zone"), zones) { Text = zones[0].Id };
        var note = new FormFieldModel("Note", text.Get("Lockers.Label.Note"));
        FormViewModel<string>? form = null;
        form = new FormViewModel<string>(
            [number, zone, note],
            ct => TryNumber(number, out var value) && Guid.TryParse(zone.Text, out var zoneId)
                ? _services.Add(new AddLockerRequest(value, zoneId, note.Text), ct)
                : Task.FromResult(Result<string>.Failure(FormErrors.NumberInvalid(number.Id))),
            FieldOfLocker, sentence => sentence, "AddLocker", _context.Notifications, text, _context.Log, _context.Delay,
            text.Get("Lockers.Action.New"), text.Get("Common.Action.Save"),
            _ =>
            {
                number.Text = ((TryNumber(number, out var typed) ? typed : 0) + 1).ToString(CultureInfo.InvariantCulture); // the one after this
                note.Text = string.Empty;
            },
            () => RefreshAsync());
        form.StaysOpen = true;
        await _context.Forms.ShowAsync(form);
    }

    /// <summary>Opens the form of a range of lockers: it shows how many will be created, or the numbers in conflict, before anything is created.</summary>
    public async Task NewRangeAsync()
    {
        var text = _context.Localizer;
        if (await ActiveZonesAsync() is not { } zones || !await RequireZoneAsync(zones))
        {
            return;
        }

        var first = new FormFieldModel("First", text.Get("Lockers.Label.First"));
        var last = new FormFieldModel("Last", text.Get("Lockers.Label.Last"));
        var zone = new FormFieldModel("Zone", text.Get("Lockers.Label.Zone"), zones) { Text = zones[0].Id };
        FormViewModel<string>? form = null;
        form = new FormViewModel<string>(
            [first, last, zone],
            async (ct, progress) =>
            {
                if (!TryNumber(first, out var from))
                {
                    return Result<string>.Failure(FormErrors.NumberInvalid(first.Id));
                }

                if (!TryNumber(last, out var to))
                {
                    return Result<string>.Failure(FormErrors.NumberInvalid(last.Id));
                }

                var plan = await _services.AnalyzeRange(new CreateLockerRangeRequest(from, to, Guid.Parse(zone.Text)), progress, ct);
                if (!plan.IsSuccess)
                {
                    return Result<string>.Failure(plan.Error!);
                }

                if (plan.Value!.HasConflicts)
                {
                    form!.Note = text.Get("Lockers.Note.RangeConflicts", string.Join(", ", plan.Value.Conflicts));
                    return Result<string>.Failure(FormViewModel<string>.Cancelled); // nothing is created while there are conflicts
                }

                form!.Note = text.Get("Lockers.Note.RangePreview", plan.Value.ToCreate.Count, plan.Value.ZoneName);
                if (!await _context.Confirmations.ConfirmAsync(new LockerConfirmations(text).ForRange(plan.Value), ct))
                {
                    return Result<string>.Failure(FormViewModel<string>.Cancelled);
                }

                var applied = await _services.ApplyRange(plan.Value, progress, ct);
                return applied.IsSuccess
                    ? Result<string>.Success(_services.RangeSentence(applied.Value!), [.. applied.Notices])
                    : Result<string>.Failure(applied.Error!);
            },
            FieldOfLocker, sentence => sentence, "CreateLockerRange", _context.Notifications, text, _context.Log, _context.Delay,
            text.Get("Lockers.Action.NewRange"), text.Get("Lockers.Action.CheckRange"), null, () => RefreshAsync());
        await _context.Forms.ShowAsync(form);
    }

    async Task EditAsync(LockerScreenDetail detail)
    {
        var text = _context.Localizer;
        var row = detail.Row;
        var zones = await ActiveZonesAsync() ?? [];
        var number = new FormFieldModel("Number", text.Get("Lockers.Label.Number")) { Text = row.Number.ToString(CultureInfo.InvariantCulture) };
        var zone = new FormFieldModel("Zone", text.Get("Lockers.Label.Zone"), zones) { Text = row.ZoneId.ToString() };
        var currentNumber = row.Number;
        var form = new FormViewModel<string>(
            [number, zone],
            async ct =>
            {
                if (!TryNumber(number, out var value))
                {
                    return Result<string>.Failure(FormErrors.NumberInvalid(number.Id));
                }

                Result<string>? last = null;
                if (value != currentNumber)
                {
                    last = await _services.ChangeNumber(row.Id, value, ct);
                    if (!last.IsSuccess)
                    {
                        return last;
                    }

                    currentNumber = value; // saved: a retry after a failure of the zone must not try the number again
                }

                if (zone.Text.Length > 0 && zone.Text != row.ZoneId.ToString())
                {
                    var moved = await _services.ChangeZone(row.Id, Guid.Parse(zone.Text), ct);
                    if (!moved.IsSuccess && last is not null)
                    {
                        await RefreshAsync(row.Id); // the number did change: the list and the detail show it
                    }

                    last = moved;
                }

                return last ?? Result<string>.Failure(new Error("Lockers.Unchanged"));
            },
            FieldOfLocker, sentence => sentence, "EditLocker", _context.Notifications, text, _context.Log, _context.Delay,
            text.Get("Lockers.Action.Edit"), text.Get("Common.Action.Save"), null, () => RefreshAsync(row.Id));
        await _context.Forms.ShowAsync(form);
    }

    async Task ReserveAsync(LockerListRow row)
    {
        var text = _context.Localizer;
        var note = new FormFieldModel("Note", text.Get("Lockers.Label.ReservationNote"));
        var form = new FormViewModel<string>(
            [note], ct => _services.Reserve(row.Id, note.Text, ct), FieldOfLocker, sentence => sentence, "ReserveLocker",
            _context.Notifications, text, _context.Log, _context.Delay, text.Get("Lockers.Action.Reserve"), text.Get("Common.Action.Save"),
            null, () => RefreshAsync(row.Id));
        await _context.Forms.ShowAsync(form);
    }

    // --- Out of service, with the decision about the student ---

    async Task OutOfServiceAsync(LockerScreenDetail detail, OutOfServiceKindView kind)
    {
        var row = detail.Row;
        if (row.Status != LockerStatusView.Occupied)
        {
            await _once.RunAsync("OutOfService", () => new RunOnceCommand<OutOfServiceView>(
                (ct, _) => _services.OutOfService(row.Id, kind, null, null, false, ct), v => v.Sentence ?? string.Empty, "MarkLockerOutOfService",
                _context.Notifications, _context.Localizer, _context.Log, _context.Delay, () => RefreshAsync(row.Id)).RunAsync());
            return;
        }

        // An occupied locker changes nothing until the person decides what happens to the student.
        var text = _context.Localizer;
        var choice = _context.Choices is null ? null : await _context.Choices.ChooseAsync(new ChoiceRequest(
            text.Get("Lockers.Label.DecisionTitle", row.Number),
            text.Get("Lockers.Label.DecisionMessage", row.StudentName ?? string.Empty, row.Number),
            [
                new(nameof(OutOfServiceDecisionView.Reassign), text.Get("Lockers.Label.DecisionReassign")),
                new(nameof(OutOfServiceDecisionView.Keep), text.Get("Lockers.Label.DecisionKeep")),
                new(nameof(OutOfServiceDecisionView.Release), text.Get("Lockers.Label.DecisionRelease")),
            ],
            text.Get("Common.Label.Cancel")));
        if (choice is null)
        {
            return; // the locker and its assignment stay as they were
        }

        var decision = Enum.Parse<OutOfServiceDecisionView>(choice);
        if (decision == OutOfServiceDecisionView.Reassign)
        {
            await ReassignAsync(row, kind);
            return;
        }

        await _once.RunAsync("OutOfService", () => new RunOnceCommand<OutOfServiceView>(
            (ct, _) => _services.OutOfService(row.Id, kind, decision, null, false, ct), v => v.Sentence ?? string.Empty, "MarkLockerOutOfService",
            _context.Notifications, text, _context.Log, _context.Delay, () => RefreshAsync(row.Id)).RunAsync());
    }

    async Task ReassignAsync(LockerListRow row, OutOfServiceKindView kind)
    {
        var text = _context.Localizer;
        var free = Lockers.List.Rows.Where(r => r.Status == LockerStatusView.Free && r.Id != row.Id)
            .Select(r => new FormOption(r.Id.ToString(), text.Get("Lockers.Label.FreeOption", r.Number, r.ZoneName))).ToList();
        var target = new FormFieldModel("Target", text.Get("Lockers.Label.Target"), free);
        var form = new FormViewModel<OutOfServiceView>(
            [target],
            async ct =>
            {
                if (!Guid.TryParse(target.Text, out var targetId))
                {
                    return Result<OutOfServiceView>.Failure(new Error("Lockers.NotFree"));
                }

                var first = await _services.OutOfService(row.Id, kind, OutOfServiceDecisionView.Reassign, targetId, false, ct);
                if (!first.IsSuccess || !first.Value!.NeedsWarningConfirmation)
                {
                    return first;
                }

                // The destination raised warnings: nothing was changed, so the person confirms them first.
                var request = new Arca.Application.Feedback.ConfirmationRequest(
                    text.Get("Lockers.Label.WarningsTitle"), text.Get("Lockers.Label.WarningsMessage"), text.Get("Lockers.Label.WarningsConfirm"), Details: first.Value.Warnings);
                return await _context.Confirmations.ConfirmAsync(request, ct)
                    ? await _services.OutOfService(row.Id, kind, OutOfServiceDecisionView.Reassign, targetId, true, ct)
                    : Result<OutOfServiceView>.Failure(FormViewModel<OutOfServiceView>.Cancelled);
            },
            error => error.Code == "Lockers.NotFree" ? "Target" : null, v => v.Sentence ?? string.Empty, "MarkLockerOutOfService",
            _context.Notifications, text, _context.Log, _context.Delay, text.Get("Lockers.Label.DecisionReassign"), text.Get("Common.Action.Save"), null,
            () => RefreshAsync(row.Id));
        await _context.Forms.ShowAsync(form);
    }
}
