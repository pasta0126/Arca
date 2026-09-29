// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
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

    public LockersViewModel(LockerServices services, ScreenContext context, AppAction standardNew, Func<Task> openNewZone, AssignmentDialogs assign)
    {
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
        ApplyFilters();
        Detail = new DetailViewModel<Guid, LockerScreenDetail>(LoadDetailAsync, BuildActions, text, context.Notifications, context.Log, services.History);
        Lockers.CurrentChanged += (_, _) => _ = ShowCurrentAsync();
    }

    public ScreenListViewModel<LockerListRow, Guid> Lockers { get; }

    public DetailViewModel<Guid, LockerScreenDetail> Detail { get; }

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

    void ApplyFilters() => Lockers.List.SetPredicate(row =>
        (IncludeRetired || row.Status != LockerStatusView.Retired)
        && (ZoneFilter.Length == 0 || row.ZoneId.ToString() == ZoneFilter)
        && (StatusFilter.Length == 0 || row.Status.ToString() == StatusFilter)
        && (NumberFilter.Length == 0 || (int.TryParse(NumberFilter, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && row.Number == number)));

    /// <summary>Removes every filter, the search and the number, which is what the state of a list without results offers.</summary>
    public void ClearFilters()
    {
        _zoneFilter = _statusFilter = _numberFilter = string.Empty;
        _includeRetired = false;
        Raise(nameof(ZoneFilter));
        Raise(nameof(StatusFilter));
        Raise(nameof(NumberFilter));
        Raise(nameof(IncludeRetired));
        Lockers.ClearFilter();
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
        ZoneOptions =
        [
            new(string.Empty, _context.Localizer.Get("Lockers.Label.AllZones")),
            .. listing.Value.Rows.GroupBy(r => r.ZoneId).Select(g => new FormOption(g.Key.ToString(), g.First().ZoneName)).OrderBy(o => o.Label, TextComparer.Comparer),
        ];
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

    async Task RefreshAsync(Guid? select = null)
    {
        await LoadAsync();
        if (select is { } id && Lockers.List.Rows.FirstOrDefault(r => r.Id == id) is { } row)
        {
            Lockers.Select(row);
        }
        else if (Lockers.TryGetSelectedKey(out _))
        {
            await ShowCurrentAsync();
        }

        await _context.AfterWrite();
    }

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
        await _assign.ChooseStudentForAsync(row.Id, row.Number, row.ZoneName);
        await RefreshAsync(row.Id); // the detail shows the student the locker has now
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
        var form = new FormViewModel<string>(
            [number, zone],
            async ct =>
            {
                if (!TryNumber(number, out var value))
                {
                    return Result<string>.Failure(FormErrors.NumberInvalid(number.Id));
                }

                Result<string>? last = null;
                if (value != row.Number)
                {
                    last = await _services.ChangeNumber(row.Id, value, ct);
                    if (!last.IsSuccess)
                    {
                        return last;
                    }
                }

                if (zone.Text.Length > 0 && zone.Text != row.ZoneId.ToString())
                {
                    last = await _services.ChangeZone(row.Id, Guid.Parse(zone.Text), ct);
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
