// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Application.Preferences;
using Arca.Application.Search;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Notifications;
using Arca.UI.Preferences;
using Arca.UI.Search;
using Arca.UI.Shell;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;

namespace Arca.UI.Tests;

public sealed class GlobalSearchUiTests
{
    const string Spec = "ui-shell/navegacio-i-cerca";

    sealed class MemoryStore : IUiPreferencesStore
    {
        public UiPreferences Load() => new();

        public void Save(UiPreferences preferences)
        {
        }
    }

    readonly ResxLocalizer _localizer = new();
    readonly ManualDelay _delay = new();
    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    readonly List<GlobalSearchRequest> _requests = [];
    Func<GlobalSearchRequest, Task<Result<GlobalSearchResult>>> _answer;

    static readonly Guid _student = Guid.NewGuid();
    static readonly Guid _locker = Guid.NewGuid();

    public GlobalSearchUiTests() => _answer = _ => Task.FromResult(Result<GlobalSearchResult>.Success(Sample()));

    static GlobalSearchResult Sample(int studentTotal = 1, int lockerTotal = 1) => new(
        [new StudentHit(_student, "Marta", "Puig", "1r ESO", "A", 15, false, true, 70m)], studentTotal,
        [new LockerHit(_locker, 15, "Planta 1", LockerStatusView.Occupied, "Marta Puig")], lockerTotal,
        [new GroupHit(Guid.NewGuid(), Guid.NewGuid(), "1r ESO", "A", 28)], 1, 0);

    NavigationViewModel Navigation()
    {
        var registry = SectionRegistry.Compose(new Dictionary<string, Func<Control>>());
        return new NavigationViewModel(registry, new UiPreferencesSession(new MemoryStore()), s => SectionPlaceholder.Create(s, registry, _localizer));
    }

    (GlobalSearchViewModel Model, NavigationViewModel Navigation, SearchNavigator Navigator) Build()
    {
        var navigation = Navigation();
        var navigator = new SearchNavigator(navigation);
        var model = new GlobalSearchViewModel(
            (request, _) => { _requests.Add(request); return _answer(request); }, _delay,
            new ResultNotifier(_notifications, _localizer, _log), navigator, _localizer);
        return (model, navigation, navigator);
    }

    static async Task Settle(ManualDelay delay)
    {
        delay.Elapse(GlobalSearchViewModel.Pause + TimeSpan.FromMilliseconds(1));
        await Task.Yield();
        await Task.Delay(10);
    }

    // --- Timing and cancellation ---

    [Fact]
    [Trait("spec", Spec + ": Búsqueda sin bloquear (Escritura rápida)")]
    public async Task The_search_waits_for_the_pause_after_the_last_key_and_runs_once()
    {
        var (model, _, _) = Build();

        model.Text = "g";
        model.Text = "ga";
        model.Text = "garcia";
        _delay.Elapse(TimeSpan.FromMilliseconds(200));
        Assert.Empty(_requests);
        Assert.Equal(SearchState.Searching, model.State);
        await Settle(_delay);

        Assert.Equal(["garcia"], _requests.Select(r => r.Text));
        Assert.Equal(SearchState.Results, model.State);
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda sin bloquear (Escritura rápida)")]
    public async Task Only_the_result_of_the_last_search_is_shown_even_if_an_older_one_answers_later()
    {
        var (model, _, _) = Build();
        var slow = new TaskCompletionSource<Result<GlobalSearchResult>>();
        _answer = request => request.Text == "ga" ? slow.Task : Task.FromResult(Result<GlobalSearchResult>.Success(Sample(studentTotal: 1)));

        model.Text = "ga";
        await Settle(_delay); // the first search is now waiting for its answer
        model.Text = "garcia";
        await Settle(_delay);
        var shown = model.Items.Select(i => i.Text).ToList();
        slow.SetResult(Result<GlobalSearchResult>.Success(new GlobalSearchResult([], 0, [], 0, [], 0, 0))); // the old one arrives late, empty
        await Task.Delay(20);

        Assert.Equal(shown, model.Items.Select(i => i.Text));
        Assert.Equal(SearchState.Results, model.State);
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda sin bloquear (Escritura rápida)")]
    public async Task Clearing_the_text_cancels_the_search_and_closes_the_panel()
    {
        var (model, _, _) = Build();
        model.Text = "garcia";
        await Settle(_delay);
        Assert.True(model.IsOpen);

        model.Text = "  ";
        await Task.Yield();

        Assert.False(model.IsOpen);
        Assert.Empty(model.Items);
    }

    // --- The results ---

    [Fact]
    [Trait("spec", Spec + ": Resultados con estado visible (Alumno moroso)")]
    public async Task Results_are_grouped_by_kind_with_the_state_of_each_visible_and_no_email()
    {
        var (model, _, _) = Build();
        model.Text = "garcia";
        await Settle(_delay);

        var lines = model.Items.Select(i => i.Text).ToList();
        var debt = 70m.ToString("C", _localizer.Culture);
        Assert.Equal(
            [
                "Alumnes", $"Puig, Marta · 1r ESO A · Taquilla 15 · Pendent de pagament ({debt})",
                "Taquilles", "Taquilla 15 · Planta 1 · Ocupada · Marta Puig",
                "Grups", "1r ESO A · 28 alumnes",
            ],
            lines);
        Assert.DoesNotContain(lines, l => l.Contains('@', StringComparison.Ordinal));
    }

    [Fact]
    [Trait("spec", Spec + ": Navegación de resultados (Abrir la ficha)")]
    public async Task The_arrows_skip_the_headings_and_stop_at_the_ends()
    {
        var (model, _, _) = Build();
        model.Text = "garcia";
        await Settle(_delay);
        Assert.Equal(1, model.SelectedIndex); // the first result, never a heading

        model.MoveNext();
        Assert.Equal(3, model.SelectedIndex);
        model.MoveNext();
        model.MoveNext();
        Assert.Equal(5, model.SelectedIndex);
        model.MovePrevious();
        model.MovePrevious();
        model.MovePrevious();
        model.MovePrevious();
        Assert.Equal(1, model.SelectedIndex);
    }

    [Fact]
    [Trait("spec", Spec + ": Navegación de resultados (Abrir la ficha)")]
    public async Task Enter_opens_the_student_in_its_section_and_closes_the_search()
    {
        var (model, navigation, navigator) = Build();
        model.Text = "garcia";
        await Settle(_delay);

        Assert.True(model.OpenSelected());

        Assert.Equal("Students", navigation.CurrentSectionId);
        Assert.Equal(new SearchTarget(SearchTargetKind.Student, _student, Text: "garcia"), navigator.Pending);
        Assert.False(model.IsOpen);
        Assert.Equal(string.Empty, model.Text);
    }

    [Fact]
    [Trait("spec", Spec + ": Navegación de resultados (Abrir la ficha)")]
    public async Task Opening_a_locker_shows_the_map_with_that_locker_highlighted()
    {
        var (model, navigation, navigator) = Build();
        navigation.Navigate("Students");
        model.Text = "15";
        await Settle(_delay);
        model.MoveNext();

        model.OpenSelected();

        Assert.Equal("Home", navigation.CurrentSectionId);
        Assert.Equal(_locker, navigator.HighlightedLocker);
        Assert.Equal(_locker, navigator.TakePending()!.Id);
        Assert.Null(navigator.TakePending()); // handed over once
        navigator.ClearHighlight();
        Assert.Null(navigator.HighlightedLocker);
    }

    [Fact]
    [Trait("spec", Spec + ": Navegación de resultados (Muchos resultados)")]
    public async Task With_more_results_than_shown_the_total_is_said_and_the_whole_list_is_offered()
    {
        var (model, navigation, navigator) = Build();
        _answer = _ => Task.FromResult(Result<GlobalSearchResult>.Success(Sample(studentTotal: 12)));
        model.Text = "alumne";
        await Settle(_delay);

        var more = model.Items.Single(i => i.Text == "Veure els 12");
        model.Open(more);

        Assert.Equal("Students", navigation.CurrentSectionId);
        Assert.Equal(new SearchTarget(SearchTargetKind.StudentList, Text: "alumne"), navigator.Pending);
    }

    [Fact]
    [Trait("spec", Spec + ": Navegación de resultados (Sin resultados)")]
    public async Task Nothing_found_says_so_and_asks_to_check_what_was_typed()
    {
        var (model, _, _) = Build();
        _answer = _ => Task.FromResult(Result<GlobalSearchResult>.Success(GlobalSearchResult.Empty));
        model.Text = "xyzzy";
        await Settle(_delay);

        Assert.Equal(SearchState.Empty, model.State);
        Assert.Equal("Cap resultat per «xyzzy». Revisa el que has escrit.", model.Message);
        Assert.False(model.OpenSelected());
    }

    [Fact]
    [Trait("spec", Spec + ": Ámbito de la búsqueda (Alumno de baja)")]
    public async Task Students_who_left_are_offered_and_including_them_searches_again()
    {
        var (model, _, _) = Build();
        _answer = request => Task.FromResult(Result<GlobalSearchResult>.Success(
            request.IncludeRetired ? Sample() : new GlobalSearchResult([], 0, [], 0, [], 0, 2)));
        model.Text = "puig";
        await Settle(_delay);
        Assert.Equal(SearchState.Empty, model.State);
        Assert.Contains("2 alumnes de baixa coincideixen", model.Message);

        model.IncludeRetired = true;
        await Settle(_delay);

        Assert.Equal(SearchState.Results, model.State);
        Assert.True(_requests[^1].IncludeRetired);
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda sin bloquear (Escritura rápida)")]
    public async Task A_failing_search_tells_the_person_and_leaves_the_panel_closed()
    {
        var (model, _, _) = Build();
        _answer = _ => throw new IOException("disk failed");
        model.Text = "garcia";
        await Settle(_delay);

        Assert.False(model.IsOpen);
        Assert.Equal(NotificationKind.Error, Assert.Single(_notifications.Published).Kind);
        Assert.Equal("GlobalSearch", Assert.Single(_log.Entries).Context);
    }

    [Fact]
    [Trait("spec", Spec + ": Navegación de resultados (Abrir la ficha)")]
    public async Task Escape_forgets_the_text_and_closes_without_opening_anything()
    {
        var (model, navigation, _) = Build();
        model.Text = "garcia";
        await Settle(_delay);

        model.Close();

        Assert.Equal(string.Empty, model.Text);
        Assert.False(model.IsOpen);
        Assert.Equal("Home", navigation.CurrentSectionId);
    }

    // --- The view ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": Búsqueda global siempre visible (Atajo)")]
    public void The_search_shortcut_puts_the_cursor_in_the_box_with_its_text_selected()
    {
        var (model, _, _) = Build();
        var box = new SearchBoxView(model, _localizer);
        var window = new Window { Content = new StackPanel { Children = { new Button(), box } } };
        window.Show();
        box.Input.Text = "antic";
        Dispatcher.UIThread.RunJobs();

        box.FocusInput();
        Dispatcher.UIThread.RunJobs();

        Assert.Same(box.Input, window.FocusManager!.GetFocusedElement());
        Assert.Equal((0, 5), (box.Input.SelectionStart, box.Input.SelectionEnd));
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Navegación de resultados (Abrir la ficha)")]
    public async Task Typing_shows_the_results_the_arrows_choose_and_enter_opens()
    {
        var (model, navigation, navigator) = Build();
        var box = new SearchBoxView(model, _localizer);
        var window = new Window { Width = 800, Height = 500, Content = box };
        window.Show();
        box.Input.Focus();
        Dispatcher.UIThread.RunJobs();

        box.Input.Text = "garcia";
        Dispatcher.UIThread.RunJobs(); // the box reports the typing, and the search starts waiting for the pause
        await Settle(_delay);
        Dispatcher.UIThread.RunJobs();
        Assert.True(box.IsPanelOpen);
        Assert.Equal((6, SearchState.Results), (model.Items.Count, model.State));
        Assert.Equal(6, box.Results.ItemCount);

        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Home", navigation.CurrentSectionId); // the second result is the locker, which lives on the map
        Assert.Equal(SearchTargetKind.Locker, navigator.Pending!.Kind);
        Assert.False(box.IsPanelOpen);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Navegación de resultados (Abrir la ficha)")]
    public async Task Escape_in_the_box_clears_it_and_closes_the_panel()
    {
        var (model, _, _) = Build();
        var box = new SearchBoxView(model, _localizer);
        var window = new Window { Width = 800, Height = 500, Content = box };
        window.Show();
        box.Input.Focus();
        box.Input.Text = "garcia";
        Dispatcher.UIThread.RunJobs();
        await Settle(_delay);
        Dispatcher.UIThread.RunJobs();
        Assert.True(box.IsPanelOpen);
        Assert.Equal(SearchState.Results, model.State);

        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Dispatcher.UIThread.RunJobs();

        Assert.False(box.IsPanelOpen);
        Assert.Equal(string.Empty, box.Input.Text);
    }
}
