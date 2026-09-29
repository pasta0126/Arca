// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.GlobalState;
using Arca.Application.Localization;
using Arca.Application.Preferences;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Commands;
using Arca.UI.Notifications;
using Arca.UI.Preferences;
using Arca.UI.Shell;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Arca.UI.Tests;

public sealed class ShellStateTests
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
    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    GlobalState _next = new(null, 0);
    Result<GlobalState>? _failure;
    Exception? _crash;

    static GlobalState Year(int pending = 0) => new(new YearSummary(Guid.NewGuid(), "2026-2027"), pending);

    GlobalStateService Service() => new(
        _ => _crash is not null ? throw _crash : Task.FromResult(_failure ?? Result<GlobalState>.Success(_next)),
        new ResultNotifier(_notifications, _localizer, _log));

    NavigationViewModel Navigation(Func<int>? payments = null)
    {
        var attention = payments is null ? null : new Dictionary<string, Func<int>> { [ShellCatalog.Payments] = payments };
        var registry = SectionRegistry.Compose(new Dictionary<string, Func<Control>>(), attention);
        return new NavigationViewModel(registry, new UiPreferencesSession(new MemoryStore()), s => SectionPlaceholder.Create(s, registry, _localizer));
    }

    // --- The service ---

    [Fact]
    [Trait("spec", Spec + ": Cabecera con el estado global (Curso activo)")]
    public async Task Refreshing_loads_the_state_and_announces_it_once_and_only_when_it_is_different()
    {
        var service = Service();
        var changes = 0;
        service.Changed += (_, _) => changes++;
        Assert.False(service.IsLoaded);

        _next = new GlobalState(new YearSummary(Guid.Empty, "2026-2027"), 3);
        await service.RefreshAsync();
        await service.RefreshAsync(); // nothing changed since

        Assert.True(service.IsLoaded);
        Assert.Equal(3, service.Current!.PendingCharges);
        Assert.Equal(1, changes);

        _next = new GlobalState(new YearSummary(Guid.Empty, "2026-2027"), 2);
        await service.RefreshAsync();
        Assert.Equal(2, changes);
    }

    [Fact]
    [Trait("spec", Spec + ": Indicadores de sección")]
    public async Task A_failed_refresh_keeps_the_state_it_had_and_tells_the_person()
    {
        var service = Service();
        _next = Year(4);
        await service.RefreshAsync();

        _failure = Result<GlobalState>.Failure(new Error("Storage.SchemaNewer"));
        await service.RefreshAsync();
        _failure = null;
        _crash = new IOException("disk failed");
        await service.RefreshAsync();

        Assert.Equal(4, service.Current!.PendingCharges);
        Assert.Equal([NotificationKind.Error, NotificationKind.Error], _notifications.Published.Select(p => p.Kind));
        Assert.Single(_log.Entries); // only the unexpected one goes to the technical log
        Assert.Equal("GlobalState", _log.Entries[0].Context);
    }

    [Fact]
    [Trait("spec", Spec + ": Indicadores de sección")]
    public async Task A_command_refreshes_the_state_after_a_successful_write_and_not_after_a_failed_one()
    {
        var service = Service();
        var runs = 0;
        _next = Year(1);
        var command = new RunOnceCommand<int>(
            (_, _) => Task.FromResult(runs++ == 0 ? Result<int>.Success(1) : Result<int>.Failure(new Error("Storage.SchemaNewer"))),
            n => n.ToString(System.Globalization.CultureInfo.InvariantCulture), "Write", _notifications, _localizer, _log, new ManualDelay(), () => service.RefreshAsync());

        await command.RunAsync();
        Assert.Equal(1, service.Current!.PendingCharges);

        _next = Year(9);
        await command.RunAsync(); // fails: the state was not asked again

        Assert.Equal(1, service.Current.PendingCharges);
    }

    // --- Notices ---

    [Fact]
    [Trait("spec", Spec + ": Avisos globales (Sin curso activo)")]
    public async Task Without_an_active_year_a_notice_with_its_action_takes_the_person_to_the_course_section()
    {
        var service = Service();
        var navigation = Navigation();
        var notices = new GlobalNoticesViewModel(service, navigation, _localizer);
        Assert.Empty(notices.Notices); // nothing is said before the state is known

        await service.RefreshAsync();

        var notice = Assert.Single(notices.Notices);
        Assert.Equal("NoActiveYear", notice.Id);
        Assert.Contains("cap curs actiu", notice.Text);
        Assert.Equal("Ves a Curs", notice.ActionLabel);
        notice.Act();
        Assert.Equal("Course", navigation.CurrentSectionId);
    }

    [Fact]
    [Trait("spec", Spec + ": Avisos globales (Aviso descartable)")]
    public async Task A_notice_that_blocks_actions_cannot_be_closed_and_goes_away_when_solved()
    {
        var service = Service();
        var notices = new GlobalNoticesViewModel(service, Navigation(), _localizer);
        await service.RefreshAsync();

        notices.Dismiss("NoActiveYear");
        Assert.Single(notices.Notices);
        Assert.False(notices.Notices[0].CanDismiss);

        _next = Year();
        await service.RefreshAsync();
        Assert.Empty(notices.Notices);
    }

    // --- Header ---

    [Fact]
    [Trait("spec", Spec + ": Cabecera con el estado global (Curso activo)")]
    public async Task The_state_of_the_year_is_written_in_the_header()
    {
        var service = Service();
        var header = new HeaderView(service, _localizer, "ARCA");
        Assert.Equal(string.Empty, header.Year.Text);

        _next = Year();
        await service.RefreshAsync();
        Assert.Equal("Curs 2026-2027", header.Year.Text);

        _next = new GlobalState(null, 0);
        await service.RefreshAsync();
        Assert.Equal("Sense curs actiu", header.Year.Text);
        Assert.Equal("ARCA", header.Name.Text);
    }

    // --- Views ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": Indicadores de sección (Nada que atender)")]
    public void The_payments_section_shows_a_badge_with_the_pending_count_only_when_there_is_something_to_attend_to()
    {
        var pending = 0;
        var navigation = Navigation(() => pending);
        var sidebar = new SidebarView(navigation, _localizer);
        var window = new Window { Width = 400, Height = 700, Content = sidebar };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Border Badge(string id) => sidebar.ButtonOf(id).GetVisualDescendants().OfType<Border>().Single(b => b.CornerRadius.TopLeft > 0);
        Assert.False(Badge("Payments").IsVisible);

        pending = 3;
        navigation.RefreshAttention();
        Dispatcher.UIThread.RunJobs();

        Assert.True(Badge("Payments").IsVisible);
        Assert.Contains(sidebar.ButtonOf("Payments").GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "3");
        Assert.Equal("Cobraments · 3 pendents", ToolTip.GetTip(sidebar.ButtonOf("Payments")));
        Assert.False(Badge("Students").IsVisible); // the others have nothing to show

        pending = 0;
        navigation.RefreshAttention();
        Assert.False(Badge("Payments").IsVisible);
    }

    [Fact]
    [Trait("spec", Spec + ": Indicadores de sección (Nada que atender)")]
    public void A_count_that_makes_no_sense_shows_no_indicator()
    {
        Assert.Equal(0, Navigation(() => -5).AttentionOf("Payments"));
        Assert.Equal(0, Navigation().AttentionOf("Students"));
        Assert.Equal(0, Navigation().AttentionOf("Nowhere"));
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Avisos globales (Sin curso activo)")]
    public async Task The_notice_bar_shows_the_banner_with_its_button_and_nothing_when_there_is_nothing_to_say()
    {
        var service = Service();
        var navigation = Navigation();
        var bar = new NoticeBarView(new GlobalNoticesViewModel(service, navigation, _localizer), _localizer);
        var window = new Window { Width = 900, Height = 300, Content = bar };
        window.Show();
        Assert.Empty(bar.Banners);

        await service.RefreshAsync();
        Dispatcher.UIThread.RunJobs();
        var banner = Assert.Single(bar.Banners);
        var go = banner.GetVisualDescendants().OfType<Button>().Single();
        go.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

        Assert.Equal("Course", navigation.CurrentSectionId);
        Assert.DoesNotContain(banner.GetVisualDescendants().OfType<Button>(), b => Equals(b.Content, "Tanca")); // it cannot be closed
    }
}
