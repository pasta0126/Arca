// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Backup;

/// <summary>Asks for a backup file with the chooser of the system. The real one opens a window; tests script it.</summary>
public interface IBackupFilePicker
{
    /// <summary>Where to save a backup, proposing a name and the folder of the last one; null when the person gives up.</summary>
    Task<string?> PickSaveAsync(string suggestedName, string? folder);

    /// <summary>Which backup to open, starting at the folder of the last one; null when the person gives up.</summary>
    Task<string?> PickOpenAsync(string? folder);
}
