// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.LockerMap;
using Arca.Application.Localization;
using Arca.Application.Lockers;
using Arca.Application.Preferences;
using Arca.Application.Search;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Lists;
using Arca.UI.Map;
using Arca.UI.Notifications;
using Arca.UI.Preferences;
using Arca.UI.Search;
using Arca.UI.Shell;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Arca.UI.Tests;

public sealed class LockerMapUiTests
{
    const string Spec = "ui-shell/pantalla-principal";

    sealed class MemoryStore : IUiPreferencesStore
    {
        public UiPreferences Saved { get; private set; } = new();

        public UiPreferences Load() => Saved;

        public void Save(UiPreferences preferences) => Saved = preferences;
    }

    static readonly Guid _zoneOne = Guid.NewGuid();
    static readonly Guid _zoneTwo = Guid.NewGuid();
    static readonly Guid _free = Guid.NewGuid();
    static readonly Guid _occupied = Guid.NewGuid();
    static readonly Guid _broken = Guid.NewGuid();
    static readonly Guid _other = Guid.NewGuid();

    readonly ResxLocalizer _localizer = new();
    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    readonly MemoryStore _store = new();
    int _loads;
    Func<Guid, MapLocker?> _reread = _ => null;
    LockerMapData _map = Sample();
    Exception? _crash;

    static LockerMapData Sample()
    {
        var one = new List<MapLocker>
        {
            new(_free, 1, LockerStatusView.Free, null, null, false),
            new(_occupied, 2, LockerStatusView.Occupied, Guid.NewGuid(), "Marta Puig", true),
            new(_broken, 3, LockerStatusView.Broken, null, null, false),
        };
        var two = new List<MapLocker> { new(_other, 10, LockerStatusView.Free, null, null, false) };
        ZoneMap Zone(Guid id, string name, List<MapLocker> l) => new(id, name, l, GetLockerMapHandler.Count(l));
        var zones = new List<ZoneMap> { Zone(_zoneOne, "Planta 1", one), Zone(_zoneTwo, "Planta 2", two) };
        return new LockerMapData(zones, GetLockerMapHandler.Count(zones.SelectMany(z => z.Lockers)));
    }

    LockerMapViewModel Model() => new(
        _ =>
        {
            _loads++;
            return _crash is not null ? throw _crash : Task.FromResult(Result<LockerMapData>.Success(_map));
        },
        (id, _) => Task.FromResult(Result<MapLocker?>.Success(_reread(id))),
        new UiPreferencesSession(_store), new ResultNotifier(_notifications, _localizer, _log), _localizer);

    // --- The model ---

    [Fact]
    [Trait("spec", Spec + ": Mapa de taquillas por zona (Mapa por zonas)")]
    public async Task Loading_shows_the_zones_with_their_lockers_and_the_counts_by_status()
    {
        var model = Model();
        Assert.True(model.State.IsLoading);

        await model.LoadAsync();

        Assert.True(model.State.IsContent);
        Assert.Equal(["Planta 1", "Planta 2"], model.Zones.Select(z => z.Name));
        Assert.Equal([1, 2, 3], model.Zones[0].Visible.Select(l => l.Number));
        Assert.Equal((4, 2, 1, 1), (model.Counters.Active, model.Counters.Free, model.Counters.Occupied, model.Counters.Broken));
    }

    [Fact]
    [Trait("spec", Spec + ": Carga y rendimiento del mapa (300 taquillas)")]
    public async Task While_the_map_loads_the_state_says_loading_and_never_empty()
    {
        var release = new TaskCompletionSource<Result<LockerMapData>>();
        var model = new LockerMapViewModel(_ => release.Task, (_, _) => Task.FromResult(Result<MapLocker?>.Success(null)), new UiPreferencesSession(_store), new ResultNotifier(_notifications, _localizer, _log), _localizer);

        var loading = model.LoadAsync();
        Assert.True(model.State.IsLoading);
        Assert.False(model.State.IsEmpty);
        release.SetResult(Result<LockerMapData>.Success(_map));
        await loading;

        Assert.True(model.State.IsContent);
    }

    [Fact]
    [Trait("spec", Spec + ": Mapa de taquillas por zona (Sin taquillas)")]
    public async Task Without_lockers_the_map_explains_what_to_do()
    {
        _map = LockerMapData.Empty;
        var model = Model();

        await model.LoadAsync();

        Assert.True(model.State.IsEmpty);
        Assert.Contains("Dona-les d'alta", model.State.Message);
        Assert.Empty(model.Zones);
    }

    [Fact]
    [Trait("spec", Spec + ": Filtros del mapa (Filtrar por estado)")]
    public async Task Filtering_by_status_shows_only_those_lockers_and_leaves_out_zones_with_none_but_the_counts_stay()
    {
        var model = Model();
        await model.LoadAsync();

        model.StatusFilter = LockerStatusView.Broken;

        var zone = Assert.Single(model.Zones);
        Assert.Equal("Planta 1", zone.Name);
        Assert.Equal([3], zone.Visible.Select(l => l.Number));
        Assert.Equal(4, model.Counters.Active); // the counters describe the whole map, not the filter

        model.StatusFilter = null;
        Assert.Equal(2, model.Zones.Count);
    }

    [Fact]
    [Trait("spec", Spec + ": Filtros del mapa (Filtrar por estado)")]
    public async Task Filtering_by_zone_and_a_filter_that_leaves_nothing_offers_to_clear_it()
    {
        var model = Model();
        await model.LoadAsync();

        model.ZoneFilter = _zoneTwo;
        Assert.Equal(["Planta 2"], model.Zones.Select(z => z.Name));

        model.StatusFilter = LockerStatusView.Broken; // Planta 2 has none broken
        Assert.True(model.State.IsEmpty);
        Assert.Equal(ListViewState.NoResults, model.State.State);
        model.State.Actions[0].Command.Execute(null);

        Assert.Null(model.StatusFilter);
        Assert.Null(model.ZoneFilter);
        Assert.Equal(2, model.Zones.Count);
    }

    [Fact]
    [Trait("spec", Spec + ": Carga y rendimiento del mapa (Actualización tras un cambio)")]
    public async Task After_a_change_only_that_locker_is_read_again_and_the_counters_follow_without_reloading()
    {
        var model = Model();
        await model.LoadAsync();
        Assert.Equal(1, _loads);
        _reread = id => id == _free ? new MapLocker(_free, 1, LockerStatusView.Occupied, Guid.NewGuid(), "Pau Abad", false) : null;

        await model.RefreshLockerAsync(_free);

        Assert.Equal(1, _loads); // the map was not loaded again
        Assert.Equal(LockerStatusView.Occupied, model.Find(_free)!.Status);
        Assert.Equal("Pau Abad", model.Find(_free)!.StudentName);
        Assert.Equal((1, 2), (model.Counters.Free, model.Counters.Occupied));
        Assert.Equal(LockerStatusView.Occupied, model.Zones[0].Visible[0].Status);
    }

    [Fact]
    [Trait("spec", Spec + ": Carga y rendimiento del mapa (Actualización tras un cambio)")]
    public async Task A_locker_that_is_gone_leaves_the_map_and_the_counters()
    {
        var model = Model();
        await model.LoadAsync();

        await model.RefreshLockerAsync(_broken); // the query answers null: retired

        Assert.Null(model.Find(_broken));
        Assert.Equal(3, model.Counters.Active);
        Assert.Equal(0, model.Counters.Broken);
    }

    [Fact]
    [Trait("spec", Spec + ": Filtros del mapa (Resaltar una búsqueda)")]
    public async Task Revealing_a_locker_highlights_it_opens_its_detail_clears_what_hides_it_and_unfolds_its_zone()
    {
        var model = Model();
        await model.LoadAsync();
        model.StatusFilter = LockerStatusView.Free;
        model.Zones.Single(z => z.ZoneId == _zoneOne).Section.IsExpanded = false;

        model.Reveal(_occupied); // an occupied locker, hidden by the filter, in a folded zone

        Assert.Equal(_occupied, model.HighlightedLockerId);
        Assert.Equal(_occupied, model.SelectedLockerId);
        Assert.Null(model.StatusFilter);
        Assert.True(model.Zones.Single(z => z.ZoneId == _zoneOne).Section.IsExpanded);
    }

    [Fact]
    [Trait("spec", Spec + ": Filtros del mapa (Resaltar una búsqueda)")]
    public async Task Revealing_a_locker_that_is_not_on_the_map_does_nothing()
    {
        var model = Model();
        await model.LoadAsync();

        model.Reveal(Guid.NewGuid());

        Assert.Null(model.HighlightedLockerId);
        Assert.Null(model.SelectedLockerId);
    }

    [Fact]
    [Trait("spec", Spec + ": Carga y rendimiento del mapa (300 taquillas)")]
    public async Task A_failure_while_loading_tells_the_person_and_keeps_the_interface_alive()
    {
        _crash = new IOException("disk failed");
        var model = Model();

        await model.LoadAsync();

        Assert.Equal(NotificationKind.Error, Assert.Single(_notifications.Published).Kind);
        Assert.Equal("LoadLockerMap", Assert.Single(_log.Entries).Context);
    }

    // --- The view ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": Mapa de taquillas por zona (Estado visible)")]
    public async Task Each_locker_is_drawn_with_its_status_by_colour_and_icon_and_the_debt_mark_and_the_counters_filter()
    {
        var model = Model();
        var view = new LockerMapView(model, _localizer);
        var window = new Window { Width = 1000, Height = 700, Content = view };
        window.Show();
        await model.LoadAsync();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(4, view.Cells.Count);
        Assert.Equal(5, view.Chips.Count); // one per status
        Assert.Contains(view.Chips[0].GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Lliure: 2");
        Assert.Equal("Taquilla 2 · Ocupada · Marta Puig · Pendent de pagament", ToolTip.GetTip(view.Cells[_occupied]));
        Assert.Equal("Taquilla 1 · Lliure", ToolTip.GetTip(view.Cells[_free]));
        var withMarkers = view.Cells.ToDictionary(c => c.Key, c => c.Value.GetVisualDescendants().OfType<Material.Icons.Avalonia.MaterialIcon>().Count());
        Assert.Equal(2, withMarkers[_occupied]); // its status icon and the debt mark
        Assert.Equal(1, withMarkers[_free]);

        view.Chips[3].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Primitives.ToggleButton.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(LockerStatusView.Broken, model.StatusFilter);
        Assert.Equal(new[] { _broken }, view.Cells.Keys);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Detalle de la taquilla seleccionada")]
    public async Task Clicking_a_locker_selects_it_and_outlines_it()
    {
        var model = Model();
        var view = new LockerMapView(model, _localizer);
        var window = new Window { Width = 1000, Height = 700, Content = view };
        window.Show();
        await model.LoadAsync();
        Dispatcher.UIThread.RunJobs();

        view.Cells[_occupied].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(_occupied, model.SelectedLockerId);
        Assert.NotSame(view.Cells[_free].BorderBrush, view.Cells[_occupied].BorderBrush);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Filtros del mapa (Filtrar por estado)")]
    public async Task The_zone_filter_lists_every_zone_and_choosing_one_shows_only_it()
    {
        var model = Model();
        var view = new LockerMapView(model, _localizer);
        var window = new Window { Width = 1000, Height = 700, Content = view };
        window.Show();
        await model.LoadAsync();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(["Totes les zones", "Planta 1", "Planta 2"], view.ZoneFilter.Items.Cast<string>());

        view.ZoneFilter.SelectedIndex = 2;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(_zoneTwo, model.ZoneFilter);
        Assert.Equal(new[] { _other }, view.Cells.Keys);
    }

    // --- The start screen as a replaceable piece ---

    sealed class OtherHome : IHomeScreen
    {
        public Control Create() => new TextBlock { Text = "un altre inici" };
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Inicio como pantalla registrable (Cambiar el inicio)")]
    public void Registering_another_start_screen_shows_it_in_home_without_touching_the_rest_of_the_frame()
    {
        var registry = SectionRegistry.Compose(new Dictionary<string, Func<Control>>(), home: new OtherHome());
        var navigation = new NavigationViewModel(registry, new UiPreferencesSession(_store), s => SectionPlaceholder.Create(s, registry, _localizer));
        var shell = new ShellView(navigation, _localizer);
        var window = new Window { Width = 1000, Height = 700, Content = shell };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Home", navigation.CurrentSectionId);
        Assert.Equal("un altre inici", Assert.IsType<TextBlock>(shell.Current).Text);
        Assert.Equal(8, registry.Sections.Count); // the sections did not change
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Inicio como pantalla registrable (Cambiar el inicio)")]
    public async Task The_map_is_the_default_start_screen_and_takes_a_locker_chosen_in_the_search_before_it_existed()
    {
        var model = Model();
        var navigator = new SearchNavigator();
        var home = new LockerMapHomeScreen(model, navigator, _localizer);
        var registry = SectionRegistry.Compose(new Dictionary<string, Func<Control>>(), home: home);
        var navigation = new NavigationViewModel(registry, new UiPreferencesSession(_store), s => SectionPlaceholder.Create(s, registry, _localizer), startSection: "Students");
        navigator.Bind(navigation);
        navigator.Open(new SearchTarget(SearchTargetKind.Locker, _occupied)); // the person chose a locker; Home was never opened

        var window = new Window { Width = 1000, Height = 700, Content = new ShellView(navigation, _localizer) };
        window.Show();
        await Task.Delay(50);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Home", navigation.CurrentSectionId);
        Assert.Equal(_occupied, model.HighlightedLockerId);
        Assert.Equal(_occupied, model.SelectedLockerId);
    }
}
