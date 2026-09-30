// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.UI.Common;

namespace Arca.UI.Lists;

/// <summary>A column of a list: what its header says, how a row shows in it and how rows sort by it.</summary>
/// <typeparam name="TRow">The kind of row.</typeparam>
/// <param name="Id">A stable name.</param>
/// <param name="Header">The heading, already in the user's language.</param>
/// <param name="Text">What the row shows in this column. It is also what the text filter searches.</param>
/// <param name="SortKey">The value rows are ordered by; the text itself if not given. Text is ordered as people read it, ignoring case and accents.</param>
/// <param name="Width">The share of the width of the list, in stars.</param>
public sealed record ListColumn<TRow>(string Id, string Header, Func<TRow, string> Text, Func<TRow, IComparable?>? SortKey = null, double Width = 1);

/// <summary>
/// The behaviour of a long list, without any window (ux-fonaments, D7): rows that can be ordered by any column and filtered,
/// and a selection that belongs to the rows themselves, by identity, so ordering, filtering or loading the data again never
/// loses a mark. Marks of rows a filter hides are kept and counted.
/// </summary>
/// <typeparam name="TRow">The kind of row.</typeparam>
/// <typeparam name="TKey">What identifies a row, such as a student's identity, never its position.</typeparam>
public sealed class ListViewModel<TRow, TKey>(
    IReadOnlyList<ListColumn<TRow>> columns, Func<TRow, TKey> key, ILocalizer localizer) : ObservableObject
    where TKey : notnull
{
    readonly HashSet<TKey> _selected = [];
    List<TRow> _all = [];
    IReadOnlyList<TRow> _rows = [];
    string _filterText = string.Empty;
    Func<TRow, bool>? _predicate;
    string? _sortColumn;
    bool _sortDescending;

    public IReadOnlyList<ListColumn<TRow>> Columns => columns;

    /// <summary>The rows to show now: filtered and ordered.</summary>
    public IReadOnlyList<TRow> Rows
    {
        get => _rows;
        private set => Set(ref _rows, value);
    }

    /// <summary>Every row the list has, whatever the filter hides.</summary>
    public IReadOnlyList<TRow> AllRows => _all;

    /// <summary>How many rows there are in all, whatever the filter hides.</summary>
    public int TotalCount => _all.Count;

    public string? SortColumnId => _sortColumn;

    public bool SortDescending => _sortDescending;

    /// <summary>Words the rows must contain, in any of their columns, to be shown. Empty shows everything.</summary>
    public string FilterText
    {
        get => _filterText;
        set
        {
            if (Set(ref _filterText, value))
            {
                Refresh();
            }
        }
    }

    /// <summary>Another condition on top of the text, such as "without a locker". Null removes it.</summary>
    public void SetPredicate(Func<TRow, bool>? predicate)
    {
        _predicate = predicate;
        Refresh();
    }

    /// <summary>Gives the list its data. The marks of rows that are still there stay; those of rows that left are dropped.</summary>
    public void SetItems(IEnumerable<TRow> items)
    {
        _all = [.. items];
        var present = _all.Select(key).ToHashSet();
        _selected.IntersectWith(present);
        Raise(nameof(TotalCount));
        Refresh();
        RaiseSelection();
    }

    /// <summary>Puts a row in place of the one with the same identity, or adds it, without touching the others: what a change to one row costs.</summary>
    public void ReplaceItem(TRow item)
    {
        var all = _all.ToList();
        var at = all.FindIndex(r => EqualityComparer<TKey>.Default.Equals(key(r), key(item)));
        if (at >= 0)
        {
            all[at] = item;
        }
        else
        {
            all.Add(item);
        }

        _all = all;
        Raise(nameof(TotalCount));
        Refresh();
    }

    /// <summary>What a click on a header does: order by that column, and ordering by it again reverses the order.</summary>
    public void SortBy(string columnId)
    {
        _sortDescending = _sortColumn == columnId && !_sortDescending;
        _sortColumn = columnId;
        Raise(nameof(SortColumnId));
        Raise(nameof(SortDescending));
        Refresh();
    }

    // --- Selection, by identity ---

    public bool IsSelected(TRow row) => _selected.Contains(key(row));

    public void SetSelected(TRow row, bool selected)
    {
        if (selected ? _selected.Add(key(row)) : _selected.Remove(key(row)))
        {
            RaiseSelection();
        }
    }

    public void Toggle(TRow row) => SetSelected(row, !IsSelected(row));

    /// <summary>The identities marked, including the rows the filter hides now.</summary>
    public IReadOnlyCollection<TKey> SelectedKeys => _selected;

    public int SelectedCount => _selected.Count;

    /// <summary>Marked rows the filter hides now; they are still selected and still counted.</summary>
    public int HiddenSelectedCount => _selected.Count - _rows.Count(IsSelected);

    /// <summary>"295 de 300": what is selected out of all the rows.</summary>
    public string SelectionText => localizer.Get("Common.Label.ProgressOf", SelectedCount, TotalCount);

    /// <summary>Tells the views that the marks changed, so each row can redraw its own.</summary>
    public event EventHandler? SelectionChanged;

    void RaiseSelection()
    {
        Raise(nameof(SelectedCount));
        Raise(nameof(HiddenSelectedCount));
        Raise(nameof(SelectionText));
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    void Refresh()
    {
        var words = TextComparer.Key(_filterText).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        IEnumerable<TRow> shown = _all;
        if (words.Length > 0)
        {
            shown = shown.Where(row =>
            {
                var haystack = TextComparer.Key(string.Join(' ', columns.Select(c => c.Text(row))));
                return words.All(w => haystack.Contains(w, StringComparison.Ordinal));
            });
        }

        if (_predicate is not null)
        {
            shown = shown.Where(_predicate);
        }

        if (columns.FirstOrDefault(c => c.Id == _sortColumn) is { } column)
        {
            Func<TRow, IComparable?> sortKey = column.SortKey ?? (row => column.Text(row));
            shown = _sortDescending
                ? shown.OrderByDescending(sortKey, _comparer)
                : shown.OrderBy(sortKey, _comparer);
        }

        Rows = [.. shown];
        Raise(nameof(HiddenSelectedCount));
    }

    static readonly SortKeyComparer _comparer = new();

    /// <summary>Text as people read it, numbers as numbers, and an empty value before any other.</summary>
    sealed class SortKeyComparer : IComparer<IComparable?>
    {
        public int Compare(IComparable? x, IComparable? y) => (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            (string a, string b) => TextComparer.Comparer.Compare(a, b),
            _ => x.CompareTo(y),
        };
    }
}
