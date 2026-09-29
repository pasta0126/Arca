// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using Arca.Application.Common;
using Arca.Application.ConceptAmounts;
using Arca.Application.ConceptAmounts.SetConceptAmounts;
using Arca.Application.SchoolYears;
using Arca.Application.SchoolYears.CreateAcademicYear;
using Arca.Application.SchoolYears.GetYearScreen;
using Arca.Domain.Common;
using Arca.UI.Actions;
using Arca.UI.Commands;
using Arca.UI.Common;
using Arca.UI.Lists;
using Arca.UI.Screens;

namespace Arca.UI.Course;

/// <summary>A year with its amounts, what the detail of the Course section shows.</summary>
public sealed record CourseDetail(YearScreenDetail Year, ConceptAmountsView Amounts);

/// <summary>
/// The Course section (pantalles-de-domini, pantalles-curs-i-imports): the list of school years, the detail of the one chosen with
/// its amounts and their history, and the operations of a year: create, activate, delete and define its amounts. It has no window
/// and no rule of its own: what can be done, and why not, is what Application answers.
/// </summary>
public sealed class CourseViewModel : ObservableObject
{
    readonly CourseServices _services;
    readonly ScreenContext _context;
    readonly YearTexts _texts;
    readonly OneAtATime _once = new();
    AcademicYearSummary? _activeMissingAmounts;

    public CourseViewModel(CourseServices services, ScreenContext context, AppAction newAction)
    {
        _services = services;
        _context = context;
        _texts = new YearTexts(context.Localizer);
        StandardNew = newAction;
        NewYear = new AppAction("NewYear", context.Localizer.Get("Course.Action.New"), newAction.Shortcut, newAction.ShortcutText);
        NewYear.Attach(() => _ = NewYearAsync());
        var text = context.Localizer;
        Years = new ScreenListViewModel<AcademicYearSummary, Guid>(
            [
                new ListColumn<AcademicYearSummary>("name", text.Get("Course.Label.Name"), y => y.Name, y => y.StartDate, Width: 2),
                new ListColumn<AcademicYearSummary>("start", text.Get("Course.Label.Start"), y => text.Format(y.StartDate), y => y.StartDate, Width: 2),
                new ListColumn<AcademicYearSummary>("end", text.Get("Course.Label.End"), y => text.Format(y.EndDate), y => y.EndDate, Width: 2),
                new ListColumn<AcademicYearSummary>("state", text.Get("Course.Label.State"), y => StateOf(y), Width: 2),
            ],
            y => y.Id, services.List, text, context.Notifications, context.Log,
            () => text.Get("SchoolYears.Empty.NoYears"), () => NewAction());
        Detail = new DetailViewModel<Guid, CourseDetail>(LoadDetailAsync, BuildActions, text, context.Notifications, context.Log, LoadHistoryAsync);
        Years.CurrentChanged += (_, _) => _ = ShowCurrentAsync();
    }

    public ScreenListViewModel<AcademicYearSummary, Guid> Years { get; }

    public DetailViewModel<Guid, CourseDetail> Detail { get; }

    /// <summary>The main actions of the screen, in order: New. The others belong to the year chosen.</summary>
    public IReadOnlyList<AppAction> MainActions => [NewYear];

    /// <summary>The action of the year form, with the shortcut of the standard New.</summary>
    public AppAction NewYear { get; }

    /// <summary>The standard New action of the registry: the view attaches it while the section is on screen, so its shortcut opens this form.</summary>
    public AppAction StandardNew { get; }

    /// <summary>The active year, when its amounts are not defined: the notice with a direct way to define them.</summary>
    public AcademicYearSummary? ActiveYearMissingAmounts
    {
        get => _activeMissingAmounts;
        private set
        {
            if (Set(ref _activeMissingAmounts, value))
            {
                Raise(nameof(HasMissingAmountsNotice));
                Raise(nameof(MissingAmountsNotice));
            }
        }
    }

    public bool HasMissingAmountsNotice => ActiveYearMissingAmounts is not null;

    public string MissingAmountsNotice => ActiveYearMissingAmounts is { } year ? _context.Localizer.Get("Course.Notice.MissingAmounts", year.Name) : string.Empty;

    /// <summary>"Define amounts" for the active year, from the notice.</summary>
    public AppAction DefineActiveAmounts { get; private set; } = new("DefineActiveAmounts", string.Empty);

    /// <summary>The text of the state of a year: with words, never only with colour.</summary>
    public string StateOf(AcademicYearSummary year) => _context.Localizer.Get(year.IsActive ? "Course.State.Active" : "Course.State.Inactive");

    /// <summary>Fetches the years, and looks whether the active one has its amounts.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        await Years.LoadAsync(ct);
        ActiveYearMissingAmounts = null;
        if (Years.List.Rows.FirstOrDefault(y => y.IsActive) is { } active
            && await _services.Detail(active.Id, ct) is { IsSuccess: true } detail && !detail.Value!.HasAmounts)
        {
            ActiveYearMissingAmounts = active;
            DefineActiveAmounts = new AppAction("DefineActiveAmounts", _context.Localizer.Get("Course.Action.DefineAmounts"));
            DefineActiveAmounts.Attach(() => _ = DefineAmountsAsync(active.Id));
            Raise(nameof(DefineActiveAmounts));
        }
    }

    async Task ShowCurrentAsync()
    {
        var has = Years.TryGetSelectedKey(out var id);
        await Detail.ShowAsync(has, id);
    }

    EmptyStateAction? NewAction() => new(NewYear.Label, NewYear);

    // --- Detail ---

    async Task<Result<CourseDetail?>> LoadDetailAsync(Guid id, CancellationToken ct)
    {
        var year = await _services.Detail(id, ct);
        if (!year.IsSuccess)
        {
            return Result<CourseDetail?>.Failure(year.Error!);
        }

        var amounts = await _services.GetAmounts(id, ct);
        return amounts.IsSuccess
            ? Result<CourseDetail?>.Success(new CourseDetail(year.Value!, amounts.Value!))
            : Result<CourseDetail?>.Failure(amounts.Error!);
    }

    async Task<Result<IReadOnlyList<string>>> LoadHistoryAsync(Guid id, CancellationToken ct)
    {
        var history = await _services.AmountsHistory(id, ct);
        return history.IsSuccess
            ? Result<IReadOnlyList<string>>.Success([.. history.Value!.Select(l => $"{_context.Localizer.Format(l.At)}: {l.Text}")])
            : Result<IReadOnlyList<string>>.Failure(history.Error!);
    }

    IReadOnlyList<AppAction> BuildActions(CourseDetail detail)
    {
        var actions = new ActionSet(_context.Localizer);
        var year = detail.Year;
        if (!year.Year.IsActive)
        {
            actions.Add("Activate", "Course.Action.Activate", () => _ = ActivateAsync(year.Year), () => year.ActivationBlocked);
        }

        actions.Add("DefineAmounts", "Course.Action.DefineAmounts", () => _ = DefineAmountsAsync(year.Year.Id), () => year.AmountsBlocked);
        actions.Add("Delete", "Course.Action.Delete", () => _ = DeleteAsync(year.Year), () => year.DeletionBlocked);
        return actions.Actions;
    }

    async Task RefreshAsync(Guid? select = null)
    {
        await LoadAsync();
        if (select is { } id && Years.List.Rows.FirstOrDefault(y => y.Id == id) is { } row)
        {
            Years.Select(row);
        }
        else if (Years.TryGetSelectedKey(out _))
        {
            await ShowCurrentAsync(); // the detail follows what changed
        }

        await _context.AfterWrite();
    }

    // --- Operations ---

    RunOnceCommand<T> Run<T>(Func<CancellationToken, Task<Result<T>>> operation, Func<T, string> success, string name, Func<Task> after) =>
        new((ct, _) => operation(ct), success, name, _context.Notifications, _context.Localizer, _context.Log, _context.Delay, after);

    async Task ActivateAsync(AcademicYearSummary year)
    {
        if (!await _context.Confirmations.ConfirmAsync(_texts.ForActivate(year)))
        {
            return;
        }

        await _once.RunAsync("Activate", () => Run(ct => _services.Activate(year.Id, ct), _texts.Activated, "ActivateYear", () => RefreshAsync(year.Id)).RunAsync());
    }

    async Task DeleteAsync(AcademicYearSummary year)
    {
        if (!await _context.Confirmations.ConfirmAsync(_texts.ForDelete(year)))
        {
            return;
        }

        await _once.RunAsync("Delete", () => Run(ct => _services.Delete(year.Id, ct), _ => _texts.Deleted(year.Name), "DeleteYear", async () =>
        {
            Years.Select(null);
            await Detail.ShowAsync(false, Guid.Empty);
            await RefreshAsync();
        }).RunAsync());
    }

    // --- Forms ---

    /// <summary>Opens the form of a new year. The dates follow the year typed while the person has not written them.</summary>
    public async Task NewYearAsync()
    {
        var text = _context.Localizer;
        var start = new FormFieldModel("StartYear", text.Get("Course.Label.StartYear"));
        var from = new FormFieldModel("StartDate", text.Get("Course.Label.Start"));
        var to = new FormFieldModel("EndDate", text.Get("Course.Label.End"));
        AcademicYearSummary? created = null;
        var form = new FormViewModel<AcademicYearSummary>(
            [start, from, to],
            async ct =>
            {
                if (!TryDate(from, out var first) || !TryDate(to, out var last))
                {
                    return Result<AcademicYearSummary>.Failure(TryDate(from, out _) ? FormErrors.DateInvalid(to.Id) : FormErrors.DateInvalid(from.Id));
                }

                return await _services.Create(new CreateAcademicYearRequest(first, last), ct);
            },
            error => error.Args.Count > 0 && error.Code.StartsWith("Common.", StringComparison.Ordinal) ? error.Args[0]?.ToString()
                : error.Code.StartsWith("SchoolYears.", StringComparison.Ordinal) ? "StartDate" : null,
            _texts.Created, "CreateYear", _context.Notifications, text, _context.Log, _context.Delay,
            text.Get("Course.Action.New"), text.Get("Common.Action.Save"), year => created = year, () => RefreshAsync(created?.Id));

        var defaults = (Start: string.Empty, End: string.Empty);
        start.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(FormFieldModel.Text))
            {
                return;
            }

            if (int.TryParse(start.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var year) && year is >= 1900 and <= 9998)
            {
                form.Note = text.Get("Course.Note.DerivedName", Arca.Domain.SchoolYears.AcademicYear.NameOf(year));
                var next = (text.Format(new DateOnly(year, 9, 1)), text.Format(new DateOnly(year + 1, 6, 30)));
                if (from.Text == defaults.Start && to.Text == defaults.End)
                {
                    (from.Text, to.Text) = next;
                    defaults = next;
                }
            }
            else
            {
                form.Note = null;
            }
        };
        await _context.Forms.ShowAsync(form);
    }

    bool TryDate(FormFieldModel field, out DateOnly date) =>
        DateOnly.TryParse(field.Text, _context.Localizer.Culture, DateTimeStyles.None, out date);

    /// <summary>Opens the form of the three amounts of a year, with the previous year's proposed when it has none of its own.</summary>
    public async Task DefineAmountsAsync(Guid yearId)
    {
        var text = _context.Localizer;
        var loaded = await LoadDetailAsync(yearId, default);
        if (!loaded.IsSuccess || loaded.Value is not { } detail)
        {
            return;
        }

        var fields = ConceptNames.All.Select(id => new FormFieldModel(id, ConceptNames.Of(text, id))).ToArray();
        var amounts = detail.Amounts;
        foreach (var (field, value) in fields.Zip([amounts.Fee, amounts.Deposit, amounts.KeyReplacementFee]))
        {
            field.Text = value is { } v ? v.ToString("0.00", text.Culture) : string.Empty;
        }

        var year = detail.Year.Year;
        var form = new FormViewModel<ConceptAmountsView>(
            fields,
            async ct =>
            {
                var values = new List<decimal>();
                foreach (var field in fields)
                {
                    if (!decimal.TryParse(field.Text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, text.Culture, out var value))
                    {
                        return Result<ConceptAmountsView>.Failure(FormErrors.NumberInvalid(field.Id));
                    }

                    values.Add(value);
                }

                // Changing amounts of a year that already has charges only affects the charges generated later: say so first.
                if (detail.Year.HasCharges && detail.Year.HasAmounts && !await _context.Confirmations.ConfirmAsync(_texts.ForAmountChange(year), ct))
                {
                    return Result<ConceptAmountsView>.Failure(FormViewModel<ConceptAmountsView>.Cancelled);
                }

                return await _services.SetAmounts(new SetConceptAmountsRequest(yearId, values[0], values[1], values[2]), ct);
            },
            error => error.Args.Count > 0 && error.Code is "Common.NumberInvalid" or "ConceptAmounts.AmountInvalid"
                ? error.Args[^1]?.ToString() : null,
            _ => _texts.AmountsSaved(year.Name), "SetAmounts", _context.Notifications, text, _context.Log, _context.Delay,
            text.Get("Course.Action.DefineAmounts") + " · " + year.Name, text.Get("Common.Action.Save"), null, () => RefreshAsync(yearId));
        form.Note = amounts.IsProposed
            ? text.Get("Course.Note.ProposedFrom", amounts.ProposedFrom ?? string.Empty)
            : amounts.IsComplete ? null : text.Get("Course.Note.AmountRange");
        await _context.Forms.ShowAsync(form);
    }
}
