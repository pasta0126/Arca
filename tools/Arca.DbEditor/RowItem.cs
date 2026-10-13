// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.ComponentModel;

namespace Arca.DbEditor;

/// <summary>A row of any table: its cells as text, reachable by index so the grid can build its columns from the schema at run time.</summary>
public sealed class RowItem(long? rowId, string[] cells) : INotifyPropertyChanged
{
    public long? RowId { get; } = rowId;

    public string[] Cells { get; } = cells;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[int index]
    {
        get => Cells[index];
        set
        {
            Cells[index] = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }
    }
}
