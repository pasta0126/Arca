// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Localization;
using Arca.Application.Search;
using Arca.Domain.Common;
using Arca.UI.Common;
using Arca.UI.Notifications;
using Arca.UI.Shell;

namespace Arca.UI.Search;

/// <summary>One line of the results: a heading, a result the person can open, or the offer to see the whole list.</summary>
/// <param name="Text">What the line says, already in the user's language.</param>
/// <param name="Target">Where opening it goes; null for a heading.</param>
public sealed record SearchItem(string Text, SearchTarget? Target)
{
    public bool IsHeading => Target is null;
}

/// <summary>What the search box is showing.</summary>
public enum SearchState
{
    /// <summary>Nothing typed: no results panel.</summary>
    Idle,

    Searching,
    Results,

    /// <summary>Nothing matches what was typed.</summary>
    Empty,
}

/// <summary>
/// The global search without any window (ui-shell, D4 and Búsqueda sin bloquear): it waits a short moment after the last key
/// so a quick typist causes one search, not one per letter, drops the search in progress when the text changes and shows only
/// the result of the last one, groups what it finds by kind, and is driven with the keyboard (arrows, Enter and Escape).
/// </summary>
public sealed class GlobalSearchViewModel : ObservableObject
{
    public static readonly TimeSpan Pause = TimeSpan.FromMilliseconds(250);

    readonly Func<GlobalSearchRequest, CancellationToken, Task<Result<GlobalSearchResult>>> _search;
    readonly IDelay _delay;
    readonly ResultNotifier _notifier;
    readonly SearchNavigator _navigator;
    readonly ILocalizer _localizer;
    CancellationTokenSource? _current;
    string _text = string.Empty;
    bool _includeRetired;
    SearchState _state = SearchState.Idle;
    IReadOnlyList<SearchItem> _items = [];
    string _message = string.Empty;
    int _selected = -1;
    int _retiredMatches;

    public GlobalSearchViewModel(
        Func<GlobalSearchRequest, CancellationToken, Task<Result<GlobalSearchResult>>> search, IDelay delay, ResultNotifier notifier,
        SearchNavigator navigator, ILocalizer localizer)
    {
        _search = search;
        _delay = delay;
        _notifier = notifier;
        _navigator = navigator;
        _localizer = localizer;
    }

    /// <summary>What the person has typed. Changing it starts a new search after the pause.</summary>
    public string Text
    {
        get => _text;
        set
        {
            if (Set(ref _text, value))
            {
                _ = SearchLaterAsync();
            }
        }
    }

    /// <summary>Also search the students who left.</summary>
    public bool IncludeRetired
    {
        get => _includeRetired;
        set
        {
            if (Set(ref _includeRetired, value))
            {
                _ = SearchLaterAsync();
            }
        }
    }

    public SearchState State
    {
        get => _state;
        private set
        {
            if (Set(ref _state, value))
            {
                Raise(nameof(IsOpen));
            }
        }
    }

    /// <summary>Whether the results panel is showing.</summary>
    public bool IsOpen => State != SearchState.Idle;

    /// <summary>The lines of the results panel, in order.</summary>
    public IReadOnlyList<SearchItem> Items
    {
        get => _items;
        private set => Set(ref _items, value);
    }

    /// <summary>What the panel says when there is nothing to list: "Searching…" or that nothing matched, and what to do about it.</summary>
    public string Message
    {
        get => _message;
        private set => Set(ref _message, value);
    }

    /// <summary>The line with the focus, or -1. It only ever rests on a result, never on a heading.</summary>
    public int SelectedIndex
    {
        get => _selected;
        private set => Set(ref _selected, value);
    }

    /// <summary>How many students who left match what was typed but are not being shown: the search offers to include them.</summary>
    public int RetiredMatches
    {
        get => _retiredMatches;
        private set => Set(ref _retiredMatches, value);
    }

    /// <summary>Runs the search of the current text now, after the pause. Only the last call to it matters.</summary>
    public async Task SearchLaterAsync()
    {
        _current?.Cancel();
        var source = new CancellationTokenSource();
        _current = source;
        var text = _text;
        if (string.IsNullOrWhiteSpace(text))
        {
            Clear();
            return;
        }

        State = SearchState.Searching;
        Message = _localizer.Get("Shell.Search.Searching");
        Items = [];
        SelectedIndex = -1;
        try
        {
            await _delay.DelayAsync(Pause, source.Token);
            var result = await _search(new GlobalSearchRequest(text, _includeRetired), source.Token);
            if (source.IsCancellationRequested)
            {
                return; // a newer search took over: its answer is the one to show
            }

            if (!result.IsSuccess)
            {
                _notifier.Error(result.Error!);
                Clear();
                return;
            }

            Show(result.Value!, text);
        }
        catch (OperationCanceledException)
        {
            // Replaced by a newer search, or closed.
        }
        catch (Exception e)
        {
            _notifier.Unexpected(e, "GlobalSearch");
            Clear();
        }
    }

    /// <summary>Escape: closes the panel and forgets what was typed.</summary>
    public void Close()
    {
        _current?.Cancel();
        if (_text.Length > 0)
        {
            Set(ref _text, string.Empty, nameof(Text));
        }

        Clear();
    }

    public void MoveNext() => Move(1);

    public void MovePrevious() => Move(-1);

    /// <summary>Enter: opens the result with the focus where it lives, and closes the panel.</summary>
    public bool OpenSelected()
    {
        if (SelectedIndex < 0 || Items[SelectedIndex].Target is not { } target)
        {
            return false;
        }

        Open(Items[SelectedIndex]);
        return true;
    }

    /// <summary>Opens a result, with the mouse or the keyboard.</summary>
    public void Open(SearchItem item)
    {
        if (item.Target is not { } target)
        {
            return;
        }

        _navigator.Open(target with { Text = target.Text ?? _text });
        Close();
    }

    void Clear()
    {
        State = SearchState.Idle;
        Items = [];
        Message = string.Empty;
        SelectedIndex = -1;
        RetiredMatches = 0;
    }

    void Move(int step)
    {
        var next = SelectedIndex;
        do
        {
            next += step;
        }
        while (next >= 0 && next < Items.Count && Items[next].IsHeading);

        if (next >= 0 && next < Items.Count)
        {
            SelectedIndex = next;
        }
    }

    void Show(GlobalSearchResult result, string text)
    {
        RetiredMatches = result.RetiredMatches;
        if (result.IsEmpty)
        {
            Items = [];
            SelectedIndex = -1;
            Message = result.RetiredMatches > 0
                ? _localizer.Get("Shell.Search.EmptyRetired", result.RetiredMatches)
                : _localizer.Get("Shell.Search.Empty", text.Trim());
            State = SearchState.Empty;
            return;
        }

        var items = new List<SearchItem>();
        if (result.StudentTotal > 0)
        {
            items.Add(new SearchItem(_localizer.Get("Shell.Search.Students"), null));
            items.AddRange(result.Students.Select(s => new SearchItem(StudentText(s), new SearchTarget(SearchTargetKind.Student, s.StudentId))));
            AddMore(items, result.StudentTotal, result.Students.Count, SearchTargetKind.StudentList);
        }

        if (result.LockerTotal > 0)
        {
            items.Add(new SearchItem(_localizer.Get("Shell.Search.Lockers"), null));
            items.AddRange(result.Lockers.Select(l => new SearchItem(LockerText(l), new SearchTarget(SearchTargetKind.Locker, l.LockerId))));
            AddMore(items, result.LockerTotal, result.Lockers.Count, SearchTargetKind.LockerList);
        }

        if (result.GroupTotal > 0)
        {
            items.Add(new SearchItem(_localizer.Get("Shell.Search.Groups"), null));
            items.AddRange(result.Groups.Select(g => new SearchItem(
                _localizer.Get("Shell.Search.Group", g.LevelName, g.GroupName ?? string.Empty, g.StudentCount).Replace("  ", " ", StringComparison.Ordinal),
                new SearchTarget(SearchTargetKind.Group, g.GroupId ?? Guid.Empty, g.LevelId))));
            AddMore(items, result.GroupTotal, result.Groups.Count, SearchTargetKind.GroupList);
        }

        Items = items;
        Message = string.Empty;
        SelectedIndex = items.FindIndex(i => !i.IsHeading);
        State = SearchState.Results;
    }

    void AddMore(List<SearchItem> items, int total, int shown, SearchTargetKind list)
    {
        if (total > shown)
        {
            items.Add(new SearchItem(_localizer.Get("Shell.Search.SeeAll", total), new SearchTarget(list, Text: _text)));
        }
    }

    string StudentText(StudentHit s)
    {
        var details = new List<string>();
        var group = string.Join(' ', new[] { s.LevelName, s.GroupName }.Where(x => !string.IsNullOrEmpty(x)));
        if (group.Length > 0)
        {
            details.Add(group);
        }

        if (s.LockerNumber is { } number)
        {
            details.Add(_localizer.Get("Shell.Search.LockerNumber", number));
        }

        if (s.IsRetired)
        {
            details.Add(_localizer.Get("Shell.Search.Retired"));
        }
        else
        {
            details.Add(s.HasDebt ? _localizer.Get("Shell.Search.Debt", s.PendingTotal) : _localizer.Get("Shell.Search.UpToDate"));
        }

        return _localizer.Get("Shell.Search.Student", $"{s.LastName}, {s.FirstName}", string.Join(" · ", details));
    }

    string LockerText(LockerHit l)
    {
        var status = LockerStatusPresentation.Text(l.Status, _localizer);
        var text = _localizer.Get("Shell.Search.Locker", l.Number, l.ZoneName, status);
        return l.StudentName is null ? text : _localizer.Get("Shell.Search.WithStudent", text, l.StudentName);
    }
}
