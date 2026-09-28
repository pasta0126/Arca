// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Windows.Input;
using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.UI.Common;
using Arca.UI.Notifications;

namespace Arca.UI.Commands;

/// <summary>
/// The one way a screen runs an action that changes data (arquitectura-base, D14). While it runs the command
/// cannot run again (a double click executes once), a busy indicator appears only after 300 ms so quick actions
/// do not flicker, progress and cancellation are exposed, and the outcome is turned into a notification:
/// success with what happened, warnings, a business error in plain language, or an unexpected error with a log reference.
/// </summary>
/// <typeparam name="T">The data the operation returns on success.</typeparam>
public sealed class RunOnceCommand<T> : ObservableObject, ICommand, IWorkState
{
    public static readonly TimeSpan BusyIndicatorDelay = TimeSpan.FromMilliseconds(300);

    readonly Func<CancellationToken, IProgress<OperationProgress>, Task<Result<T>>> _operation;
    readonly Func<T, string> _successText;
    readonly string _context;
    readonly ResultNotifier _notifier;
    readonly ILocalizer _localizer;
    readonly IDelay _delay;

    CancellationTokenSource? _running;
    bool _isRunning;
    bool _showBusyIndicator;
    bool _canCancel;
    string _progressText = string.Empty;

    /// <param name="operation">
    /// The work. It is cancellable only while it reports progress with CanCancel true, and it honours the token only then;
    /// an operation that never reports cannot be cancelled, which is the safe default for a data write.
    /// </param>
    /// <param name="successText">Message for a successful result, with its counts ("40 taquilles creades").</param>
    /// <param name="context">Name of the action, for the technical log.</param>
    public RunOnceCommand(
        Func<CancellationToken, IProgress<OperationProgress>, Task<Result<T>>> operation,
        Func<T, string> successText,
        string context,
        INotificationService notifications,
        ILocalizer localizer,
        IErrorLog log,
        IDelay delay)
    {
        _operation = operation;
        _successText = successText;
        _context = context;
        _notifier = new ResultNotifier(notifications, localizer, log);
        _localizer = localizer;
        _delay = delay;
    }

    public event EventHandler? CanExecuteChanged;

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (Set(ref _isRunning, value))
            {
                Raise(nameof(CancelDisabledReason));
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>True once the action has lasted more than 300 ms.</summary>
    public bool ShowBusyIndicator
    {
        get => _showBusyIndicator;
        private set => Set(ref _showBusyIndicator, value);
    }

    public bool CanCancel
    {
        get => _canCancel;
        private set
        {
            if (Set(ref _canCancel, value))
            {
                Raise(nameof(CancelDisabledReason));
            }
        }
    }

    /// <summary>Why cancelling is not available while the action runs, or empty when it is available or nothing runs.</summary>
    public string CancelDisabledReason => IsRunning && !CanCancel ? _localizer.Get("Common.Label.CancelUnavailable") : string.Empty;

    /// <summary>"120 de 300" while a long operation reports progress, otherwise empty.</summary>
    public string ProgressText
    {
        get => _progressText;
        private set => Set(ref _progressText, value);
    }

    public bool CanExecute(object? parameter) => !IsRunning;

    public void Execute(object? parameter) => _ = RunAsync();

    /// <summary>Asks the running operation to stop. Ignored once it can no longer be cancelled safely.</summary>
    public void Cancel()
    {
        if (CanCancel)
        {
            _running?.Cancel();
        }
    }

    /// <summary>Runs the action and completes when it is done. Calling it while it runs does nothing.</summary>
    public async Task RunAsync()
    {
        if (IsRunning)
        {
            return;
        }

        using var cancellation = new CancellationTokenSource();
        using var indicator = new CancellationTokenSource();
        _running = cancellation;
        IsRunning = true;
        CanCancel = false; // an operation is cancellable only once it says so through its progress
        ProgressText = string.Empty;
        _ = ShowIndicatorLaterAsync(indicator.Token);
        try
        {
            var progress = new SyncProgress(this);
            var result = await _operation(cancellation.Token, progress);
            _notifier.Notify(result, _successText);
        }
        catch (OperationCanceledException)
        {
            _notifier.Cancelled();
        }
        catch (Exception e)
        {
            _notifier.Unexpected(e, _context);
        }
        finally
        {
            await indicator.CancelAsync();
            ShowBusyIndicator = false;
            CanCancel = false;
            ProgressText = string.Empty;
            _running = null;
            IsRunning = false;
        }
    }

    async Task ShowIndicatorLaterAsync(CancellationToken ct)
    {
        try
        {
            await _delay.DelayAsync(BusyIndicatorDelay, ct);
            if (IsRunning)
            {
                ShowBusyIndicator = true;
            }
        }
        catch (OperationCanceledException)
        {
            // The action finished first: no indicator, which is the point of the delay.
        }
    }

    sealed class SyncProgress(RunOnceCommand<T> owner) : IProgress<OperationProgress>
    {
        public void Report(OperationProgress value)
        {
            owner.CanCancel = value.CanCancel;
            owner.ProgressText = owner._localizer.Get("Common.Label.ProgressOf", value.Current, value.Total);
        }
    }
}
