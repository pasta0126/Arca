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
using Arca.UI.Shell;

namespace Arca.UI.Screens;

/// <summary>
/// The list half of a domain screen (pantalles-de-domini, D1): it asks Application for its rows through one query, keeps the
/// ordering, the filter and the selection of <see cref="ListViewModel{TRow, TKey}"/>, and says what the list shows now (loading,
/// rows, nothing yet, or a filter that left nothing) so the person never sees a blank area. The row chosen is kept by identity,
/// so loading the data again after a change keeps it while it still exists. It has no window and no rules of its own.
/// </summary>
/// <typeparam name="TRow">The kind of row, a transfer object without email or identifier.</typeparam>
/// <typeparam name="TKey">What identifies a row.</typeparam>
public sealed class ScreenListViewModel<TRow, TKey> : ObservableObject, ISelectionOwner
    where TRow : class
    where TKey : notnull
{
    readonly Func<CancellationToken, Task<Result<IReadOnlyList<TRow>>>> _query;
    readonly Func<TRow, TKey> _key;
    readonly Func<EmptyStateAction?>? _emptyAction;
    readonly Func<IReadOnlyList<EmptyStateAction>>? _emptyActions;
    readonly Func<string?>? _emptyMessage;
    readonly ResultNotifier _notifier;
    readonly ILocalizer _localizer;
    Func<IReadOnlyList<ListFilterTag>>? _filterTags;
    Action? _resetFilters;
    int _request;
    bool _hasKey;
    TKey? _selectedKey;
    TRow? _current;

    /// <param name="query">The list query of Application. It is the only thing the screen asks for its rows.</param>
    /// <param name="emptyMessage">What to say when there is nothing yet, if the general text does not do; it may depend on the state of the data.</param>
    /// <param name="emptyAction">The main action offered under an empty list, such as creating the first one.</param>
    /// <param name="emptyActions">Several actions to offer under an empty list, instead of one.</param>
    public ScreenListViewModel(
        IReadOnlyList<ListColumn<TRow>> columns, Func<TRow, TKey> key, Func<CancellationToken, Task<Result<IReadOnlyList<TRow>>>> query,
        ILocalizer localizer, INotificationService notifications, IErrorLog log, Func<string?>? emptyMessage = null, Func<EmptyStateAction?>? emptyAction = null,
        Func<IReadOnlyList<EmptyStateAction>>? emptyActions = null)
    {
        _key = key;
        _query = query;
        _localizer = localizer;
        _emptyMessage = emptyMessage;
        _emptyAction = emptyAction;
        _emptyActions = emptyActions;
        _notifier = new ResultNotifier(notifications, localizer, log);
        List = new ListViewModel<TRow, TKey>(columns, key, localizer);
        State = new ListStateViewModel(localizer);
        Reset = new AppAction("ResetList", localizer.Get("Common.Action.ResetList"));
        Reset.Attach(ClearFilter, () => HasActiveFilters ? Availability.Available : Availability.Unavailable(localizer.Get("Common.Reason.NothingToReset")));
        List.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ListViewModel<TRow, TKey>.Rows))
            {
                ShowState();
            }

            if (e.PropertyName is nameof(ListViewModel<TRow, TKey>.Rows) or nameof(ListViewModel<TRow, TKey>.FilterText) or nameof(ListViewModel<TRow, TKey>.TotalCount))
            {
                RefreshFilters();
            }
        };
    }

    /// <summary>Puts the search and every filter back as the list starts: the action of the Reset button, available only when there is something to put back.</summary>
    public AppAction Reset { get; }

    /// <summary>The filters that are on, other than the search text, each one removable on its own.</summary>
    public IReadOnlyList<ListFilterTag> ActiveFilters => _filterTags?.Invoke() ?? [];

    /// <summary>True when there is search text or any filter on, so there is something to reset.</summary>
    public bool HasActiveFilters => List.FilterText.Length > 0 || ActiveFilters.Count > 0;

    /// <summary>"12 de 600": the rows shown out of all the rows the list has.</summary>
    public string CountText => _localizer.Get("Common.Label.ProgressOf", List.Rows.Count, List.TotalCount);

    /// <summary>
    /// Tells the list which filters the screen has: the labels of those that are on, and how to put all of them back. The screen
    /// calls <see cref="RefreshFilters"/> whenever one of them changes.
    /// </summary>
    public void UseFilters(Func<IReadOnlyList<ListFilterTag>> tags, Action reset)
    {
        _filterTags = tags;
        _resetFilters = reset;
        RefreshFilters();
    }

    /// <summary>Tells the views that the filters or the rows shown changed, so the labels, the count and the Reset button redraw.</summary>
    public void RefreshFilters()
    {
        Raise(nameof(ActiveFilters));
        Raise(nameof(HasActiveFilters));
        Raise(nameof(CountText));
        Reset.Refresh();
    }

    /// <summary>True while a row is chosen.</summary>
    public bool HasSelection => _hasKey;

    /// <summary>Chooses no row, which leaves the detail on its "choose one" state. What Esc does on a list.</summary>
    public void ClearSelection()
    {
        if (_hasKey)
        {
            Select(null);
        }
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
        List.FilterText = string.Empty;
        if (_resetFilters is not null)
        {
            _resetFilters(); // the screen knows its own filters and their controls
        }
        else
        {
            List.SetPredicate(null);
        }

        RefreshFilters();
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
            if (_emptyActions is not null)
            {
                State.ShowEmpty(_emptyMessage?.Invoke(), _emptyActions());
            }
            else
            {
                State.ShowEmpty(_emptyMessage?.Invoke(), _emptyAction?.Invoke());
            }
        }
        else
        {
            var clear = new AppAction("ClearFilter", _localizer.Get("Common.Action.ClearFilter"));
            clear.Attach(ClearFilter);
            State.ShowNoResults(new EmptyStateAction(clear.Label, clear));
        }
    }
}
