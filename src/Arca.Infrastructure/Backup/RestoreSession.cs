// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;

namespace Arca.Infrastructure.Backup;

/// <summary>
/// A backup opened for restoring: its files taken out into a work folder and, once the person has proved they know its
/// password or recovery key, the key that opens it. Nothing of the current data has been touched. Disposing removes
/// the work folder and wipes the key.
/// </summary>
public sealed class RestoreSession : IDisposable
{
    internal RestoreSession(string workFolder, BackupContainer.Extracted files)
    {
        WorkFolder = workFolder;
        DatabasePath = files.DatabasePath;
    }

    internal string WorkFolder { get; }

    /// <summary>The extracted database. The key file sits next to it under the usual name.</summary>
    internal string DatabasePath { get; }

    /// <summary>Set when the backup has been unlocked and verified.</summary>
    internal DatabaseKey? Key { get; set; }

    public bool IsUnlocked => Key is not null;

    public void Dispose()
    {
        Key?.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(WorkFolder, recursive: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // A leftover work folder is harmless.
        }
    }
}
