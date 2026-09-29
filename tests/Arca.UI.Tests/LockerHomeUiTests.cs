// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Assignments.CheckAssignmentTarget;
using Arca.Application.Feedback;
using Arca.Application.GlobalState;
using Arca.Application.LockerMap;
using Arca.Application.Localization;
using Arca.Application.Preferences;
using Arca.Application.Search;
using Arca.Application.Students;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Actions;
using Arca.UI.Assigning;
using Arca.UI.Map;
using Arca.UI.Notifications;
using Arca.UI.Preferences;
using Arca.UI.Shell;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;

namespace Arca.UI.Tests;

public sealed class LockerHomeUiTests
{
    const string Spec = "ui-shell/pantalla-principal";

    sealed class MemoryStore : IUiPreferencesStore
    {
        public UiPreferences Load() => new();

        public void Save(UiPreferences preferences)
        {
        }
    }

    static readonly Guid _zone = Guid.NewGuid();
    static readonly Guid _free = Guid.NewGuid();
    static readonly Guid _free2 = Guid.NewGuid();
    static readonly Guid _occupied = Guid.NewGuid();
    static readonly Guid _reserved = Guid.NewGuid();
    static readonly Guid _broken = Guid.NewGuid();
    static readonly Guid _holder = Guid.NewGuid();
    static readonly Guid _waiting = Guid.NewGuid();

    readonly ResxLocalizer _localizer = new();
    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    readonly ManualDelay _delay = new();
    readonly RecordingConfirmations _confirmations = new(true);
    readonly List<string> _calls = [];
    readonly List<AssignLockerRequest> _assigned = [];
    readonly List<AssignLockerRequest> _changed = [];
    readonly Dictionary<Guid, MapLocker> _lockers = [];
    Result<StudentListing> _students = Result<StudentListing>.Success(new(
        [new StudentRow(_waiting, "Pau", "Abad", "1r ESO", "A", false, null), new StudentRow(Guid.NewGuid(), "Aina", "Zapata", "2n ESO", "B", false, null)], new(50, 48, 2)));
    int _stateLoads;
    int _mapLoads;

    public LockerHomeUiTests()
    {
        foreach (var l in new[]
        {
            new MapLocker(_free, 1, LockerStatusView.Free, null, null, false),
            new MapLocker(_free2, 2, LockerStatusView.Free, null, null, false),
            new MapLocker(_occupied, 3, LockerStatusView.Occupied, _holder, "Marta Puig", true),
            new MapLocker(_reserved, 4, LockerStatusView.Reserved, null, null, false),
            new MapLocker(_broken, 5, LockerStatusView.Broken, null, null, false),
        })
        {
            _lockers[l.LockerId] = l;
        }
    }

    Task<Result<string>> Op(string name, Guid id) { _calls.Add($"{name}:{id}"); return Task.FromResult(Result<string>.Success("Fet.")); }

    LockerHomeModel Home()
    {
        var services = new LockerHomeServices(
            _ =>
            {
                _mapLoads++;
                var lockers = _lockers.Values.OrderBy(l => l.Number).ToList();
                return Task.FromResult(Result<LockerMapData>.Success(new([new ZoneMap(_zone, "Planta 1", lockers, GetLockerMapHandler.Count(lockers))], GetLockerMapHandler.Count(lockers))));
            },
            (id, _) => { _calls.Add("reread:" + id); return Task.FromResult(Result<MapLocker?>.Success(_lockers.GetValueOrDefault(id))); },
            (id, _) =>
            {
                var l = _lockers[id];
                return Task.FromResult(Result<LockerDetail?>.Success(new LockerDetail(
                    id, l.Number, "Planta 1", l.Status, null, null, l.StudentId, l.StudentName, l.StudentId is null ? null : "2n ESO", l.StudentId is null ? null : "B", l.HasDebt, l.HasDebt ? 70m : 0m)));
            },
            _ => Task.FromResult(_students),
            (_, locker, _) => Task.FromResult(Result<AssignmentTargetCheck>.Success(_lockers[locker].Status == LockerStatusView.Free ? new AssignmentTargetCheck(null, []) : new AssignmentTargetCheck(new Error("Assignments.LockerUnavailable"), []))),
            (r, _) => { _assigned.Add(r); return Task.FromResult(Result<AssignLockerResult>.Success(new AssignLockerResult(Row(r), []))); },
            (r, _) => { _changed.Add(r); return Task.FromResult(Result<AssignLockerResult>.Success(new AssignLockerResult(Row(r), []))); },
            new LockerOperations(
                (student, _) => Op("release", student), (locker, _) => Op("reserve", locker), (locker, _) => Op("unreserve", locker),
                (locker, _) => Op("broken", locker), (locker, _) => Op("restore", locker)));
        var state = new GlobalStateService(_ => { _stateLoads++; return Task.FromResult(Result<GlobalState>.Success(new(null, _stateLoads))); }, new ResultNotifier(_notifications, _localizer, _log));
        return new LockerHomeModel(
            services, new UiPreferencesSession(new MemoryStore()), new ResultNotifier(_notifications, _localizer, _log), _confirmations, _localizer,
            _notifications, _log, _delay, state);
    }

    static AssignmentRow Row(AssignLockerRequest r) => new(
        Guid.NewGuid(), r.StudentId, "Pau Abad", r.LockerId, 1, "Planta 1", "2026-2027", DateTimeOffset.UtcNow, null, null, null);

    static AppAction Action(LockerHomeModel home, string id) => home.Detail.Actions.Single(a => a.Id == id);

    static async Task<LockerHomeModel> LoadedAsync(LockerHomeModel home)
    {
        await home.LoadAsync();
        return home;
    }

    // --- The detail and its actions ---

    [Fact]
    [Trait("spec", Spec + ": Detalle de la taquilla seleccionada (Taquilla ocupada)")]
    public async Task An_occupied_locker_offers_release_and_change_and_explains_why_the_rest_are_not_available()
    {
        var home = await LoadedAsync(Home());

        home.Map.Select(_occupied);
        await Task.Delay(20);

        Assert.Equal("Marta Puig", home.Detail.Detail!.StudentName);
        Assert.True(Action(home, "Release").IsAvailable);
        Assert.True(Action(home, "Change").IsAvailable);
        Assert.False(Action(home, "Assign").IsAvailable);
        Assert.Equal("La taquilla ja té un alumne.", Action(home, "Assign").UnavailableReason);
        Assert.Equal("Allibera primer la taquilla.", Action(home, "MarkBroken").UnavailableReason);
    }

    [Fact]
    [Trait("spec", Spec + ": Detalle de la taquilla seleccionada (Taquilla libre)")]
    public async Task A_free_locker_offers_assign_reserve_and_mark_broken_and_assign_needs_a_chosen_student()
    {
        var home = await LoadedAsync(Home());

        home.Map.Select(_free);
        await Task.Delay(20);

        Assert.True(Action(home, "Reserve").IsAvailable);
        Assert.True(Action(home, "MarkBroken").IsAvailable);
        Assert.False(Action(home, "Assign").IsAvailable);
        Assert.Equal("Tria primer un alumne al panell d'alumnes sense taquilla.", Action(home, "Assign").UnavailableReason);
        Assert.Equal("La taquilla no té cap alumne.", Action(home, "Release").UnavailableReason);

        home.Students.Select(_waiting);
        Assert.True(Action(home, "Assign").IsAvailable); // choosing the student makes it available, with no reload
    }

    [Fact]
    [Trait("spec", Spec + ": Detalle de la taquilla seleccionada (Acciones no disponibles)")]
    public async Task Reserved_and_out_of_service_lockers_offer_only_what_applies_to_them()
    {
        var home = await LoadedAsync(Home());

        home.Map.Select(_reserved);
        await Task.Delay(20);
        Assert.True(Action(home, "RemoveReservation").IsAvailable);
        Assert.False(Action(home, "Assign").IsAvailable);

        home.Map.Select(_broken);
        await Task.Delay(20);
        Assert.True(Action(home, "Restore").IsAvailable);
        Assert.Equal("La taquilla és fora de servei.", Action(home, "Reserve").UnavailableReason);
    }

    [Fact]
    [Trait("spec", Spec + ": Detalle de la taquilla seleccionada (Taquilla ocupada)")]
    public async Task Releasing_asks_for_confirmation_then_frees_the_locker_and_reads_again_only_that_locker()
    {
        var home = await LoadedAsync(Home());
        home.Map.Select(_occupied);
        await Task.Delay(20);
        _lockers[_occupied] = new MapLocker(_occupied, 3, LockerStatusView.Free, null, null, false); // what the use case will have done
        _calls.Clear();

        Action(home, "Release").Execute(null);
        await Task.Delay(50);

        var asked = Assert.Single(_confirmations.Asked);
        Assert.Contains("Marta Puig", asked.Title);
        Assert.Contains($"release:{_holder}", _calls);
        Assert.Equal([$"reread:{_occupied}"], _calls.Where(c => c.StartsWith("reread:", StringComparison.Ordinal))); // only that locker
        Assert.Equal(1, _mapLoads); // the map was not loaded again
        Assert.Equal(1, _stateLoads); // and the global state was, because payments may have changed
        Assert.Equal(LockerStatusView.Free, home.Map.Find(_occupied)!.Status);
        Assert.Equal((0, 3), (home.Map.Counters.Occupied, home.Map.Counters.Free)); // the counters follow
    }

    [Fact]
    [Trait("spec", Spec + ": Detalle de la taquilla seleccionada (Taquilla ocupada)")]
    public async Task Declining_the_release_confirmation_changes_nothing()
    {
        var home = await LoadedAsync(Home());
        var declined = new RecordingConfirmations(false);
        home = new LockerHomeModel(HomeServicesOf(home), new UiPreferencesSession(new MemoryStore()), new ResultNotifier(_notifications, _localizer, _log), declined, _localizer, _notifications, _log, _delay,
            new GlobalStateService(_ => Task.FromResult(Result<GlobalState>.Success(new(null, 0))), new ResultNotifier(_notifications, _localizer, _log)));
        await home.LoadAsync();
        home.Map.Select(_occupied);
        await Task.Delay(20);

        Action(home, "Release").Execute(null);
        await Task.Delay(50);

        Assert.Single(declined.Asked);
        Assert.DoesNotContain(_calls, c => c.StartsWith("release:", StringComparison.Ordinal));
    }

    LockerHomeServices HomeServicesOf(LockerHomeModel _) => new(
        _ => Task.FromResult(Result<LockerMapData>.Success(new([new ZoneMap(_zone, "Planta 1", [.. _lockers.Values], GetLockerMapHandler.Count(_lockers.Values))], GetLockerMapHandler.Count(_lockers.Values)))),
        (id, _) => Task.FromResult(Result<MapLocker?>.Success(_lockers.GetValueOrDefault(id))),
        (id, _) => Task.FromResult(Result<LockerDetail?>.Success(new LockerDetail(id, 3, "Planta 1", _lockers[id].Status, null, null, _lockers[id].StudentId, _lockers[id].StudentName, null, null, false, 0m))),
        _ => Task.FromResult(_students),
        (_, _, _) => Task.FromResult(Result<AssignmentTargetCheck>.Success(new(null, []))),
        (r, _) => Task.FromResult(Result<AssignLockerResult>.Success(new(Row(r), []))),
        (r, _) => Task.FromResult(Result<AssignLockerResult>.Success(new(Row(r), []))),
        new LockerOperations((s, _) => Op("release", s), (l, _) => Op("reserve", l), (l, _) => Op("unreserve", l), (l, _) => Op("broken", l), (l, _) => Op("restore", l)));

    [Fact]
    [Trait("spec", Spec + ": Detalle de la taquilla seleccionada (Taquilla libre)")]
    public async Task Reserve_unreserve_mark_broken_and_restore_each_act_on_the_locker_and_read_it_again()
    {
        var home = await LoadedAsync(Home());
        var cases = new (Guid Locker, string Action, string Call)[]
        {
            (_free, "Reserve", "reserve"), (_reserved, "RemoveReservation", "unreserve"), (_free2, "MarkBroken", "broken"), (_broken, "Restore", "restore"),
        };

        foreach (var (locker, action, call) in cases)
        {
            home.Map.Select(locker);
            await Task.Delay(20);
            _calls.Clear();

            Action(home, action).Execute(null);
            await Task.Delay(50);

            Assert.Contains($"{call}:{locker}", _calls);
            Assert.Contains($"reread:{locker}", _calls);
        }
    }

    // --- Assigning, from the detail, the list and dragging ---

    [Fact]
    [Trait("spec", Spec + ": Alumnos sin taquilla (Asignar arrastrando)")]
    public async Task Assigning_from_the_detail_the_list_menu_and_dragging_all_send_the_same_request_and_update_the_same_way()
    {
        var home = await LoadedAsync(Home());
        home.Map.Select(_free);
        home.Students.Select(_waiting);
        await Task.Delay(20);

        Action(home, "Assign").Execute(null); // from the detail of the locker
        await Task.Delay(50);
        home.AssignToSelected.Execute(null); // from the menu of the list
        await Task.Delay(50);
        home.Drop.BeginDrag(_waiting); // by dragging
        await home.Drop.DropAsync(_free);
        await Task.Delay(50);

        Assert.Equal(3, _assigned.Count);
        Assert.All(_assigned, r => Assert.Equal((_waiting, _free, false), (r.StudentId, r.LockerId, r.ConfirmWarnings)));
        Assert.Equal(3, _notifications.Published.Count(n => n.Kind == NotificationKind.Success));
        Assert.Equal(1, _mapLoads); // never the whole map again
    }

    [Fact]
    [Trait("spec", Spec + ": Alumnos sin taquilla (Asignar arrastrando)")]
    public async Task The_list_menu_action_needs_a_chosen_student_and_a_chosen_free_locker()
    {
        var home = await LoadedAsync(Home());

        Assert.Equal("Tria primer un alumne al panell d'alumnes sense taquilla.", home.AssignToSelected.UnavailableReason);
        home.Students.Select(_waiting);
        Assert.Equal("Tria una taquilla lliure al mapa.", home.AssignToSelected.UnavailableReason);
        home.Map.Select(_occupied);
        Assert.False(home.AssignToSelected.IsAvailable);
        home.Map.Select(_free);
        Assert.True(home.AssignToSelected.IsAvailable);
    }

    // --- Changing a student's locker ---

    [Fact]
    [Trait("spec", Spec + ": Detalle de la taquilla seleccionada (Taquilla ocupada)")]
    public async Task Changing_asks_for_the_new_locker_on_the_map_ignores_taken_ones_and_moves_the_student_to_the_free_one_chosen()
    {
        var home = await LoadedAsync(Home());
        home.Map.Select(_occupied);
        await Task.Delay(20);

        Action(home, "Change").Execute(null);
        Assert.Equal(("Marta Puig"), home.Map.Picking!.Value.StudentName);
        home.Map.Select(_broken); // not free: ignored, still choosing
        Assert.NotNull(home.Map.Picking);
        Assert.Empty(_changed);

        _lockers[_occupied] = new MapLocker(_occupied, 3, LockerStatusView.Free, null, null, false);
        _lockers[_free2] = new MapLocker(_free2, 2, LockerStatusView.Occupied, _holder, "Marta Puig", true); // what the change will have done
        home.Map.Select(_free2);
        await Task.Delay(80);

        Assert.Null(home.Map.Picking);
        var request = Assert.Single(_changed);
        Assert.Equal((_holder, _free2), (request.StudentId, request.LockerId));
        Assert.Equal(1, _mapLoads);
        Assert.Contains(_notifications.Published, n => n.Text.Contains("ha canviat", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("spec", Spec + ": Detalle de la taquilla seleccionada (Taquilla ocupada)")]
    public async Task Cancelling_the_change_leaves_everything_as_it_was()
    {
        var home = await LoadedAsync(Home());
        home.Map.Select(_occupied);
        await Task.Delay(20);
        Action(home, "Change").Execute(null);

        home.Map.CancelPick();
        home.Map.Select(_free2);

        Assert.Null(home.Map.Picking);
        Assert.Empty(_changed);
        Assert.Equal(_free2, home.Map.SelectedLockerId); // an ordinary choice again
    }

    // --- The panel of students ---

    [Fact]
    [Trait("spec", Spec + ": Alumnos sin taquilla (Lista de alumnos)")]
    public async Task The_panel_lists_the_students_with_the_count_and_the_search_box_narrows_them()
    {
        var home = await LoadedAsync(Home());

        Assert.Equal(["Abad", "Zapata"], home.Students.Students.Select(s => s.LastName));
        Assert.Equal(2, home.Students.Count);

        home.Students.FilterText = "2N eso";
        Assert.Equal(["Zapata"], home.Students.Students.Select(s => s.LastName));
        Assert.Equal(2, home.Students.Count); // the count is of all, whatever the search hides
        home.Students.FilterText = "zz";
        Assert.True(home.Students.State.IsEmpty);
    }

    [Fact]
    [Trait("spec", Spec + ": Alumnos sin taquilla (Todos con taquilla)")]
    public async Task When_everyone_has_a_locker_the_panel_says_so()
    {
        _students = Result<StudentListing>.Success(new([], new(50, 50, 0)));
        var home = await LoadedAsync(Home());

        Assert.True(home.Students.State.IsEmpty);
        Assert.Contains("Tots els alumnes actius tenen taquilla", home.Students.State.Message);
    }

    [Fact]
    [Trait("spec", Spec + ": Alumnos sin taquilla (Sin curso activo)")]
    public async Task Without_an_active_year_the_panel_asks_to_activate_one_and_nothing_can_be_assigned()
    {
        _students = Result<StudentListing>.Failure(new Error("SchoolYears.NoActiveYear"));
        var home = await LoadedAsync(Home());
        home.Map.Select(_free);
        await Task.Delay(20);

        Assert.True(home.Students.NoActiveYear);
        Assert.Contains("Cal activar-ne un", home.Students.State.Message);
        Assert.Equal("No hi ha cap curs actiu.", Action(home, "Assign").UnavailableReason);
        Assert.Empty(_notifications.Published); // it is a state to explain, not an error to shout
    }

    [Fact]
    [Trait("spec", Spec + ": Alumnos sin taquilla (Lista de alumnos)")]
    public async Task A_chosen_student_who_is_assigned_meanwhile_is_no_longer_chosen()
    {
        var home = await LoadedAsync(Home());
        home.Students.Select(_waiting);

        _students = Result<StudentListing>.Success(new([new StudentRow(Guid.NewGuid(), "Aina", "Zapata", "2n ESO", "B", false, null)], new(50, 49, 1)));
        await home.Students.LoadAsync();

        Assert.Null(home.Students.SelectedStudentId);
        Assert.Equal(1, home.Students.Count);
    }

    // --- The views ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": Detalle de la taquilla seleccionada (Acciones no disponibles)")]
    public async Task The_detail_panel_shows_the_student_and_payment_and_its_disabled_actions_explain_themselves()
    {
        var home = await LoadedAsync(Home());
        var view = new LockerDetailView(home.Detail, _localizer);
        var window = new Window { Width = 400, Height = 600, Content = view };
        window.Show();
        home.Map.Select(_occupied);
        await Task.Delay(20);
        Dispatcher.UIThread.RunJobs();

        Assert.Contains("Marta Puig", view.Texts);
        Assert.Contains("2n ESO B", view.Texts);
        Assert.Contains(view.Texts, t => t.StartsWith("Pendent de pagament", StringComparison.Ordinal));
        Assert.DoesNotContain(view.Texts, t => t.Contains('@', StringComparison.Ordinal));
        var assign = view.ActionButtons.Single(b => Equals(b.Content, "Assigna l'alumne seleccionat"));
        Assert.False(assign.IsEffectivelyEnabled);
        Assert.Equal("La taquilla ja té un alumne.", ToolTip.GetTip(assign));
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Alumnos sin taquilla (Lista de alumnos)")]
    public async Task The_students_panel_shows_the_count_and_enter_puts_the_chosen_student_in_the_chosen_locker()
    {
        var home = await LoadedAsync(Home());
        var panel = new StudentsPanelView(home.Students, home.Drop, home.AssignToSelected, _localizer);
        var window = new Window { Width = 400, Height = 600, Content = panel };
        window.Show();
        await home.LoadAsync();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("2 alumnes sense taquilla", panel.CountText);
        Assert.Equal(2, panel.List.ItemCount);

        panel.List.SelectedIndex = 0;
        home.Map.Select(_free);
        Dispatcher.UIThread.RunJobs();
        panel.List.ContainerFromIndex(0)!.Focus();
        Dispatcher.UIThread.RunJobs();
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        await Task.Delay(50);

        Assert.Equal((_waiting, _free), (_assigned.Single().StudentId, _assigned.Single().LockerId));
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Detalle de la taquilla seleccionada (Taquilla ocupada)")]
    public async Task While_changing_a_locker_the_map_shows_a_banner_that_asks_for_the_new_one_and_can_be_cancelled()
    {
        var home = await LoadedAsync(Home());
        var view = new LockerMapView(home.Map, _localizer, home.Drop);
        var window = new Window { Width = 1000, Height = 700, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.False(view.PickBanner.IsVisible);

        home.Map.BeginPick(_holder, "Marta Puig");
        Dispatcher.UIThread.RunJobs();

        Assert.True(view.PickBanner.IsVisible);
        Assert.Contains("Marta Puig", ((TextBlock)((DockPanel)view.PickBanner.Child!).Children.OfType<TextBlock>().Single()).Text);
        home.Map.CancelPick();
        Dispatcher.UIThread.RunJobs();
        Assert.False(view.PickBanner.IsVisible);
    }
}
