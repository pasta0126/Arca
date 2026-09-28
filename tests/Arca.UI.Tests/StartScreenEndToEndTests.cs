// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.ConceptAmounts.SetConceptAmounts;
using Arca.Application.GlobalState;
using Arca.Application.LockerMap;
using Arca.Application.Localization;
using Arca.Application.Lockers.AddLocker;
using Arca.Application.SchoolYears.CreateAcademicYear;
using Arca.Application.Search;
using Arca.Application.Students.AddStudent;
using Arca.Application.Zones.CreateZone;
using Arca.Desktop.Composition;
using Arca.Infrastructure.Common;
using Arca.Infrastructure.Inventory;
using Arca.Infrastructure.Storage;
using Arca.Testing;
using Arca.UI.Map;
using Arca.UI.Notifications;
using Arca.UI.Preferences;
using Arca.UI.Search;
using Arca.UI.Shell;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Arca.UI.Tests;

/// <summary>
/// The start screen over a real encrypted database with the composition of the application: a caretaker searches for a student,
/// drags them onto a free locker, sees the map, the counters and the debt follow, opens the locker from the search and releases
/// it. Nothing is faked below the screen, so it proves the pieces fit and that what the screen did is really in the database.
/// </summary>
public sealed class StartScreenEndToEndTests : IDisposable
{
    readonly string _folder = Path.Combine(Path.GetTempPath(), "arca-e2e-" + Guid.NewGuid().ToString("N"));
    readonly Arca.Application.Storage.DatabaseKey _key = TestKeys.FromSeed("start-screen");
    readonly ResxLocalizer _localizer = new();
    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    readonly ManualDelay _delay = new();

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    string DatabasePath => Path.Combine(_folder, "arca.db");

    EfInventory Open() => new(() => new ArcaDbContext(DatabasePath, _key));

    static async Task WaitAsync(Func<bool> done)
    {
        for (var i = 0; i < 200 && !done(); i++)
        {
            await Task.Delay(15);
        }

        Assert.True(done(), "the operation did not finish");
    }

    [Fact]
    [Trait("spec", "ui-shell: recorrido de extremo a extremo (búsqueda, asignar arrastrando, ficha y liberar)")]
    public async Task A_caretaker_searches_drags_a_student_onto_a_locker_and_releases_it_over_the_real_database()
    {
        Directory.CreateDirectory(_folder);
        await (await ArcaDatabase.CreateAsync(DatabasePath, _key)).Value!.DisposeAsync();
        var store = Open();
        var clock = new SystemClock();

        // The centre: a year with its amounts, a zone with two lockers and two students.
        var year = (await new CreateAcademicYearHandler(store.Years, store).HandleAsync(new CreateAcademicYearRequest(new DateOnly(2026, 9, 1), new DateOnly(2027, 6, 30)), default)).Value!;
        await new SetConceptAmountsHandler(store.Years, store.ConceptAmounts, store.ConceptAmountEvents, store, clock).HandleAsync(new SetConceptAmountsRequest(year.Id, 50m, 20m, 10m), default);
        var zone = (await new CreateZoneHandler(store.Zones, store).HandleAsync(new CreateZoneRequest("Planta 1"), default)).Value!;
        var addLocker = new AddLockerHandler(store.Lockers, store.Zones, store.Events, store, clock);
        var locker = (await addLocker.HandleAsync(new AddLockerRequest(15, zone.Id), default)).Value!;
        await addLocker.HandleAsync(new AddLockerRequest(16, zone.Id), default);
        var occupancy = new AssignmentOccupancy(store.Assignments, store.Lockers);
        var add = new AddStudentHandler(store.Students, store.Enrollments, store.Catalog, store.Years, store.StudentEvents, occupancy, store, clock);
        var marta = (await add.HandleAsync(new AddStudentRequest("Marta", "Puig", "marta@test.cat", "1r ESO", "A", ConfirmNewValues: true), default)).Value!.Student!;
        await add.HandleAsync(new AddStudentRequest("Pau", "Abad", "pau@test.cat", "1r ESO", "A", ConfirmNewValues: true), default);

        // The screen, composed as the application composes it.
        var notifier = new ResultNotifier(_notifications, _localizer, _log);
        var state = new GlobalStateService(new GetGlobalStateHandler(store.Years, store.Charges).HandleAsync, notifier);
        var confirmations = new RecordingConfirmations(true);
        var home = new LockerHomeModel(
            LockerHomeComposition.Create(store, clock, _localizer), new UiPreferencesSession(new NoPreferences()), notifier, confirmations, _localizer, _notifications, _log, _delay, state);
        var search = new GlobalSearchViewModel(
            new GlobalSearchHandler(store.Students, store.Enrollments, store.Catalog, store.Years, store.Lockers, store.Zones, store.Assignments, store.Charges).HandleAsync,
            _delay, notifier, new SearchNavigator(), _localizer);
        await home.LoadAsync();
        await state.RefreshAsync();

        Assert.Equal((2, 0), (home.Map.Counters.Free, home.Map.Counters.Occupied));
        Assert.Equal(2, home.Students.Count);
        Assert.Equal(("2026-2027", 0), (state.Current!.ActiveYear!.Name, state.Current.PendingCharges));

        // A search finds the student.
        search.Text = "puig";
        await Task.Delay(20);
        _delay.Elapse(GlobalSearchViewModel.Pause + TimeSpan.FromMilliseconds(1));
        await WaitAsync(() => search.State == SearchState.Results);
        Assert.Contains(search.Items, i => i.Text.StartsWith("Puig, Marta", StringComparison.Ordinal));

        // Dragging the student onto the free locker assigns it, through the same checks as everything else.
        home.Drop.BeginDrag(marta.Id);
        Assert.True(await home.Drop.DropAsync(locker.Id));
        await WaitAsync(() => home.Map.Find(locker.Id)?.Status == LockerStatusView.Occupied);
        await WaitAsync(() => state.Current!.PendingCharges == 2);

        var placed = home.Map.Find(locker.Id)!;
        Assert.Equal(("Marta Puig", true), (placed.StudentName, placed.HasDebt)); // the fee and the deposit are pending
        Assert.Equal((1, 1), (home.Map.Counters.Free, home.Map.Counters.Occupied));
        Assert.Equal(1, home.Students.Count);
        Assert.Contains(_notifications.Published, n => n.Text.Contains("Marta Puig", StringComparison.Ordinal));

        // The search now finds the locker; choosing it opens its detail.
        search.Text = "15";
        await Task.Delay(20);
        _delay.Elapse(GlobalSearchViewModel.Pause + TimeSpan.FromMilliseconds(1));
        await WaitAsync(() => search.Items.Any(i => i.Target?.Kind == SearchTargetKind.Locker));
        home.Map.Reveal(search.Items.First(i => i.Target?.Kind == SearchTargetKind.Locker).Target!.Id);
        await WaitAsync(() => home.Detail.Detail?.StudentName == "Marta Puig");
        Assert.Equal((true, 70m), (home.Detail.Detail!.HasDebt, home.Detail.Detail.PendingTotal));
        Assert.True(home.Detail.Actions.Single(a => a.Id == "Release").IsAvailable);

        // Releasing it asks first and frees the locker.
        home.Detail.Actions.Single(a => a.Id == "Release").Execute(null);
        await WaitAsync(() => home.Map.Find(locker.Id)?.Status == LockerStatusView.Free);
        Assert.Single(confirmations.Asked);
        Assert.Equal(2, home.Students.Count);

        // And all of it is really in the database.
        SqliteConnection.ClearAllPools();
        var reopened = Open();
        var map = (await new GetLockerMapHandler(reopened.Zones, reopened.Lockers, reopened.Assignments, reopened.Students, reopened.Charges).HandleAsync(default)).Value!;
        Assert.Equal((2, 0), (map.Counters.Free, map.Counters.Occupied));
        Assert.Equal(2, (await reopened.Charges.ListPendingAsync(default)).Count); // releasing a locker does not cancel what is owed
        Assert.Empty(_log.Entries); // and nothing went wrong on the way
    }

    sealed class NoPreferences : Arca.Application.Preferences.IUiPreferencesStore
    {
        public Arca.Application.Preferences.UiPreferences Load() => new();

        public void Save(Arca.Application.Preferences.UiPreferences preferences)
        {
        }
    }
}
