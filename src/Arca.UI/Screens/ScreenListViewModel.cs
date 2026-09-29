// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Lists;
using Arca.UI.Notifications;

namespace Arca.UI.Screens;

/// <summary>
/// The list half of a domain screen (pantalles-de-domini, D1): it asks Application for its rows through one query, keeps the
/// ordering, the filter and the selection of <see cref="ListViewModel{TRow, TKey}"/>, and says what the list shows now (loading,
/// rows, nothing yet, or a filter that left nothing) so the person never sees a blank area. The row chosen is kept by identity,
/// so loading the data again after a change keeps it while it still exists. It has no window and no rules of its own.
/// </summary>
/// <typeparam name="TRow">The kind of row, a transfer object without email or identifier.</typeparam>
/// <typeparam name="TKey">What identifies a row.</typeparam>
public sealed class ScreenListViewModel<TRow, TKey> : ObservableObject
    where TRow : class
    where TKey : notnull
{
    readonly Func<CancellationToken, Task<Result<IReadOnlyList<TRow>>>> _query;
    readonly Func<TRow, TKey> _key;
    readonly Func<EmptyStateAction?>? _emptyAction;
    readonly Func<string?>? _emptyMessage;
    readonly ResultNotifier _notifier;
    readonly ILocalizer _localizer;
    int _request;
    bool _hasKey;
    TKey? _selectedKey;
    TRow? _current;

    /// <param name="query">The list query of Application. It is the only thing the screen asks for its rows.</param>
    /// <param name="emptyMessage">What to say when there is nothing yet, if the general text does not do; it may depend on the state of the data.</param>
    /// <param name="emptyAction">The main action offered under an empty list, such as creating the first one.</param>
    public ScreenListViewModel(
        IReadOnlyList<ListColumn<TRow>> columns, Func<TRow, TKey> key, Func<CancellationToken, Task<Result<IReadOnlyList<TRow>>>> query,
        ILocalizer localizer, INotificationService notifications, IErrorLog log, Func<string?>? emptyMessage = null, Func<EmptyStateAction?>? emptyAction = null)
    {
        _key = key;
        _query = query;
        _localizer = localizer;
        _emptyMessage = emptyMessage;
        _emptyAction = emptyAction;
        _notifier = new ResultNotifier(notifications, localizer, log);
        List = new ListViewModel<TRow, TKey>(columns, key, localizer);
        State = new ListStateViewModel(localizer);
        List.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ListViewModel<TRow, TKey>.Rows))
            {
                ShowState();
            }
        };
    }

    public ListViewModel<TRow, TKey> List { get; }

    public ListStateViewModel State { get; }

    /// <summary>The row chosen to show its detail, or null when none is.</summary>
    public TRow? Current
    {
        get => _current;
        private set => Set(ref _current, value);
    }

    /// <summary>The identity of the row chosen, when there is one.</summary>
    public bool TryGetSelectedKey(out TKey key)
    {
        key = _selectedKey!;
        return _hasKey;
    }

    /// <summary>Raised when the row chosen changes, so the detail can load it.</summary>
    public event EventHandler? CurrentChanged;

    /// <summary>Chooses a row, or none with null.</summary>
    public void Select(TRow? row)
    {
        _hasKey = row is not null;
        _selectedKey = row is null ? default : _key(row);
        Current = row;
        CurrentChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Removes the text and the other condition of the filter, what the state of a filter without results offers.</summary>
    public void ClearFilter()
    {
        List.SetPredicate(null);
        List.FilterText = string.Empty;
    }

    /// <summary>Fetches the rows. Only the answer to the latest request is used, so a slow older one never overwrites a newer one.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        var mine = ++_request;
        if (List.TotalCount == 0)
        {
            State.BeginLoading();
        }

        try
        {
            var result = await _query(ct);
            if (mine != _request)
            {
                return;
            }

            if (!result.IsSuccess)
            {
                _notifier.Error(result.Error!);
                ShowState();
                return;
            }

            List.SetItems(result.Value!);
            var row = _hasKey ? result.Value!.FirstOrDefault(r => _key(r).Equals(_selectedKey)) : null;
            if (_hasKey && row is null)
            {
                Select(null); // what was chosen is gone
            }
            else if (row is not null)
            {
                Current = row; // the same identity, with its new data
            }

            ShowState();
        }
        catch (OperationCanceledException)
        {
            // A newer request took over.
        }
        catch (Exception e)
        {
            if (mine == _request)
            {
                _notifier.Unexpected(e, "ListLoad");
                ShowState();
            }
        }
    }

    void ShowState()
    {
        if (List.Rows.Count > 0)
        {
            State.ShowContent();
        }
        else if (List.TotalCount == 0)
        {
            State.ShowEmpty(_emptyMessage?.Invoke(), _emptyAction?.Invoke());
        }
        else
        {
            var clear = new AppAction("ClearFilter", _localizer.Get("Common.Action.ClearFilter"));
            clear.Attach(ClearFilter);
            State.ShowNoResults(new EmptyStateAction(clear.Label, clear));
        }
    }
}
