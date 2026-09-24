// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Commands;
using Xunit;

namespace Arca.UI.Tests;

public sealed class RunOnceCommandTests
{
    const string Spec = "arquitectura-base/feedback-operacions";

    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    readonly ManualDelay _delay = new();
    readonly ResxLocalizer _localizer = new();

    RunOnceCommand<int> Command(Func<CancellationToken, IProgress<OperationProgress>, Task<Result<int>>> operation) =>
        new(operation, count => $"{count} taquilles creades", "CreateLockerRange", _notifications, _localizer, _log, _delay);

    [Fact]
    [Trait("spec", Spec + ": Resultado visible de cada acción (éxito con datos)")]
    public async Task Success_tells_what_happened_with_its_count()
    {
        var command = Command((_, _) => Task.FromResult(Result<int>.Success(40)));

        await command.RunAsync();

        var only = Assert.Single(_notifications.Published);
        Assert.Equal(NotificationKind.Success, only.Kind);
        Assert.Equal("40 taquilles creades", only.Text);
    }

    [Fact]
    [Trait("spec", Spec + ": Resultado visible de cada acción (error de negocio)")]
    public async Task A_business_error_is_explained_in_plain_catalan_without_technical_text()
    {
        var command = Command((_, _) => Task.FromResult(Result<int>.Failure(Arca.Application.Storage.StorageErrors.SchemaNewer)));

        await command.RunAsync();

        var only = Assert.Single(_notifications.Published);
        Assert.Equal(NotificationKind.Error, only.Kind);
        Assert.Contains("versió més nova", only.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Storage.", only.Text, StringComparison.Ordinal);
        Assert.Empty(_log.Entries); // a business error is not an unexpected failure
    }

    [Fact]
    [Trait("spec", Spec + ": Resultado visible de cada acción (aviso con éxito parcial)")]
    public async Task Success_with_a_warning_reports_what_was_done_and_what_was_skipped()
    {
        var command = Command((_, _) => Task.FromResult(Result<int>.Success(38, new Notice("Storage.Unreadable", ["fila 3"]))));

        await command.RunAsync();

        Assert.Equal([NotificationKind.Success, NotificationKind.Warning], _notifications.Published.Select(p => p.Kind));
        Assert.Equal("38 taquilles creades", _notifications.Published[0].Text);
    }

    [Fact]
    [Trait("spec", Spec + ": Resultado visible de cada acción (error inesperado)")]
    public async Task An_unexpected_failure_shows_a_reference_and_hides_the_exception_message()
    {
        var command = Command((_, _) => throw new InvalidOperationException("Núria Garcia 3r ESO B"));

        await command.RunAsync();

        var only = Assert.Single(_notifications.Published);
        Assert.Equal(NotificationKind.Error, only.Kind);
        Assert.Contains("REF1", only.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Núria", only.Text, StringComparison.Ordinal);
        Assert.Equal("CreateLockerRange", Assert.Single(_log.Entries).Context);
        Assert.False(command.IsRunning); // the command is usable again
    }

    [Fact]
    [Trait("spec", Spec + ": Indicador de trabajo sin bloquear la interfaz (acción rápida)")]
    public async Task A_quick_action_never_shows_the_busy_indicator()
    {
        var command = Command((_, _) => Task.FromResult(Result<int>.Success(1)));
        var everShown = false;
        command.PropertyChanged += (_, e) => everShown |= e.PropertyName == nameof(command.ShowBusyIndicator) && command.ShowBusyIndicator;

        await command.RunAsync();
        _delay.Elapse(TimeSpan.FromSeconds(1));

        Assert.False(everShown);
        Assert.False(command.ShowBusyIndicator);
    }

    [Fact]
    [Trait("spec", Spec + ": Indicador de trabajo sin bloquear la interfaz (acción lenta)")]
    public async Task A_slow_action_shows_the_indicator_after_300_ms_and_hides_it_when_done()
    {
        var release = new TaskCompletionSource<Result<int>>();
        var command = Command((_, _) => release.Task);

        var run = command.RunAsync();
        _delay.Elapse(TimeSpan.FromMilliseconds(299));
        Assert.False(command.ShowBusyIndicator, "not yet at 299 ms");

        _delay.Elapse(TimeSpan.FromMilliseconds(1));
        Assert.True(command.ShowBusyIndicator, "shown at 300 ms");

        release.SetResult(Result<int>.Success(5));
        await run;
        Assert.False(command.ShowBusyIndicator);
        Assert.False(command.IsRunning);
    }

    [Fact]
    [Trait("spec", Spec + ": Indicador de trabajo sin bloquear la interfaz (doble ejecución)")]
    public async Task Pressing_twice_runs_the_action_once_and_disables_the_command_meanwhile()
    {
        var calls = 0;
        var release = new TaskCompletionSource<Result<int>>();
        var command = Command((_, _) =>
        {
            calls++;
            return release.Task;
        });

        var first = command.RunAsync();
        var second = command.RunAsync(); // a second click while the first is running
        Assert.False(command.CanExecute(null));

        release.SetResult(Result<int>.Success(1));
        await Task.WhenAll(first, second);

        Assert.Equal(1, calls);
        Assert.Single(_notifications.Published);
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public async Task Can_execute_changes_are_announced_so_the_button_is_disabled()
    {
        var release = new TaskCompletionSource<Result<int>>();
        var command = Command((_, _) => release.Task);
        var changes = 0;
        command.CanExecuteChanged += (_, _) => changes++;

        var run = command.RunAsync();
        release.SetResult(Result<int>.Success(1));
        await run;

        Assert.Equal(2, changes); // disabled at the start, enabled again at the end
    }

    [Fact]
    [Trait("spec", Spec + ": Progreso y cancelación de operaciones largas (progreso con recuentos)")]
    public async Task Progress_is_shown_as_done_of_total()
    {
        var release = new TaskCompletionSource<Result<int>>();
        var seen = new List<string>();
        var command = Command((_, progress) =>
        {
            progress.Report(new OperationProgress(120, 300));
            return release.Task;
        });
        command.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(command.ProgressText))
            {
                seen.Add(command.ProgressText);
            }
        };

        var run = command.RunAsync();
        Assert.Equal("120 de 300", command.ProgressText);
        release.SetResult(Result<int>.Success(300));
        await run;

        Assert.Contains("120 de 300", seen);
        Assert.Equal(string.Empty, command.ProgressText);
    }

    [Fact]
    [Trait("spec", Spec + ": Progreso y cancelación de operaciones largas (cancelar antes de guardar)")]
    public async Task Cancelling_before_saving_stops_without_changing_data_and_says_so()
    {
        var saved = false;
        var started = new TaskCompletionSource();
        var command = Command(async (token, progress) =>
        {
            progress.Report(new OperationProgress(10, 300, CanCancel: true));
            started.SetResult();
            await Task.Delay(Timeout.Infinite, token); // the operation honours the token while it can be cancelled
            saved = true;
            return Result<int>.Success(1);
        });

        var run = command.RunAsync();
        await started.Task;
        Assert.True(command.CanCancel);
        command.Cancel();
        await run;

        Assert.False(saved);
        var only = Assert.Single(_notifications.Published);
        Assert.Equal(NotificationKind.Warning, only.Kind);
        Assert.Contains("No s'ha canviat cap dada", only.Text, StringComparison.Ordinal);
        Assert.Empty(_log.Entries); // cancelling is not a failure
    }

    [Fact]
    [Trait("spec", Spec + ": Progreso y cancelación de operaciones largas (fase no cancelable)")]
    public async Task Once_saving_starts_cancelling_is_disabled_and_ignored()
    {
        var saving = new TaskCompletionSource();
        var finish = new TaskCompletionSource<Result<int>>();
        var tokenSeen = default(CancellationToken);
        var command = Command((token, progress) =>
        {
            tokenSeen = token;
            progress.Report(new OperationProgress(300, 300, CanCancel: false)); // the indivisible save begins
            saving.SetResult();
            return finish.Task;
        });

        var run = command.RunAsync();
        await saving.Task;
        Assert.False(command.CanCancel);
        command.Cancel();

        Assert.False(tokenSeen.IsCancellationRequested, "the request must not reach an operation that can no longer stop");
        finish.SetResult(Result<int>.Success(300));
        await run;
        Assert.Equal(NotificationKind.Success, Assert.Single(_notifications.Published).Kind);
    }

    [Fact]
    [Trait("spec", Spec + ": Progreso y cancelación de operaciones largas (fase no cancelable)")]
    public async Task An_operation_that_never_reports_cannot_be_cancelled()
    {
        var release = new TaskCompletionSource<Result<int>>();
        var tokenSeen = default(CancellationToken);
        var command = Command((token, _) =>
        {
            tokenSeen = token;
            return release.Task;
        });

        var run = command.RunAsync();
        Assert.False(command.CanCancel, "cancellable only once the operation says so");
        command.Cancel();

        Assert.False(tokenSeen.IsCancellationRequested);
        release.SetResult(Result<int>.Success(1));
        await run;
        Assert.Equal(NotificationKind.Success, Assert.Single(_notifications.Published).Kind);
    }

    [Fact]
    public async Task The_command_can_run_again_after_finishing()
    {
        var calls = 0;
        var command = Command((_, _) =>
        {
            calls++;
            return Task.FromResult(Result<int>.Success(calls));
        });

        await command.RunAsync();
        await command.RunAsync();

        Assert.Equal(2, calls);
        Assert.Equal(["1 taquilles creades", "2 taquilles creades"], _notifications.Published.Select(p => p.Text));
    }
}
