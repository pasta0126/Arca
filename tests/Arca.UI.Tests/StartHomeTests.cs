// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Home;
using Arca.Application.Localization;
using Arca.Application.Preferences;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Home;
using Arca.UI.Notifications;
using Arca.UI.Preferences;
using Arca.UI.Screens;
using Arca.UI.Shell;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Arca.UI.Tests;

public sealed class StartHomeTests
{
    const string Spec = "filtres-i-targetes/pantalla-principal";

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
    readonly List<string> _calls = [];
    RecordingConfirmations _confirmations = new(true);
    string? _year = "2026-2027";
    bool _hasLockers = true;
    bool _hasStudents = true;
    Exception? _crash;
    int _ensures;
    int _restored = 1;
    ResolvedHomeCard? _resolved;
    List<HomeCardView> _cards = [];

    public StartHomeTests()
    {
        var registry = SectionRegistry.Compose(new Dictionary<string, Func<Control>>());
        _navigation = new NavigationViewModel(registry, new UiPreferencesSession(new MemoryStore()), s => SectionPlaceholder.Create(s, registry, _localizer));
        _router.Bind(_navigation);
        SeedCards();
    }

    void SeedCards()
    {
        _cards = [];
        Add("Taquilles lliures", HomeCardTargetView.LockerMap, 570, new Dictionary<string, string> { ["Status"] = "Free" });
        Add("Taquilles avariades", HomeCardTargetView.LockerMap, 15, new Dictionary<string, string> { ["Status"] = "Broken" });
        Add("Alumnes sense taquilla", HomeCardTargetView.Students, 912, new Dictionary<string, string> { ["Locker"] = "without" });
        Add("Alumnes amb pendents de pagament", HomeCardTargetView.Students, 60, new Dictionary<string, string> { ["Payment"] = "pending" });
    }

    void Add(string title, HomeCardTargetView target, int? count, Dictionary<string, string> criteria, HomeCardState state = HomeCardState.WithCount, string[]? ignored = null) =>
        _cards.Add(new HomeCardView(Guid.NewGuid(), title, target, criteria, count, state, ignored ?? [], _cards.Count, false, false, null));

    IReadOnlyList<HomeCardView> Positioned() =>
        [.. _cards.Select((c, i) => c with { Position = i, IsFirst = i == 0, IsLast = i == _cards.Count - 1 })];

    HomeCardServices Services() => new(
        _ =>
        {
            _ensures++;
            return Task.FromResult(Result<int>.Success(0));
        },
        _ =>
        {
            _calls.Add("restore");
            return Task.FromResult(Result<int>.Success(_restored));
        },
        _ => _crash is not null ? throw _crash : Task.FromResult(Result<HomeCardsView>.Success(new HomeCardsView(_year, Positioned(), _hasLockers, _hasStudents))),
        (id, _) =>
        {
            var card = _cards.Single(c => c.Id == id);
            return Task.FromResult(Result<ResolvedHomeCard>.Success(_resolved ?? new ResolvedHomeCard(card.Id, card.Title, card.Target, card.Criteria, [])));
        },
        (_, _) => Task.FromResult(Result<HomeCardSaved>.Failure(new Error("Test.NotUsedYet"))),
        (_, _) => Task.FromResult(Result<HomeCardSaved>.Failure(new Error("Test.NotUsedYet"))),
        (request, _) =>
        {
            _calls.Add($"move {request.Move} {_cards.Single(c => c.Id == request.Id).Title}");
            var at = _cards.FindIndex(c => c.Id == request.Id);
            var to = request.Move == HomeCardMove.Earlier ? at - 1 : at + 1;
            (_cards[at], _cards[to]) = (_cards[to], _cards[at]);
            return Task.FromResult(Result<bool>.Success(true));
        },
        (request, _) =>
        {
            var card = _cards.Single(c => c.Id == request.Id);
            _calls.Add("delete " + card.Title);
            _cards.Remove(card);
            return Task.FromResult(Result<string>.Success(card.Title));
        },
        _ => Task.FromResult(Result<CardOptions>.Success(new CardOptions([], [], []))));

    ScreenContext Context() => new(_localizer, _notifications, _log, new ManualDelay(), _confirmations, null!, () => Task.CompletedTask);

    StartHomeModel Model() => new(Services(), _router, _navigation.Navigate, Context());

    // --- The panel ---

    [Fact]
    [Trait("spec", Spec + ": Inicio como panel de tarjetas (Panel de tarjetas)")]
    public async Task The_panel_shows_the_year_and_the_cards_in_order_with_their_counts_and_no_amounts()
    {
        var model = Model();
        Assert.True(model.IsLoading); // a loading indicator, never an empty panel

        await model.LoadAsync();
        await model.LoadAsync();

        Assert.False(model.IsLoading);
        Assert.Equal("Curs actiu: 2026-2027", model.YearText);
        Assert.Equal(["Taquilles lliures", "Taquilles avariades", "Alumnes sense taquilla", "Alumnes amb pendents de pagament"], model.Cards.Select(c => c.View.Title));
        Assert.Equal(["570", "15", "912", "60"], model.Cards.Select(c => c.CountText));
        Assert.Equal(1, _ensures); // the default cards are made once per session, not at every read
        var texts = model.Cards.SelectMany(c => new[] { c.View.Title, c.CountText, c.StateText, c.Open.Label, c.Delete.Label });
        Assert.DoesNotContain(texts, t => t.Contains('€', StringComparison.Ordinal));
    }

    [Fact]
    [Trait("spec", Spec + ": Inicio como panel de tarjetas (Abrir una tarjeta)")]
    public async Task Opening_a_card_opens_its_section_with_its_filter_and_nothing_else()
    {
        var model = Model();
        await model.LoadAsync();
        ScreenFilterRequest? seen = null;
        _router.Requested += (_, request) => seen = request;

        model.Cards[1].Open.Execute(null); // the broken lockers
        await Task.Delay(50);

        Assert.Equal("Lockers", _navigation.CurrentSectionId);
        Assert.Equal(("Lockers", "LockerMap", "Broken"), (seen!.Section, seen.Screen, seen.Filters["Status"]));

        model.Cards[3].Open.Execute(null); // the students with pending payments
        await Task.Delay(50);

        Assert.Equal("Students", _navigation.CurrentSectionId);
        Assert.Equal((null, "pending"), (seen!.Screen, seen.Filters["Payment"]));
        Assert.Single(seen.Filters);
    }

    [Fact]
    [Trait("spec", Spec + ": Inicio como panel de tarjetas (Sin curso activo)")]
    public async Task Without_an_active_year_the_start_says_so_offers_the_course_and_keeps_the_cards()
    {
        _year = null;
        _cards[2] = _cards[2] with { Count = null, State = HomeCardState.NoCount };
        var model = Model();

        await model.LoadAsync();

        Assert.True(model.NoActiveYear);
        Assert.Equal(4, model.Cards.Count);
        Assert.Equal(("—", "Sense recompte: no hi ha curs actiu."), (model.Cards[2].CountText, model.Cards[2].StateText));
        model.GoToCourse.Execute(null);
        Assert.Equal("Course", _navigation.CurrentSectionId);
    }

    [Fact]
    [Trait("spec", Spec + ": Inicio como panel de tarjetas (Centro sin configurar)")]
    public async Task A_centre_without_lockers_and_students_is_guided_and_still_shows_its_cards_with_zero()
    {
        (_hasLockers, _hasStudents) = (false, false);
        var model = Model();
        await model.LoadAsync();

        Assert.True(model.NotSetUp);
        Assert.False(model.NoActiveYear);
        Assert.Equal(4, model.Cards.Count);
        ScreenFilterRequest? seen = null;
        _router.Requested += (_, request) => seen = request;
        model.SetUpLockers.Execute(null);
        Assert.Equal(("Lockers", "Zones"), (seen!.Section, seen.Screen));
        model.AddStudents.Execute(null);
        Assert.Equal("Students", seen.Section);
    }

    [Fact]
    [Trait("spec", Spec + ": Inicio como panel de tarjetas (Sin tarjetas)")]
    public async Task When_every_card_was_deleted_the_start_explains_it_and_restoring_adds_the_default_ones()
    {
        _cards.Clear();
        var model = Model();
        await model.LoadAsync();
        Assert.True(model.NoCards);

        model.RestoreDefaults.Execute(null);
        await Task.Delay(50);

        Assert.Contains("restore", _calls);
        Assert.Contains(_notifications.Published, n => n.Text == "S'han restaurat 1 targetes de sèrie.");
    }

    [Fact]
    [Trait("spec", Spec + ": Tarjetas de serie (Restaurar)")]
    public async Task Restoring_with_every_default_card_present_says_so()
    {
        _restored = 0;
        var model = Model();
        await model.LoadAsync();

        model.RestoreDefaults.Execute(null);
        await Task.Delay(50);

        Assert.Contains(_notifications.Published, n => n.Text == "Ja hi són totes les targetes de sèrie.");
    }

    // --- Obsolete cards ---

    [Fact]
    [Trait("spec", "filtres-i-targetes/targetes-d-inici: Tarjetas con filtros obsoletos (Zona que ya no existe)")]
    public async Task An_obsolete_card_says_so_with_text_and_opening_it_warns_of_the_criterion_that_was_left_out()
    {
        Add("De la zona vella", HomeCardTargetView.Lockers, 2, new Dictionary<string, string> { ["Status"] = "Free" }, HomeCardState.Obsolete, ["Zone"]);
        var model = Model();
        await model.LoadAsync();
        var card = model.Cards[^1];
        _resolved = new ResolvedHomeCard(card.View.Id, card.View.Title, HomeCardTargetView.Lockers, card.View.Criteria, ["Zone"]);

        Assert.Equal("Filtre obsolet: ja no existeix la zona. El recompte l'ignora.", card.StateText);
        card.Open.Execute(null);
        await Task.Delay(50);

        var warning = Assert.Single(_notifications.Published, n => n.Kind == NotificationKind.Warning);
        Assert.Contains("la zona", warning.Text, StringComparison.Ordinal);
        Assert.Equal("Lockers", _navigation.CurrentSectionId); // and it opened anyway, without that criterion
    }

    // --- Moving and deleting ---

    [Fact]
    [Trait("spec", "filtres-i-targetes/targetes-d-inici: Editar, ordenar y borrar tarjetas (Mover, Primera tarjeta)")]
    public async Task The_first_card_cannot_move_earlier_nor_the_last_later_and_each_says_why()
    {
        var model = Model();
        await model.LoadAsync();

        Assert.False(model.Cards[0].MoveEarlier.IsAvailable);
        Assert.Equal("Ja és la primera targeta.", model.Cards[0].MoveEarlier.UnavailableReason);
        Assert.True(model.Cards[0].MoveLater.IsAvailable);
        Assert.False(model.Cards[^1].MoveLater.IsAvailable);
        Assert.Equal("Ja és l'última targeta.", model.Cards[^1].MoveLater.UnavailableReason);
    }

    [Fact]
    [Trait("spec", "filtres-i-targetes/targetes-d-inici: Editar, ordenar y borrar tarjetas (Mover)")]
    public async Task Moving_a_card_moves_it_tells_so_and_the_panel_shows_the_new_order()
    {
        var model = Model();
        await model.LoadAsync();

        model.Cards[2].MoveEarlier.Execute(null);
        await Task.Delay(80);

        Assert.Contains("move Earlier Alumnes sense taquilla", _calls);
        Assert.Equal(["Taquilles lliures", "Alumnes sense taquilla", "Taquilles avariades", "Alumnes amb pendents de pagament"], model.Cards.Select(c => c.View.Title));
        Assert.Contains(_notifications.Published, n => n.Text == "S'ha mogut la targeta «Alumnes sense taquilla».");
    }

    [Fact]
    [Trait("spec", "filtres-i-targetes/targetes-d-inici: Feedback y accesibilidad de las tarjetas (Error al guardar)")]
    public async Task A_double_click_on_move_moves_once()
    {
        var model = Model();
        await model.LoadAsync();
        var move = model.Cards[2].MoveEarlier;

        move.Execute(null);
        move.Execute(null);
        await Task.Delay(120);

        Assert.Single(_calls, c => c.StartsWith("move", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("spec", "filtres-i-targetes/targetes-d-inici: Editar, ordenar y borrar tarjetas (Borrar)")]
    public async Task Deleting_asks_first_with_the_title_and_says_nothing_else_is_touched_and_then_tells_it()
    {
        var model = Model();
        await model.LoadAsync();

        model.Cards[1].Delete.Execute(null);
        await Task.Delay(100);

        var asked = Assert.Single(_confirmations.Asked);
        Assert.Contains("Taquilles avariades", asked.Title, StringComparison.Ordinal);
        Assert.Contains("No es canvia cap alumne ni cap taquilla", asked.Consequence, StringComparison.Ordinal);
        Assert.True(asked.Destructive);
        Assert.Contains("delete Taquilles avariades", _calls);
        Assert.Equal(3, model.Cards.Count);
        Assert.Contains(_notifications.Published, n => n.Text == "S'ha esborrat la targeta «Taquilles avariades».");
    }

    [Fact]
    [Trait("spec", "filtres-i-targetes/targetes-d-inici: Editar, ordenar y borrar tarjetas (Borrar)")]
    public async Task Declining_the_confirmation_deletes_nothing()
    {
        _confirmations = new RecordingConfirmations(false);
        var model = Model();
        await model.LoadAsync();

        model.Cards[1].Delete.Execute(null);
        await Task.Delay(60);

        Assert.DoesNotContain(_calls, c => c.StartsWith("delete", StringComparison.Ordinal));
        Assert.Equal(4, model.Cards.Count);
    }

    [Fact]
    [Trait("spec", Spec + ": Inicio como panel de tarjetas (Carga)")]
    public async Task A_failure_reading_the_cards_tells_the_person_without_student_data_and_the_panel_keeps_loading()
    {
        _crash = new IOException("disk failed for Marta Puig");
        var model = Model();

        await model.LoadAsync();

        Assert.True(model.IsLoading);
        Assert.Equal("LoadStartCards", Assert.Single(_log.Entries).Context);
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

    // --- The screen ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": Inicio como panel de tarjetas (Panel de tarjetas)")]
    public async Task The_start_screen_draws_each_card_as_a_button_with_its_count_and_its_tools_reachable_without_the_mouse()
    {
        var state = new GlobalStateService(_ => Task.FromResult(Result<Arca.Application.GlobalState.GlobalState>.Success(new(null, 0))), new ResultNotifier(_notifications, _localizer, _log));
        var home = new StartHomeScreen(Model(), state, _localizer);
        var screen = home.Create();
        var window = new Window { Width = 1000, Height = 700, Content = screen };
        window.Show();
        await Task.Delay(100);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(4, home.OpenButtons.Count);
        var texts = screen.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Empty).ToList();
        Assert.Contains("912", texts);
        Assert.Contains("Alumnes sense taquilla", texts);
        Assert.DoesNotContain(texts, t => t.Contains('€', StringComparison.Ordinal));
        var buttons = screen.GetVisualDescendants().OfType<Button>().ToList();
        Assert.Equal(4 + 4 * 3 + 1, buttons.Count); // open, three tools each and the restore button
        Assert.All(buttons, b => Assert.True(b.Focusable && b.IsTabStop));
        Assert.All(buttons.Take(4 + 12), b => Assert.False(string.IsNullOrEmpty(Avalonia.Automation.AutomationProperties.GetName(b))));

        home.OpenButtons[2].Command!.Execute(null);
        await Task.Delay(50);

        Assert.Equal("Students", _navigation.CurrentSectionId);
    }

    [AvaloniaFact]
    public async Task Screenshot_of_the_start_screen()
    {
        Add("De la zona vella", HomeCardTargetView.Lockers, 2, new Dictionary<string, string> { ["Status"] = "Free" }, HomeCardState.Obsolete, ["Zone"]);
        var state = new GlobalStateService(_ => Task.FromResult(Result<Arca.Application.GlobalState.GlobalState>.Success(new(null, 0))), new ResultNotifier(_notifications, _localizer, _log));
        var window = new Window { Width = 1000, Height = 640, Content = new StartHomeScreen(Model(), state, _localizer).Create() };
        window.Show();
        await Task.Delay(120);
        Dispatcher.UIThread.RunJobs();
        ScreenshotTests.Take(window, "start-home");
        window.Close();
    }
}
