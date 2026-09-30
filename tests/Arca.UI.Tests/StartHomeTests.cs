// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Home;
using Arca.Application.Lockers;
using Arca.Application.Localization;
using Arca.Application.Preferences;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Home;
using Arca.UI.Notifications;
using Arca.UI.Preferences;
using Arca.UI.Shell;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Arca.UI.Tests;

public sealed class StartHomeTests
{
    const string Spec = "ui-llistats-i-detall/pantalla-principal";

    sealed class MemoryStore : IUiPreferencesStore
    {
        public UiPreferences Load() => new();

        public void Save(UiPreferences preferences)
        {
        }
    }

    readonly ResxLocalizer _localizer = new();
    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    readonly ScreenFilterRouter _router = new();
    readonly NavigationViewModel _navigation;
    HomeSummary _summary = new("2026-2027", new LockerCounters(600, 570, 20, 15, 10, 5), 912, 60, true, true);
    Exception? _crash;

    public StartHomeTests()
    {
        var registry = SectionRegistry.Compose(new Dictionary<string, Func<Control>>());
        _navigation = new NavigationViewModel(registry, new UiPreferencesSession(new MemoryStore()), s => SectionPlaceholder.Create(s, registry, _localizer));
        _router.Bind(_navigation);
    }

    StartHomeModel Model() => new(
        _ => _crash is not null ? throw _crash : Task.FromResult(Result<HomeSummary>.Success(_summary)), _router, _navigation.Navigate,
        new ResultNotifier(_notifications, _localizer, _log), _localizer);

    [Fact]
    [Trait("spec", Spec + ": Inicio como pantalla registrable (Resumen mínimo)")]
    public async Task The_summary_shows_the_year_and_a_link_per_status_and_per_group_of_students_with_counts_and_no_amounts()
    {
        var model = Model();
        Assert.Equal(StartHomeState.Loading, model.State);

        await model.LoadAsync();

        Assert.Equal(StartHomeState.Summary, model.State);
        Assert.Equal("Curs actiu: 2026-2027", model.YearText);
        Assert.Equal([570, 20, 5, 15, 10], model.LockerLinks.Select(l => l.Count));
        Assert.Equal([912, 60], model.StudentLinks.Select(l => l.Count));
        var texts = model.LockerLinks.Concat(model.StudentLinks).Select(l => l.Text + l.Open.Label).ToList();
        Assert.DoesNotContain(texts, t => t.Contains('€', StringComparison.Ordinal));
        Assert.Contains("Alumnes amb pendents de pagament", model.StudentLinks[1].Text, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Inicio como pantalla registrable (Resumen mínimo)")]
    public async Task Each_link_opens_its_section_with_the_filter_that_produced_the_count()
    {
        var model = Model();
        await model.LoadAsync();
        ScreenFilterRequest? seen = null;
        _router.Requested += (_, request) => seen = request;

        model.LockerLinks[0].Open.Execute(null); // free

        Assert.Equal("Lockers", _navigation.CurrentSectionId);
        Assert.Equal(("Lockers", "LockerMap", "Free"), (seen!.Section, seen.Screen, seen.Filters["Status"]));

        model.StudentLinks[1].Open.Execute(null); // with pending payments

        Assert.Equal("Students", _navigation.CurrentSectionId);
        Assert.Equal("pending", seen!.Filters["Payment"]);
        model.StudentLinks[0].Open.Execute(null);
        Assert.Equal("without", seen.Filters["Locker"]);
    }

    [Fact]
    [Trait("spec", Spec + ": Inicio como pantalla registrable (Sin curso activo)")]
    public async Task Without_an_active_year_the_start_says_so_and_offers_the_course_section()
    {
        _summary = new HomeSummary(null, new LockerCounters(0, 0, 0, 0, 0, 0), 0, 0, false, false);
        var model = Model();

        await model.LoadAsync();

        Assert.Equal(StartHomeState.NoActiveYear, model.State);
        Assert.Empty(model.StudentLinks);
        model.GoToCourse.Execute(null);
        Assert.Equal("Course", _navigation.CurrentSectionId);
    }

    [Fact]
    [Trait("spec", Spec + ": Inicio como pantalla registrable (Centro sin configurar)")]
    public async Task A_centre_without_lockers_and_students_is_guided_to_set_up_the_zones_and_add_students()
    {
        _summary = new HomeSummary("2026-2027", new LockerCounters(0, 0, 0, 0, 0, 0), 0, 0, false, false);
        var model = Model();

        await model.LoadAsync();

        Assert.Equal(StartHomeState.NotSetUp, model.State);
        ScreenFilterRequest? seen = null;
        _router.Requested += (_, request) => seen = request;
        model.SetUpLockers.Execute(null);
        Assert.Equal(("Lockers", "Zones"), (seen!.Section, seen.Screen));
        model.AddStudents.Execute(null);
        Assert.Equal("Students", seen.Section);
    }

    [Fact]
    [Trait("spec", Spec + ": Inicio como pantalla registrable (Resumen mínimo)")]
    public async Task A_failure_reading_the_summary_tells_the_person_without_any_student_data_and_the_start_stays_alive()
    {
        _crash = new IOException("disk failed for Marta Puig");
        var model = Model();

        await model.LoadAsync();

        Assert.Equal(StartHomeState.Loading, model.State);
        Assert.Equal("LoadStartSummary", Assert.Single(_log.Entries).Context);
        Assert.All(_notifications.Published, n => Assert.DoesNotContain("Marta", n.Text, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("spec", "ui-llistats-i-detall/navegacio-i-cerca: Patrón común de pantalla (Quitar un filtro)")]
    public void A_filter_request_waits_for_the_section_to_be_built_and_is_taken_only_once()
    {
        _router.Open(new ScreenFilterRequest("Students", null, new Dictionary<string, string> { ["Payment"] = "pending" }));

        Assert.Equal("Students", _navigation.CurrentSectionId);
        Assert.Null(_router.Take("Lockers"));
        Assert.Equal("pending", _router.Take("Students")!.Filters["Payment"]);
        Assert.Null(_router.Take("Students"));
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Inicio como pantalla registrable (Resumen mínimo)")]
    public async Task The_start_screen_draws_the_counts_as_buttons_that_open_the_sections()
    {
        var state = new GlobalStateService(_ => Task.FromResult(Result<Arca.Application.GlobalState.GlobalState>.Success(new(null, 0))), new ResultNotifier(_notifications, _localizer, _log));
        var model = Model();
        var screen = new StartHomeScreen(model, state, _localizer).Create();
        var window = new Window { Width = 1000, Height = 700, Content = screen };
        window.Show();
        await Task.Delay(80);
        Dispatcher.UIThread.RunJobs();

        var buttons = screen.GetVisualDescendants().OfType<Button>().ToList();
        Assert.Equal(7, buttons.Count);
        var texts = screen.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Empty).ToList();
        Assert.Contains("912", texts);
        Assert.Contains("Alumnes sense taquilla", texts);
        Assert.DoesNotContain(texts, t => t.Contains('€', StringComparison.Ordinal));

        buttons[5].Command!.Execute(null);

        Assert.Equal("Students", _navigation.CurrentSectionId);
    }

    [AvaloniaFact]
    public async Task Screenshot_of_the_start_screen()
    {
        var state = new GlobalStateService(_ => Task.FromResult(Result<Arca.Application.GlobalState.GlobalState>.Success(new(null, 0))), new ResultNotifier(_notifications, _localizer, _log));
        var model = Model();
        await model.LoadAsync();
        var window = new Window { Width = 1000, Height = 640, Content = new StartHomeScreen(model, state, _localizer).Create() };
        window.Show();
        await Task.Delay(80);
        Dispatcher.UIThread.RunJobs();
        ScreenshotTests.Take(window, "start-home");
        window.Close();
    }
}
