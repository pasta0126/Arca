// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Assignments.CheckAssignmentTarget;
using Arca.Application.Localization;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Zones.ListZoneRows;
using Arca.Testing;
using Arca.Application.Preferences;
using Arca.Application.Search;
using Arca.Application.Students;
using Arca.Domain.Common;
using Arca.UI.Actions;
using Arca.UI.Lists;
using Arca.UI.Lockers;
using Arca.UI.Map;
using Arca.UI.Notifications;
using Arca.UI.Preferences;
using Arca.UI.Screens;
using Arca.UI.Shell;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Arca.UI.Tests;

/// <summary>The map view of the Lockers section (pantalles-taquilles-i-zones), over the same world as the list.</summary>
public sealed partial class LockersScreenTests
{
    const string MapSpec = "ui-llistats-i-detall/pantalles-taquilles-i-zones";

    sealed class MemoryPreferences : IUiPreferencesStore
    {
        public UiPreferences Saved { get; private set; } = new();

        public UiPreferences Load() => Saved;

        public void Save(UiPreferences preferences) => Saved = preferences;
    }

    readonly List<AssignLockerRequest> _assigned = [];
    readonly List<AssignLockerRequest> _changed = [];
    readonly MemoryPreferences _preferences = new();
    Result<StudentListing> _waiting = Result<StudentListing>.Success(new(
        [new StudentRow(_waitingId, "Pau", "Abad", "1r ESO", "A", false, null), new StudentRow(Guid.NewGuid(), "Aina", "Zapata", "2n ESO", "B", false, null)], new(50, 48, 2)));

    static readonly Guid _waitingId = Guid.NewGuid();
    static readonly Guid _holder = Guid.NewGuid();

    static AssignmentRow Assigned(AssignLockerRequest r) => new(
        Guid.NewGuid(), r.StudentId, "Pau Abad", r.LockerId, 1, "Planta 1", "2026-2027", DateTimeOffset.UtcNow, null, null, null);

    LockerAssignmentServices AssignmentServices() => new(
        _ => Task.FromResult(_waiting),
        (_, locker, _) => Task.FromResult(Result<AssignmentTargetCheck>.Success(
            _rows.Single(r => r.Id == locker).Status == LockerStatusView.Free ? new AssignmentTargetCheck(null, []) : new AssignmentTargetCheck(new Error("Assignments.LockerUnavailable"), []))),
        (r, _) =>
        {
            _assigned.Add(r);
            return Task.FromResult(Result<AssignLockerResult>.Success(new AssignLockerResult(Assigned(r), [])));
        },
        (r, _) =>
        {
            _changed.Add(r);
            return Task.FromResult(Result<AssignLockerResult>.Success(new AssignLockerResult(Assigned(r), [])));
        });

    LockersMapModel Map(LockersViewModel model, RecordingConfirmations? confirmations = null) => new(
        model, AssignmentServices(), new ResultNotifier(_notifications, _localizer, _log), confirmations ?? _confirmations, _localizer, _notifications, _log, _delay);

    /// <summary>A zone with five lockers: free, free, occupied with debt, reserved and broken.</summary>
    (ZoneRow Zone, LockerListRow Free, LockerListRow Free2, LockerListRow Occupied, LockerListRow Reserved, LockerListRow Broken) Sample()
    {
        var zone = AddZone("Planta 1");
        return (zone, AddLocker(1, zone), AddLocker(2, zone), AddLocker(3, zone, LockerStatusView.Occupied, "Marta Puig", debt: true, studentId: _holder),
            AddLocker(4, zone, LockerStatusView.Reserved), AddLocker(5, zone, LockerStatusView.Broken));
    }

    async Task<(LockersViewModel Model, LockersMapModel Map)> LoadedAsync(RecordingConfirmations? confirmations = null)
    {
        var model = Model();
        var map = Map(model, confirmations);
        await model.LoadAsync();
        await map.LoadAsync();
        return (model, map);
    }

    static AppAction Action(LockersViewModel model, string id) => model.Detail.Actions.Single(a => a.Id == id);

    async Task<LockersMapView> ShownAsync(LockersViewModel model, LockersMapModel? map = null)
    {
        var view = new LockersMapView(model, _localizer, new UiPreferencesSession(_preferences), map?.Drop);
        var window = new Window { Width = 1000, Height = 700, Content = view };
        window.Show();
        await model.LoadAsync();
        Dispatcher.UIThread.RunJobs();
        return view;
    }

    // --- The map as a view of the list ---

    [AvaloniaFact]
    [Trait("spec", MapSpec + ": Vista de mapa de taquillas (Mapa por zonas, Estado visible)")]
    public async Task The_map_draws_each_zone_with_its_lockers_by_status_with_the_debt_mark_and_the_counters_filter()
    {
        var sample = Sample();
        AddLocker(10, AddZone("Planta 2"));
        var model = Model();
        var view = await ShownAsync(model);

        Assert.Equal(6, view.Cells.Count);
        Assert.Equal(["Planta 1", "Planta 2"], view.Sections.Values.Select(s => s.Title));
        Assert.Equal(5, view.Chips.Count); // one per status
        Assert.Contains(view.Chips[0].GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Lliure: 3");
        Assert.Equal("Taquilla 3 · Ocupada · Marta Puig · Pendent de pagament", ToolTip.GetTip(view.Cells[sample.Occupied.Id]));
        Assert.Equal("Taquilla 1 · Lliure", ToolTip.GetTip(view.Cells[sample.Free.Id]));
        var icons = view.Cells.ToDictionary(c => c.Key, c => c.Value.GetVisualDescendants().OfType<Material.Icons.Avalonia.MaterialIcon>().Count());
        Assert.Equal(2, icons[sample.Occupied.Id]); // its status icon and the debt mark
        Assert.Equal(1, icons[sample.Free.Id]);

        view.Chips[3].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Primitives.ToggleButton.ClickEvent)); // broken
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Broken", model.StatusFilter);
        Assert.Equal(new[] { sample.Broken.Id }, view.Cells.Keys);
        Assert.Equal(6, model.Counters.Active); // the counters describe all the lockers, not the filter
    }

    [AvaloniaFact]
    [Trait("spec", MapSpec + ": Vista de mapa de taquillas (Mapa por zonas)")]
    public async Task The_zones_of_the_map_are_told_apart_with_room_between_them()
    {
        Sample();
        AddLocker(10, AddZone("Planta 2"));
        var view = await ShownAsync(Model());

        var stack = view.GetVisualDescendants().OfType<StackPanel>().First(p => p.Children.OfType<Arca.UI.Layout.CollapsibleSectionView>().Count() == 2);

        Assert.True(stack.Spacing >= 16, $"the zones are {stack.Spacing} apart");
    }

    [AvaloniaFact]
    [Trait("spec", MapSpec + ": Vista de mapa de taquillas (Zona desactivada)")]
    public async Task A_zone_that_is_not_in_use_is_not_drawn()
    {
        var sample = Sample();
        var dead = AddZone("Antic magatzem", active: false);
        var hidden = AddLocker(20, dead);
        _rows[_rows.IndexOf(hidden)] = hidden with { ZoneActive = false };
        var view = await ShownAsync(Model());

        Assert.Equal(["Planta 1"], view.Sections.Values.Select(s => s.Title));
        Assert.DoesNotContain(hidden.Id, view.Cells.Keys);
        Assert.Contains(sample.Free.Id, view.Cells.Keys);
    }

    [Fact]
    [Trait("spec", MapSpec + ": Vista de mapa de taquillas (Filtrar por estado)")]
    public async Task Filtering_by_zone_and_by_status_narrows_the_rows_and_a_filter_that_leaves_nothing_offers_to_clear_it()
    {
        Sample();
        var other = AddZone("Planta 2");
        AddLocker(10, other);
        var (model, _) = await LoadedAsync();

        model.ZoneFilter = other.Id.ToString();
        Assert.Equal([10], model.Lockers.List.Rows.Select(r => r.Number));
        model.StatusFilter = "Broken"; // Planta 2 has none broken
        Assert.Equal(ListViewState.NoResults, model.Lockers.State.State);
        model.Lockers.State.Actions[0].Command.Execute(null);

        Assert.Equal((string.Empty, string.Empty), (model.StatusFilter, model.ZoneFilter));
        Assert.Equal(6, model.Lockers.List.Rows.Count);
    }

    [AvaloniaFact]
    [Trait("spec", MapSpec + ": Organización de la sección Taquillas (Cambiar de vista con filtros)")]
    public async Task The_map_and_the_list_share_the_filters_the_search_and_the_chosen_locker()
    {
        var sample = Sample();
        var model = Model();
        var map = Map(model);
        var mapScreen = LockersView.CreateMap(model, map, _localizer, new UiPreferencesSession(_preferences));
        var listScreen = LockersView.Create(model, _localizer);
        var window = new Window { Width = 1200, Height = 700, Content = mapScreen };
        window.Show();
        await Task.Delay(50);
        Dispatcher.UIThread.RunJobs();

        model.StatusFilter = "Broken"; // filtered on the map…
        model.SelectLocker(sample.Broken.Id);
        window.Content = listScreen; // …and then the list
        Dispatcher.UIThread.RunJobs();

        var list = listScreen.GetVisualDescendants().OfType<ScreenListView<LockerListRow, Guid>>().Single();
        Assert.Equal([sample.Broken.Number], model.Lockers.List.Rows.Select(r => r.Number));
        Assert.Equal(["Estat: Fora de servei"], list.TagsPanel.Children.OfType<Button>().Select(b => ((string?)b.Content ?? string.Empty).Replace(" ✕", string.Empty, StringComparison.Ordinal)).Select(t => t.Replace("Avariada", "Fora de servei", StringComparison.Ordinal)));
        Assert.Equal(sample.Broken.Id, model.Lockers.Current!.Id);
        Assert.Equal(sample.Broken.Id, ((LockerListRow?)list.Rows.List.SelectedItem)?.Id);
    }

    [AvaloniaFact]
    [Trait("spec", MapSpec + ": Vista de mapa de taquillas (Esc en el mapa)")]
    public async Task Esc_on_the_map_lets_go_of_the_locker_chosen_and_the_detail_goes_back_to_choose_one()
    {
        var sample = Sample();
        var model = Model();
        var map = Map(model);
        var screen = LockersView.CreateMap(model, map, _localizer, new UiPreferencesSession(_preferences));
        var window = new Window { Width = 1200, Height = 700, Content = screen };
        window.Show();
        await Task.Delay(50);
        Dispatcher.UIThread.RunJobs();
        var view = screen.GetVisualDescendants().OfType<LockersMapView>().Single();
        view.Cells[sample.Free.Id].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(50);
        Assert.NotNull(model.Detail.Detail);

        view.Toolbar.Search.Focus(); // the search is empty, so Esc goes on to the locker
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        await Task.Delay(50);
        Dispatcher.UIThread.RunJobs();

        Assert.False(model.Lockers.HasSelection);
        Assert.Null(model.Detail.Detail);
    }

    [AvaloniaFact]
    [Trait("spec", MapSpec + ": Vista de mapa de taquillas (Mapa por zonas)")]
    public async Task Clicking_a_locker_chooses_it_and_outlines_it_and_the_reset_button_is_in_the_toolbar()
    {
        var sample = Sample();
        var model = Model();
        var view = await ShownAsync(model);

        view.Cells[sample.Occupied.Id].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(sample.Occupied.Id, model.Lockers.Current!.Id);
        Assert.NotSame(view.Cells[sample.Free.Id].BorderBrush, view.Cells[sample.Occupied.Id].BorderBrush);
        model.StatusFilter = "Free";
        Assert.True(view.Toolbar.ResetButton.IsEffectivelyEnabled);
        view.Toolbar.ResetButton.Command!.Execute(null);
        Assert.Equal(string.Empty, model.StatusFilter);
    }

    [Fact]
    [Trait("spec", "ui-llistats-i-detall/pantalla-principal: Inicio como pantalla registrable (Resumen mínimo)")]
    public async Task A_card_of_the_start_shows_the_map_and_the_list_with_exactly_its_status()
    {
        Sample();
        var (model, _) = await LoadedAsync();
        model.Lockers.List.FilterText = "zzz";
        model.NumberFilter = "3";

        model.ApplyRequest(new Dictionary<string, string> { ["Status"] = "Free" });

        Assert.Equal((string.Empty, string.Empty, "Free"), (model.Lockers.List.FilterText, model.NumberFilter, model.StatusFilter));
        Assert.Equal([1, 2], model.Lockers.List.Rows.Select(r => r.Number));
    }

    [Fact]
    [Trait("spec", "filtres-i-targetes/targetes-d-inici: Una tarjeta es un filtro guardado (Tarjeta de taquillas libres)")]
    public async Task The_status_and_zone_on_come_back_as_the_criteria_of_a_card_and_are_the_same_for_the_map_and_the_list()
    {
        Sample();
        var other = AddZone("Planta 2");
        AddLocker(10, other);
        var (model, _) = await LoadedAsync();
        Assert.Empty(model.CurrentCardCriteria);

        model.StatusFilter = "Free";
        model.ZoneFilter = other.Id.ToString();
        model.NumberFilter = "10"; // what is typed in the number box is not a criterion of a card

        Assert.Equal(new Dictionary<string, string> { ["Status"] = "Free", ["Zone"] = other.Id.ToString() }, model.CurrentCardCriteria);
        var counted = model.Lockers.List.AllRows.Count(Arca.Application.Home.LockerCardFilter.From(model.CurrentCardCriteria).Matches);
        model.NumberFilter = string.Empty;
        Assert.Equal(counted, model.Lockers.List.Rows.Count);
    }

    // --- One locker changes ---

    [Fact]
    [Trait("spec", MapSpec + ": Vista de mapa de taquillas (Actualización tras un cambio)")]
    public async Task After_a_change_only_that_locker_is_read_again_and_the_counters_follow_without_loading_them_all()
    {
        var sample = Sample();
        var (model, _) = await LoadedAsync();
        Assert.Equal(1, _listLoads);
        _rows[_rows.IndexOf(sample.Free)] = sample.Free with { Status = LockerStatusView.Occupied, StudentName = "Pau Abad" };
        _holders[sample.Free.Id] = Guid.NewGuid();
        _detailReads.Clear();

        await model.RefreshLockerAsync(sample.Free.Id);

        Assert.Equal(1, _listLoads); // the lockers were not loaded again
        Assert.Equal([sample.Free.Id], _detailReads);
        Assert.Equal(LockerStatusView.Occupied, model.Find(sample.Free.Id)!.Status);
        Assert.Equal((1, 2), (model.Counters.Free, model.Counters.Occupied));
    }

    [Fact]
    [Trait("spec", MapSpec + ": Vista de mapa de taquillas (Actualización tras un cambio)")]
    public async Task A_locker_that_leaves_service_leaves_the_counters()
    {
        var sample = Sample();
        var (model, _) = await LoadedAsync();
        _rows[_rows.IndexOf(sample.Broken)] = sample.Broken with { Status = LockerStatusView.Retired };

        await model.RefreshLockerAsync(sample.Broken.Id);

        Assert.Equal(4, model.Counters.Active);
        Assert.Equal(0, model.Counters.Broken);
    }

    // --- Revealing a locker the search chose ---

    [AvaloniaFact]
    [Trait("spec", MapSpec + ": Vista de mapa de taquillas (Resaltar una búsqueda)")]
    public async Task Revealing_a_locker_outlines_it_clears_what_hides_it_unfolds_its_zone_and_opens_its_detail()
    {
        var sample = Sample();
        var model = Model();
        var view = await ShownAsync(model);
        model.StatusFilter = "Free";
        view.Sections.Values.Single().IsExpanded = false;

        model.Reveal(sample.Occupied.Id); // an occupied locker, hidden by the filter, in a folded zone
        await Task.Delay(50);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(sample.Occupied.Id, model.HighlightedLockerId);
        Assert.Equal(sample.Occupied.Id, model.Lockers.Current!.Id);
        Assert.Equal(string.Empty, model.StatusFilter);
        Assert.True(view.Sections.Values.Single().IsExpanded);
        Assert.Equal("Marta Puig", model.Detail.Detail!.Row.StudentName);
    }

    [Fact]
    [Trait("spec", MapSpec + ": Vista de mapa de taquillas (Resaltar una búsqueda)")]
    public async Task Revealing_a_locker_that_is_not_there_does_nothing()
    {
        Sample();
        var (model, _) = await LoadedAsync();

        model.Reveal(Guid.NewGuid());

        Assert.Null(model.HighlightedLockerId);
        Assert.False(model.Lockers.HasSelection);
    }

    [Fact]
    [Trait("spec", MapSpec + ": Vista de mapa de taquillas (Resaltar una búsqueda)")]
    public async Task Choosing_a_locker_in_the_search_while_changing_a_locker_ends_the_change_first()
    {
        var sample = Sample();
        var (model, _) = await LoadedAsync();
        model.BeginPick(_holder, "Marta Puig");

        model.Reveal(sample.Free.Id);

        Assert.Null(model.Picking); // the person moved on: the next click must not reassign the student
        Assert.Equal(sample.Free.Id, model.Lockers.Current!.Id);
    }

    // --- The failures ---

    [Fact]
    [Trait("spec", MapSpec + ": Vista de mapa de taquillas (300 taquillas)")]
    public async Task A_failure_while_loading_or_reading_one_locker_tells_the_person_without_any_student_data()
    {
        var sample = Sample();
        var model = Model();
        _listCrash = new IOException("disk failed for Marta Puig");
        await model.LoadAsync();
        _listCrash = null;
        await model.LoadAsync();
        _detailCrash = new IOException("disk failed for Marta Puig");

        await model.RefreshLockerAsync(sample.Free.Id);

        Assert.Equal(["ListLoad", "RefreshLocker"], _log.Entries.Select(e => e.Context));
        Assert.All(_notifications.Published, n => Assert.DoesNotContain("Marta", n.Text, StringComparison.Ordinal));
    }

    // --- The detail and its actions ---

    [Fact]
    [Trait("spec", MapSpec + ": Detalle de la taquilla (Taquilla ocupada)")]
    public async Task An_occupied_locker_offers_release_and_change_and_explains_why_the_rest_are_not_available()
    {
        var sample = Sample();
        var (model, _) = await LoadedAsync();

        model.SelectLocker(sample.Occupied.Id);
        await Task.Delay(50);

        Assert.Equal("Marta Puig", model.Detail.Detail!.Row.StudentName);
        Assert.True(Action(model, "Release").IsAvailable);
        Assert.True(Action(model, "Change").IsAvailable);
        Assert.False(Action(model, "Reserve").IsAvailable);
        Assert.Equal(_localizer.Message(new Error("Lockers.NotFree")), Action(model, "Reserve").UnavailableReason);

        model.SelectLocker(sample.Free.Id);
        await Task.Delay(50);
        Assert.False(Action(model, "Release").IsAvailable);
        Assert.False(Action(model, "Change").IsAvailable);
        Assert.True(Action(model, "Reserve").IsAvailable);
    }

    [Fact]
    [Trait("spec", MapSpec + ": Detalle de la taquilla (Taquilla ocupada)")]
    public async Task Releasing_asks_for_confirmation_then_frees_the_locker_and_reads_again_only_that_locker()
    {
        var sample = Sample();
        var (model, _) = await LoadedAsync();
        model.SelectLocker(sample.Occupied.Id);
        await Task.Delay(50);
        _calls.Clear();
        _detailReads.Clear();
        var refreshes = _refreshes;

        Action(model, "Release").Execute(null);
        await Task.Delay(100);

        var asked = Assert.Single(_confirmations.Asked);
        Assert.Contains("Marta Puig", asked.Title, StringComparison.Ordinal);
        Assert.Contains($"release {_holder}", _calls);
        Assert.Equal([sample.Occupied.Id], _detailReads.Distinct()); // only that locker
        Assert.Equal(1, _listLoads); // the lockers were not loaded again
        Assert.Equal(refreshes + 1, _refreshes); // and the global state was, because payments may have changed
        Assert.Equal(LockerStatusView.Free, model.Find(sample.Occupied.Id)!.Status);
        Assert.Equal((0, 3), (model.Counters.Occupied, model.Counters.Free));
    }

    [Fact]
    [Trait("spec", MapSpec + ": Detalle de la taquilla (Taquilla ocupada)")]
    public async Task Declining_the_release_confirmation_changes_nothing()
    {
        var sample = Sample();
        var declined = new RecordingConfirmations(false);
        _confirmations = declined;
        var (model, _) = await LoadedAsync();
        model.SelectLocker(sample.Occupied.Id);
        await Task.Delay(50);

        Action(model, "Release").Execute(null);
        await Task.Delay(50);

        Assert.Single(declined.Asked);
        Assert.DoesNotContain(_calls, c => c.StartsWith("release", StringComparison.Ordinal));
    }

    // --- Assigning: from the detail, the menu of the list and dragging ---

    [Fact]
    [Trait("spec", MapSpec + ": Panel de alumnos sin taquilla en el mapa (Asignar arrastrando)")]
    public async Task Assigning_from_the_detail_the_list_menu_and_dragging_all_send_the_same_request_and_update_the_same_way()
    {
        var sample = Sample();
        var (model, map) = await LoadedAsync();
        model.SelectLocker(sample.Free.Id);
        map.Students.Select(_waitingId);
        await Task.Delay(50);

        Action(model, "Assign").Execute(null); // from the detail of the locker: the student chosen in the panel
        await Task.Delay(80);
        map.AssignToSelected.Execute(null); // from the menu of the list
        await Task.Delay(80);
        map.Drop.BeginDrag(_waitingId); // by dragging
        await map.Drop.DropAsync(sample.Free.Id);
        await Task.Delay(80);

        Assert.Equal(3, _assigned.Count);
        Assert.All(_assigned, r => Assert.Equal((_waitingId, sample.Free.Id, false), (r.StudentId, r.LockerId, r.ConfirmWarnings)));
        Assert.Equal(3, _notifications.Published.Count(n => n.Kind == Arca.Application.Feedback.NotificationKind.Success));
        Assert.Equal(1, _listLoads); // never all the lockers again
    }

    [Fact]
    [Trait("spec", MapSpec + ": Panel de alumnos sin taquilla en el mapa (Asignar arrastrando)")]
    public async Task The_list_menu_action_needs_a_chosen_student_and_a_chosen_free_locker()
    {
        var sample = Sample();
        var (model, map) = await LoadedAsync();

        Assert.Equal("Tria primer un alumne al panell d'alumnes sense taquilla.", map.AssignToSelected.UnavailableReason);
        map.Students.Select(_waitingId);
        Assert.Equal("Tria una taquilla lliure al mapa.", map.AssignToSelected.UnavailableReason);
        model.SelectLocker(sample.Occupied.Id);
        Assert.False(map.AssignToSelected.IsAvailable);
        model.SelectLocker(sample.Free.Id);
        Assert.True(map.AssignToSelected.IsAvailable);
    }

    // --- Changing the locker of a student ---

    [Fact]
    [Trait("spec", MapSpec + ": Detalle de la taquilla (Taquilla ocupada)")]
    public async Task Changing_asks_for_the_new_locker_on_the_map_ignores_taken_ones_and_moves_the_student_to_the_free_one_chosen()
    {
        var sample = Sample();
        var (model, _) = await LoadedAsync();
        model.SelectLocker(sample.Occupied.Id);
        await Task.Delay(50);

        Action(model, "Change").Execute(null);
        Assert.Equal("Marta Puig", model.Picking!.Value.StudentName);
        model.SelectLocker(sample.Broken.Id); // not free: ignored, still choosing
        Assert.NotNull(model.Picking);
        Assert.Empty(_changed);

        _rows[_rows.IndexOf(sample.Occupied)] = sample.Occupied with { Status = LockerStatusView.Free, StudentName = null, HasDebt = false }; // what the change will have done
        _rows[_rows.IndexOf(sample.Free2)] = sample.Free2 with { Status = LockerStatusView.Occupied, StudentName = "Marta Puig" };
        _holders.Remove(sample.Occupied.Id);
        _holders[sample.Free2.Id] = _holder;
        model.SelectLocker(sample.Free2.Id);
        await Task.Delay(120);

        Assert.Null(model.Picking);
        var request = Assert.Single(_changed);
        Assert.Equal((_holder, sample.Free2.Id), (request.StudentId, request.LockerId));
        Assert.Equal(1, _listLoads);
        Assert.Equal(LockerStatusView.Occupied, model.Find(sample.Free2.Id)!.Status);
        Assert.Equal(LockerStatusView.Free, model.Find(sample.Occupied.Id)!.Status); // the old locker changed too
        Assert.Contains(_notifications.Published, n => n.Text.Contains("ha canviat", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("spec", MapSpec + ": Detalle de la taquilla (Taquilla ocupada)")]
    public async Task Cancelling_the_change_leaves_everything_as_it_was_and_the_next_click_is_an_ordinary_choice()
    {
        var sample = Sample();
        var (model, _) = await LoadedAsync();
        model.SelectLocker(sample.Occupied.Id);
        await Task.Delay(50);
        Action(model, "Change").Execute(null);

        model.CancelPick();
        model.SelectLocker(sample.Free2.Id);

        Assert.Null(model.Picking);
        Assert.Empty(_changed);
        Assert.Equal(sample.Free2.Id, model.Lockers.Current!.Id);
    }

    [Fact]
    [Trait("spec", MapSpec + ": Detalle de la taquilla (Taquilla ocupada)")]
    public async Task Choosing_the_locker_for_the_change_takes_the_person_to_the_map()
    {
        var sample = Sample();
        var opened = 0;
        var model = new LockersViewModel(
            Services(), Context(), new ActionRegistry(_localizer, UiPlatform.Windows)[StandardActions.New], () => Task.CompletedTask,
            new Arca.UI.Assigning.AssignmentDialogs(null!, Context()), () =>
            {
                opened++;
                return Task.CompletedTask;
            });
        await model.LoadAsync();
        model.SelectLocker(sample.Occupied.Id);
        await Task.Delay(50);

        Action(model, "Change").Execute(null);
        await Task.Delay(20);

        Assert.Equal(1, opened);
        Assert.NotNull(model.Picking);
    }

    [AvaloniaFact]
    [Trait("spec", MapSpec + ": Detalle de la taquilla (Taquilla ocupada)")]
    public async Task While_changing_a_locker_the_map_shows_a_banner_that_asks_for_the_new_one_and_can_be_cancelled()
    {
        Sample();
        var model = Model();
        var view = await ShownAsync(model);
        Assert.False(view.PickBanner.IsVisible);

        model.BeginPick(_holder, "Marta Puig");
        Dispatcher.UIThread.RunJobs();

        Assert.True(view.PickBanner.IsVisible);
        Assert.Contains("Marta Puig", ((TextBlock)((DockPanel)view.PickBanner.Child!).Children.OfType<TextBlock>().Single()).Text, StringComparison.Ordinal);
        model.CancelPick();
        Dispatcher.UIThread.RunJobs();
        Assert.False(view.PickBanner.IsVisible);
    }

    [Fact]
    [Trait("spec", MapSpec + ": Detalle de la taquilla (Taquilla ocupada)")]
    public async Task A_failure_of_the_change_outside_its_command_is_told_to_the_person()
    {
        var sample = Sample();
        var (model, _) = await LoadedAsync();
        model.OnPicked = _ => throw new InvalidOperationException("boom");
        model.BeginPick(Guid.NewGuid(), "Marta Puig");

        model.SelectLocker(sample.Free.Id);
        await Task.Delay(40);

        Assert.Equal("ChangeLocker", Assert.Single(_log.Entries).Context);
        Assert.Equal(Arca.Application.Feedback.NotificationKind.Error, Assert.Single(_notifications.Published).Kind);
    }

    // --- The panel of students ---

    [Fact]
    [Trait("spec", MapSpec + ": Panel de alumnos sin taquilla en el mapa (Lista de alumnos)")]
    public async Task The_panel_lists_the_students_with_the_count_and_the_search_box_narrows_them()
    {
        var (_, map) = await LoadedAsync();

        Assert.Equal(["Abad", "Zapata"], map.Students.Students.Select(s => s.LastName));
        Assert.Equal(2, map.Students.Count);

        map.Students.FilterText = "2N eso";
        Assert.Equal(["Zapata"], map.Students.Students.Select(s => s.LastName));
        Assert.Equal(2, map.Students.Count); // the count is of all, whatever the search hides
        map.Students.FilterText = "zz";
        Assert.True(map.Students.State.IsEmpty);
    }

    [Fact]
    [Trait("spec", MapSpec + ": Panel de alumnos sin taquilla en el mapa (Todos con taquilla)")]
    public async Task When_everyone_has_a_locker_the_panel_says_so()
    {
        _waiting = Result<StudentListing>.Success(new([], new(50, 50, 0)));
        var (_, map) = await LoadedAsync();

        Assert.True(map.Students.State.IsEmpty);
        Assert.Contains("Tots els alumnes actius tenen taquilla", map.Students.State.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", MapSpec + ": Panel de alumnos sin taquilla en el mapa (Sin curso activo)")]
    public async Task Without_an_active_year_the_panel_asks_to_activate_one_and_says_it_as_a_state_and_not_as_an_error()
    {
        _waiting = Result<StudentListing>.Failure(new Error("SchoolYears.NoActiveYear"));
        var (_, map) = await LoadedAsync();

        Assert.True(map.Students.NoActiveYear);
        Assert.Contains("Cal activar-ne un", map.Students.State.Message, StringComparison.Ordinal);
        Assert.Equal("No hi ha cap curs actiu.", map.AssignToSelected.UnavailableReason);
        Assert.Empty(_notifications.Published);
    }

    [Fact]
    [Trait("spec", MapSpec + ": Panel de alumnos sin taquilla en el mapa (Lista de alumnos)")]
    public async Task A_chosen_student_who_is_assigned_meanwhile_is_no_longer_chosen()
    {
        var (_, map) = await LoadedAsync();
        map.Students.Select(_waitingId);

        _waiting = Result<StudentListing>.Success(new([new StudentRow(Guid.NewGuid(), "Aina", "Zapata", "2n ESO", "B", false, null)], new(50, 49, 1)));
        await map.Students.LoadAsync();

        Assert.Null(map.Students.SelectedStudentId);
        Assert.Equal(1, map.Students.Count);
    }

    [AvaloniaFact]
    [Trait("spec", MapSpec + ": Panel de alumnos sin taquilla en el mapa (Lista de alumnos)")]
    public async Task The_students_panel_shows_the_count_and_enter_puts_the_chosen_student_in_the_chosen_locker()
    {
        var sample = Sample();
        var (model, map) = await LoadedAsync();
        var panel = new StudentsPanelView(map.Students, map.Drop, map.AssignToSelected, _localizer);
        var window = new Window { Width = 400, Height = 600, Content = panel };
        window.Show();
        await map.LoadAsync();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("2 alumnes sense taquilla", panel.CountText);
        Assert.Equal(2, panel.List.ItemCount);

        panel.List.SelectedIndex = 0;
        model.SelectLocker(sample.Free.Id);
        Dispatcher.UIThread.RunJobs();
        panel.List.ContainerFromIndex(0)!.Focus();
        Dispatcher.UIThread.RunJobs();
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        await Task.Delay(80);

        Assert.Equal((_waitingId, sample.Free.Id), (_assigned.Single().StudentId, _assigned.Single().LockerId));
    }

    [AvaloniaFact]
    [Trait("spec", MapSpec + ": Detalle de la taquilla (Acciones no disponibles)")]
    public async Task The_detail_panel_shows_the_student_and_payment_and_its_disabled_actions_explain_themselves()
    {
        var sample = Sample();
        var (model, _) = await LoadedAsync();
        var view = new LockerDetailPanel(model, _localizer);
        var window = new Window { Width = 400, Height = 600, Content = view };
        window.Show();
        model.SelectLocker(sample.Occupied.Id);
        await Task.Delay(50);
        Dispatcher.UIThread.RunJobs();

        var texts = view.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Empty).ToList();
        Assert.Contains(texts, t => t.Contains("Marta Puig", StringComparison.Ordinal));
        Assert.Contains(texts, t => t.Contains("pagaments pendents", StringComparison.Ordinal));
        Assert.DoesNotContain(texts, t => t.Contains('@', StringComparison.Ordinal));
        var reserve = view.ActionButtons.Single(b => Equals(b.Content, "Reserva"));
        Assert.False(reserve.IsEffectivelyEnabled);
        Assert.False(string.IsNullOrEmpty((string?)ToolTip.GetTip(reserve)));
    }

    // --- Another answer arrives late ---

    [Fact]
    [Trait("spec", MapSpec + ": Vista de mapa de taquillas (Actualización tras un cambio)")]
    public async Task A_detail_that_arrives_late_does_not_replace_the_locker_chosen_afterwards()
    {
        var sample = Sample();
        var (model, _) = await LoadedAsync();
        var slow = new TaskCompletionSource();
        var services = Services() with
        {
            Detail = async (id, ct) =>
            {
                if (id == sample.Free.Id)
                {
                    await slow.Task;
                }

                var row = _rows.Single(r => r.Id == id);
                return Result<Arca.Application.Lockers.GetLockerScreen.LockerScreenDetail>.Success(new(row, null, null, null, null, null, null, null));
            },
        };
        var late = new LockersViewModel(services, Context(), new ActionRegistry(_localizer, UiPlatform.Windows)[StandardActions.New], () => Task.CompletedTask, new Arca.UI.Assigning.AssignmentDialogs(null!, Context()));
        await late.LoadAsync();

        late.SelectLocker(sample.Free.Id); // the person clicks A…
        late.SelectLocker(sample.Free2.Id); // …and then B, which answers at once
        await Task.Delay(50);
        slow.SetResult(); // A's answer arrives late
        await Task.Delay(50);

        Assert.Equal(sample.Free2.Id, late.Detail.Detail!.Row.Id);
    }

    [AvaloniaFact]
    public async Task Screenshot_of_the_lockers_map()
    {
        var zone = AddZone("Planta baixa");
        var other = AddZone("Planta 1");
        var random = new Random(3);
        for (var i = 1; i <= 60; i++)
        {
            var status = random.Next(10) switch { < 5 => LockerStatusView.Occupied, < 8 => LockerStatusView.Free, 8 => LockerStatusView.Reserved, _ => LockerStatusView.Broken };
            AddLocker(i, i <= 36 ? zone : other, status, status == LockerStatusView.Occupied ? "Marta Puig" : null, debt: status == LockerStatusView.Occupied && random.Next(4) == 0);
        }

        var model = Model();
        var map = Map(model);
        var screen = LockersView.CreateMap(model, map, _localizer, new UiPreferencesSession(_preferences));
        var window = new Window { Width = 1180, Height = 700, Content = screen };
        window.Show();
        await model.LoadAsync();
        await map.LoadAsync();
        model.Reveal(_rows[9].Id);
        await Task.Delay(100);
        Dispatcher.UIThread.RunJobs();
        ScreenshotTests.Take(window, "lockers-map");
        window.Close();
    }
}
