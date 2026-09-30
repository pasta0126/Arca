// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Home;
using Arca.Application.Localization;
using Arca.Application.Search;
using Arca.Domain.Common;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Notifications;
using Arca.UI.Shell;

namespace Arca.UI.Home;

/// <summary>What the start screen is showing: it is reading the summary, it has no active year, the centre is not set up yet, or the summary.</summary>
public enum StartHomeState
{
    Loading,
    NoActiveYear,
    NotSetUp,
    Summary,
}

/// <summary>One count on the start screen that opens a screen with the filter that produced it: how many, what of, and the action that opens it.</summary>
public sealed record StartHomeLink(string Id, string Section, int Count, string Text, AppAction Open);

/// <summary>
/// The start screen (pantalla-principal, Inicio como pantalla registrable): the active year and a few counts of things, each one a link that
/// opens Lockers or Students already filtered. Counts only, never an amount. Without an active year it says so and offers the Course section;
/// in a centre with no lockers and no students it says what to set up first.
/// </summary>
public sealed class StartHomeModel : ObservableObject
{
    readonly Func<CancellationToken, Task<Result<HomeSummary>>> _load;
    readonly ScreenFilterRouter _router;
    readonly Func<string, bool> _navigate;
    readonly ResultNotifier _notifier;
    readonly ILocalizer _localizer;
    HomeSummary? _summary;

    public StartHomeModel(
        Func<CancellationToken, Task<Result<HomeSummary>>> load, ScreenFilterRouter router, Func<string, bool> navigate, ResultNotifier notifier, ILocalizer localizer)
    {
        _load = load;
        _router = router;
        _navigate = navigate;
        _notifier = notifier;
        _localizer = localizer;
        GoToCourse = new AppAction("GoToCourse", localizer.Get("Shell.Home.Action.GoToCourse"));
        GoToCourse.Attach(() => _navigate(ShellCatalog.Course));
        SetUpLockers = new AppAction("SetUpLockers", localizer.Get("Shell.Home.Action.SetUpLockers"));
        SetUpLockers.Attach(() => _router.Open(new ScreenFilterRequest(ShellCatalog.Lockers, "Zones", new Dictionary<string, string>())));
        AddStudents = new AppAction("AddStudents", localizer.Get("Shell.Home.Action.AddStudents"));
        AddStudents.Attach(() => _router.Open(new ScreenFilterRequest(ShellCatalog.Students, null, new Dictionary<string, string>())));
    }

    public HomeSummary? Summary => _summary;

    public StartHomeState State =>
        _summary is null ? StartHomeState.Loading
        : _summary.ActiveYearName is null ? StartHomeState.NoActiveYear
        : !_summary.HasLockers && !_summary.HasStudents ? StartHomeState.NotSetUp
        : StartHomeState.Summary;

    public string YearText => _summary?.ActiveYearName is { } year ? _localizer.Get("Shell.Home.Label.Year", year) : string.Empty;

    /// <summary>Opens the Course section, where a year is activated.</summary>
    public AppAction GoToCourse { get; }

    /// <summary>Opens the zones, where a centre that is not set up begins.</summary>
    public AppAction SetUpLockers { get; }

    /// <summary>Opens the students, to add the first one.</summary>
    public AppAction AddStudents { get; }

    /// <summary>The counts of the lockers, one per status, each opening the map with that status.</summary>
    public IReadOnlyList<StartHomeLink> LockerLinks => _summary is null ? [] :
    [
        .. new[] { LockerStatusView.Free, LockerStatusView.Occupied, LockerStatusView.Reserved, LockerStatusView.Broken, LockerStatusView.Maintenance }
            .Select(status => Link(
                "locker:" + status, ShellCatalog.Lockers, CountOf(status), LockerStatusPresentation.Text(status, _localizer),
                new ScreenFilterRequest(ShellCatalog.Lockers, "LockerMap", new Dictionary<string, string> { ["Status"] = status.ToString() }))),
    ];

    /// <summary>The counts of the students without a locker and with a pending payment, each opening the list already filtered.</summary>
    public IReadOnlyList<StartHomeLink> StudentLinks => _summary is null || _summary.ActiveYearName is null ? [] :
    [
        Link("students:without", ShellCatalog.Students, _summary.StudentsWithoutLocker, _localizer.Get("Shell.Home.Link.WithoutLocker"),
            new ScreenFilterRequest(ShellCatalog.Students, null, new Dictionary<string, string> { ["Locker"] = "without" })),
        Link("students:pending", ShellCatalog.Students, _summary.StudentsWithPending, _localizer.Get("Shell.Home.Link.WithPending"),
            new ScreenFilterRequest(ShellCatalog.Students, null, new Dictionary<string, string> { ["Payment"] = "pending" })),
    ];

    int CountOf(LockerStatusView status) => _summary is null ? 0 : status switch
    {
        LockerStatusView.Free => _summary.Lockers.Free,
        LockerStatusView.Occupied => _summary.Lockers.Occupied,
        LockerStatusView.Reserved => _summary.Lockers.Reserved,
        LockerStatusView.Broken => _summary.Lockers.Broken,
        _ => _summary.Lockers.Maintenance,
    };

    StartHomeLink Link(string id, string section, int count, string text, ScreenFilterRequest request)
    {
        var open = new AppAction(id, _localizer.Get("Shell.Home.Link.Open", text, count));
        open.Attach(() => _router.Open(request));
        return new StartHomeLink(id, section, count, text, open);
    }

    /// <summary>Reads the summary again. Called when the screen opens and whenever the state of the application changes.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _load(ct);
            if (!result.IsSuccess)
            {
                _notifier.Error(result.Error!);
                return;
            }

            _summary = result.Value;
            Raise(nameof(Summary));
            Raise(nameof(State));
            Raise(nameof(YearText));
            Raise(nameof(LockerLinks));
            Raise(nameof(StudentLinks));
        }
        catch (OperationCanceledException)
        {
            // Closed while loading.
        }
        catch (Exception e)
        {
            _notifier.Unexpected(e, "LoadStartSummary");
        }
    }
}
