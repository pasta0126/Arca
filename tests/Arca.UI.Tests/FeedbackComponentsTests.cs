// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.ComponentModel;
using System.Windows.Input;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Commands;
using Arca.UI.Confirmation;
using Arca.UI.Lists;
using Arca.UI.Notifications;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Arca.UI.Tests;

public sealed class FeedbackComponentsTests
{
    const string Spec = "ux-fonaments/components-de-feedback";

    readonly ManualDelay _delay = new();
    readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 28, 10, 0, 0, TimeSpan.Zero));
    readonly RecordingErrorLog _log = new();
    readonly ResxLocalizer _localizer = new();

    NotificationCenter Center(int max = 5) => new(_clock, _delay, max);

    // --- Notifications: pause, queue and stacking ---

    [Fact]
    [Trait("spec", Spec + ": Notificaciones de resultado (Éxito)")]
    public void A_success_waits_while_the_mouse_is_over_it_and_counts_down_again_when_it_leaves()
    {
        var center = Center();
        center.Publish(NotificationKind.Success, "Fet");
        var id = center.Visible[0].Id;

        _delay.Elapse(TimeSpan.FromSeconds(3));
        center.Pause(id);
        _delay.Elapse(TimeSpan.FromMinutes(1));
        Assert.Single(center.Visible);

        center.Resume(id);
        _delay.Elapse(TimeSpan.FromSeconds(4.9));
        Assert.Single(center.Visible);
        _delay.Elapse(TimeSpan.FromSeconds(0.2));

        Assert.Empty(center.Visible);
    }

    [Fact]
    [Trait("spec", Spec + ": Notificaciones de resultado (Varias notificaciones)")]
    public void Several_notifications_stack_and_the_successes_disappear_separately()
    {
        var center = Center();
        center.Publish(NotificationKind.Success, "Primer");
        _delay.Elapse(TimeSpan.FromSeconds(3));
        center.Publish(NotificationKind.Error, "Ha fallat");
        center.Publish(NotificationKind.Success, "Segon");

        Assert.Equal(3, center.Visible.Count);
        _delay.Elapse(TimeSpan.FromSeconds(2.1)); // the first success is now 5.1 s old

        Assert.Equal(["Ha fallat", "Segon"], center.Visible.Select(n => n.Text));
        _delay.Elapse(TimeSpan.FromSeconds(3));
        Assert.Equal(["Ha fallat"], center.Visible.Select(n => n.Text));
    }

    [Fact]
    [Trait("spec", Spec + ": Notificaciones de resultado (Varias notificaciones)")]
    public void Beyond_the_maximum_the_rest_wait_in_a_queue_and_appear_as_room_is_made()
    {
        var center = Center(max: 2);
        center.Publish(NotificationKind.Error, "Un");
        center.Publish(NotificationKind.Error, "Dos");
        center.Publish(NotificationKind.Success, "Tres");
        center.Publish(NotificationKind.Error, "Quatre");

        Assert.Equal(["Un", "Dos"], center.Visible.Select(n => n.Text));
        Assert.Equal(2, center.WaitingCount);
        Assert.Equal(4, center.History.Count);

        center.Dismiss(center.Visible[0].Id);
        Assert.Equal(["Dos", "Tres"], center.Visible.Select(n => n.Text));

        _delay.Elapse(TimeSpan.FromSeconds(5.1)); // the success starts counting when it appears, not before
        Assert.Equal(["Dos", "Quatre"], center.Visible.Select(n => n.Text));
    }

    [Fact]
    [Trait("spec", Spec + ": Notificaciones de resultado (Varias notificaciones)")]
    public void A_success_that_waited_in_the_queue_does_not_count_the_time_it_waited()
    {
        var center = Center(max: 1);
        center.Publish(NotificationKind.Error, "Un");
        center.Publish(NotificationKind.Success, "Dos");

        _delay.Elapse(TimeSpan.FromSeconds(30));
        center.Dismiss(center.Visible[0].Id);
        _delay.Elapse(TimeSpan.FromSeconds(4));

        Assert.Equal(["Dos"], center.Visible.Select(n => n.Text));
    }

    [Fact]
    [Trait("spec", Spec + ": Historial de notificaciones de la sesión (Consultar el historial)")]
    public void The_history_shows_the_session_newest_first_including_what_already_disappeared()
    {
        var center = Center();
        center.Publish(NotificationKind.Success, "Primer");
        _clock.Advance(TimeSpan.FromMinutes(1));
        center.Publish(NotificationKind.Warning, "Segon");
        _delay.Elapse(TimeSpan.FromSeconds(6));

        var entries = new NotificationHistoryViewModel(center, _localizer).Entries;

        Assert.Equal(["Segon", "Primer"], entries.Select(e => e.Text));
        Assert.Single(center.Visible);
    }

    [Fact]
    [Trait("spec", Spec + ": Historial de notificaciones de la sesión (Nueva sesión)")]
    public void A_new_session_starts_with_an_empty_history()
    {
        Assert.Empty(new NotificationHistoryViewModel(Center(), _localizer).Entries);
    }

    // --- Messages from stable codes ---

    ResultNotifier Notifier(RecordingNotifications notifications) => new(notifications, _localizer, _log);

    [Fact]
    [Trait("spec", Spec + ": Mensajes desde códigos estables (Código conocido)")]
    public void A_known_error_code_is_composed_from_its_resource_and_parameters()
    {
        var notifications = new RecordingNotifications();

        Notifier(notifications).Error(Arca.Domain.Charges.ChargeErrors.ReasonTooLong(500));

        var only = Assert.Single(notifications.Published);
        Assert.Equal(NotificationKind.Error, only.Kind);
        Assert.Equal("El motiu no pot tenir més de 500 caràcters.", only.Text);
    }

    [Fact]
    [Trait("spec", Spec + ": Mensajes desde códigos estables (Recurso ausente)")]
    public void A_missing_resource_shows_the_key_and_is_logged_once()
    {
        var notifications = new RecordingNotifications();
        var notifier = Notifier(notifications);

        notifier.Error(new Error("Nowhere.Missing"));
        notifier.Error(new Error("Nowhere.Missing"));

        Assert.All(notifications.Published, n => Assert.Equal("Nowhere.Error.Missing", n.Text));
        var entry = Assert.Single(_log.Entries);
        Assert.Equal("MissingResource:Nowhere.Error.Missing", entry.Context);
        Assert.IsType<MissingResourceException>(entry.Error);
    }

    [Fact]
    [Trait("spec", Spec + ": Notificaciones de resultado (Aviso)")]
    public void A_successful_result_with_a_notice_publishes_the_success_and_then_the_warning()
    {
        var notifications = new RecordingNotifications();

        Notifier(notifications).Notify(Result<int>.Success(3, new Notice("Charges.BulkChanged")), n => $"{n} fets");

        Assert.Equal([NotificationKind.Success, NotificationKind.Warning], notifications.Published.Select(p => p.Kind));
        Assert.Equal("3 fets", notifications.Published[0].Text);
        Assert.Contains("Les dades han canviat", notifications.Published[1].Text);
    }

    [Fact]
    [Trait("spec", Spec + ": Notificaciones de resultado (Error inesperado)")]
    public void An_unexpected_error_shows_a_generic_message_with_the_reference_and_technical_details_without_data()
    {
        var notifications = new RecordingNotifications();
        var error = new InvalidOperationException("No es pot assignar la taquilla a Núria Garcia Puig");

        Notifier(notifications).Unexpected(error, "AssignLocker");

        var only = Assert.Single(notifications.Published);
        Assert.Contains("REF1", only.Text);
        Assert.DoesNotContain("Núria", only.Text);
        Assert.Equal("Referència REF1 · System.InvalidOperationException", only.Details);
        Assert.DoesNotContain("Núria", only.Details);
    }

    // --- Cancel reason ---

    [Fact]
    [Trait("spec", Spec + ": Indicador de trabajo y progreso (Cancelación no disponible)")]
    public async Task While_it_cannot_be_cancelled_the_command_says_why()
    {
        var command = new RunOnceCommand<int>(
            async (_, progress) =>
            {
                progress.Report(new OperationProgress(0, 10, CanCancel: false));
                await Task.Delay(1);
                return Result<int>.Success(1);
            },
            n => n.ToString(System.Globalization.CultureInfo.InvariantCulture), "Op", new RecordingNotifications(), _localizer, _log, _delay);
        var reasons = new List<string>();
        command.PropertyChanged += (_, e) => reasons.Add(e.PropertyName ?? string.Empty);

        await command.RunAsync();

        Assert.Contains(nameof(IWorkState.CancelDisabledReason), reasons);
        Assert.Equal(string.Empty, command.CancelDisabledReason); // nothing runs any more
    }

    // --- Lists: loading and empty ---

    [Fact]
    [Trait("spec", Spec + ": Estados vacíos y de carga (Carga)")]
    public void A_list_that_is_loading_shows_loading_and_not_an_empty_state()
    {
        var model = new ListStateViewModel(_localizer);

        model.BeginLoading();

        Assert.True(model.IsLoading);
        Assert.False(model.IsEmpty);
        Assert.False(model.IsContent);
        Assert.Equal("Carregant…", model.LoadingText);
    }

    [Fact]
    [Trait("spec", Spec + ": Estados vacíos y de carga (Lista vacía)")]
    public void An_empty_list_explains_what_to_do_and_offers_the_main_action()
    {
        var model = new ListStateViewModel(_localizer);
        var action = new EmptyStateAction("Afegeix la primera zona", new NoOpCommand());

        model.ShowEmpty("Encara no hi ha zones. Crea'n una per començar.", action);

        Assert.True(model.IsEmpty);
        Assert.Equal("Encara no hi ha zones. Crea'n una per començar.", model.Message);
        Assert.Equal([action], model.Actions);
    }

    [Fact]
    [Trait("spec", Spec + ": Estados vacíos y de carga (Filtro sin resultados)")]
    public void A_filter_without_results_says_so_and_offers_to_clear_it()
    {
        var model = new ListStateViewModel(_localizer);

        model.ShowNoResults(new EmptyStateAction("Neteja els filtres", new NoOpCommand()));

        Assert.True(model.IsEmpty);
        Assert.Equal(ListViewState.NoResults, model.State);
        Assert.Equal("Neteja els filtres", Assert.Single(model.Actions).Label);
        model.ShowContent();
        Assert.False(model.IsEmpty);
        Assert.Empty(model.Actions);
    }

    // --- Views ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": Notificaciones de resultado (Varias notificaciones)")]
    public void The_host_draws_one_card_per_visible_notification_and_closing_one_removes_it()
    {
        var center = Center();
        center.Publish(NotificationKind.Error, "Ha fallat", "Referència X");
        center.Publish(NotificationKind.Success, "Fet");
        var host = new NotificationHostView(center, _localizer);
        var window = new Window { Content = host };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var cards = host.GetVisualDescendants().OfType<Border>().Where(b => b.Child is StackPanel).ToList();
        Assert.Equal(2, cards.Count);

        var close = cards[0].GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Tanca"));
        close.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["Fet"], center.Visible.Select(n => n.Text));
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Notificaciones de resultado (Error inesperado)")]
    public void Technical_details_are_hidden_until_asked_for()
    {
        var center = Center();
        center.Publish(NotificationKind.Error, "Error inesperat", "Referència X · System.IOException");
        var host = new NotificationHostView(center, _localizer);
        var window = new Window { Content = host };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var details = host.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "Referència X · System.IOException");
        Assert.False(details.IsVisible);

        var toggle = host.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.ToggleButton>().Single();
        toggle.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        Assert.True(details.IsVisible);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Componentes sin colores ni textos propios (Cambio de tema)")]
    public void The_cards_take_their_colour_from_the_theme_resources_so_another_theme_needs_no_code_change()
    {
        var center = Center();
        center.Publish(NotificationKind.Success, "Fet");
        var host = new NotificationHostView(center, _localizer);
        var other = new SolidColorBrush(Colors.Magenta);
        host.Resources[ArcaResourceKeys.Success] = other;
        var window = new Window { Content = host };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var card = host.GetVisualDescendants().OfType<Border>().Single(b => b.Child is StackPanel);

        Assert.Same(other, card.Background);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Estados vacíos y de carga (Lista vacía)")]
    public void The_list_state_view_draws_the_empty_state_with_its_button_and_hides_when_there_is_content()
    {
        var model = new ListStateViewModel(_localizer);
        var view = new ListStateView(model);
        var window = new Window { Content = view };
        window.Show();

        model.ShowEmpty("Encara no hi ha zones.", new EmptyStateAction("Afegeix la primera zona", new NoOpCommand()));
        Dispatcher.UIThread.RunJobs();
        Assert.True(view.IsVisible);
        Assert.Equal("Afegeix la primera zona", Assert.Single(view.ActionButtons).Content);

        model.ShowContent();
        Dispatcher.UIThread.RunJobs();
        Assert.False(view.IsVisible);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Indicador de trabajo y progreso (Cancelación no disponible)")]
    public void The_work_indicator_disables_cancel_and_explains_why_when_it_is_no_longer_safe()
    {
        var work = new FakeWork { IsRunning = true, CanCancel = false, CancelDisabledReason = "Ja no es pot cancel·lar", ProgressText = "120 de 300" };
        var view = new WorkIndicatorView(work, _localizer);
        var window = new Window { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.False(view.Cancel.IsEnabled);
        Assert.Equal("Ja no es pot cancel·lar", ToolTip.GetTip(view.Cancel));
        Assert.Contains(view.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "120 de 300");
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Diálogo de confirmación con consecuencia (Un solo diálogo)")]
    public async Task While_a_confirmation_is_open_another_request_is_not_opened_and_is_not_confirmed()
    {
        var owner = new Window();
        owner.Show();
        var service = new DialogConfirmationService(() => owner, _localizer);
        var request = new ConfirmationRequest("Condonar 3 càrrecs?", "Es condonaran.", "Condona");

        var first = service.ConfirmAsync(request);
        Dispatcher.UIThread.RunJobs();
        var second = await service.ConfirmAsync(request);

        Assert.False(second);
        var dialog = Assert.Single(owner.OwnedWindows);
        ((ConfirmationWindow)dialog).ConfirmButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.True(await first);
    }

    sealed class FakeWork : IWorkState
    {
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }

        public bool IsRunning { get; set; }

        public bool ShowBusyIndicator { get; set; }

        public bool CanCancel { get; set; }

        public string ProgressText { get; set; } = string.Empty;

        public string CancelDisabledReason { get; set; } = string.Empty;

        public void Cancel()
        {
        }
    }
}
