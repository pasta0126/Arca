// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Notifications;

namespace Arca.UI.Screens;

/// <summary>
/// The detail half of a domain screen (pantalles-de-domini, D1): the element chosen in the list, the actions that apply to it
/// and its history. Actions are built from the element loaded, so their availability and reasons come from what Application
/// answered, and the history is fetched only when it is asked for (D4: the tabs load when they open). Only the answer to the
/// latest request is shown.
/// </summary>
/// <typeparam name="TKey">What identifies the element.</typeparam>
/// <typeparam name="TDetail">The transfer object of the element.</typeparam>
public sealed class DetailViewModel<TKey, TDetail> : ObservableObject
    where TKey : notnull
    where TDetail : class
{
    readonly Func<TKey, CancellationToken, Task<Result<TDetail?>>> _load;
    readonly Func<TDetail, IReadOnlyList<AppAction>> _actions;
    readonly Func<TKey, CancellationToken, Task<Result<IReadOnlyList<string>>>>? _history;
    readonly ResultNotifier _notifier;
    TKey? _key;
    bool _hasKey;
    int _request;
    TDetail? _detail;
    IReadOnlyList<AppAction> _current = [];
    IReadOnlyList<string> _historyLines = [];
    bool _historyLoaded;

    public DetailViewModel(
        Func<TKey, CancellationToken, Task<Result<TDetail?>>> load, Func<TDetail, IReadOnlyList<AppAction>> actions,
        ILocalizer localizer, INotificationService notifications, IErrorLog log,
        Func<TKey, CancellationToken, Task<Result<IReadOnlyList<string>>>>? history = null)
    {
        _load = load;
        _actions = actions;
        _history = history;
        _notifier = new ResultNotifier(notifications, localizer, log);
    }

    /// <summary>The element shown, or null when none is chosen or it no longer exists.</summary>
    public TDetail? Detail
    {
        get => _detail;
        private set => Set(ref _detail, value);
    }

    /// <summary>The actions of the element in the order they are offered.</summary>
    public IReadOnlyList<AppAction> Actions
    {
        get => _current;
        private set => Set(ref _current, value);
    }

    /// <summary>The history of the element, once <see cref="LoadHistoryAsync"/> has run.</summary>
    public IReadOnlyList<string> History
    {
        get => _historyLines;
        private set => Set(ref _historyLines, value);
    }

    public bool HistoryLoaded => _historyLoaded;

    /// <summary>Shows the element, or nothing with false. Called when one is chosen and again after every change.</summary>
    public async Task ShowAsync(bool has, TKey? key, CancellationToken ct = default)
    {
        var mine = ++_request;
        _hasKey = has;
        _key = key;
        if (!has)
        {
            Clear();
            return;
        }

        try
        {
            var result = await _load(key!, ct);
            if (mine != _request)
            {
                return;
            }

            if (!result.IsSuccess)
            {
                _notifier.Error(result.Error!);
                return;
            }

            Detail = result.Value;
            Actions = result.Value is null ? [] : _actions(result.Value);
            if (_historyLoaded)
            {
                await LoadHistoryAsync(ct); // an open history follows the element
            }
        }
        catch (OperationCanceledException)
        {
            // Another element was chosen meanwhile.
        }
        catch (Exception e)
        {
            if (mine == _request)
            {
                _notifier.Unexpected(e, "DetailLoad");
            }
        }
    }

    /// <summary>Fetches the history, when the person opens it.</summary>
    public async Task LoadHistoryAsync(CancellationToken ct = default)
    {
        if (_history is null || !_hasKey)
        {
            return;
        }

        var mine = _request;
        try
        {
            var result = await _history(_key!, ct);
            if (mine != _request)
            {
                return;
            }

            if (result.IsSuccess)
            {
                History = result.Value!;
                _historyLoaded = true;
                Raise(nameof(HistoryLoaded));
            }
            else
            {
                _notifier.Error(result.Error!);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            _notifier.Unexpected(e, "HistoryLoad");
        }
    }

    public void Clear()
    {
        Detail = null;
        Actions = [];
        History = [];
        _historyLoaded = false;
        Raise(nameof(HistoryLoaded));
    }
}
