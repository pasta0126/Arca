// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Storage;

/// <summary>Error codes of the local storage. Resource keys are derived: Storage.Error.&lt;Name&gt;.</summary>
public static class StorageErrors
{
    /// <summary>No database file at the configured location. Args: {0} path.</summary>
    public static Error FileNotFound(string path) => new("Storage.FileNotFound", Args: [path]);

    /// <summary>Creating would overwrite an existing file, which is never done. Args: {0} path.</summary>
    public static Error FileAlreadyExists(string path) => new("Storage.FileAlreadyExists", Args: [path]);

    /// <summary>The file is damaged, is not a database, or cannot be decrypted with the key. Args: {0} path.</summary>
    public static Error Unreadable(string path) => new("Storage.Unreadable", Args: [path]);

    /// <summary>A valid database that does not belong to ARCA. Args: {0} path.</summary>
    public static Error NotArcaDatabase(string path) => new("Storage.NotArcaDatabase", Args: [path]);

    /// <summary>The file was created by a newer version of ARCA; update the application. Never modified.</summary>
    public static readonly Error SchemaNewer = new("Storage.SchemaNewer");

    /// <summary>The safety copy before migrating could not be made or failed its integrity check; nothing was migrated.</summary>
    public static readonly Error BackupFailed = new("Storage.BackupFailed");

    /// <summary>A migration failed; the database was left exactly as it was before.</summary>
    public static readonly Error MigrationFailed = new("Storage.MigrationFailed");

    /// <summary>The chosen location does not exist, is not a file path, or cannot be written. Args: {0} path.</summary>
    public static Error PathNotAccessible(string path) => new("Storage.PathNotAccessible", Args: [path]);

    /// <summary>Another instance already has this database open.</summary>
    public static readonly Error AlreadyRunning = new("Storage.AlreadyRunning");

    /// <summary>There is no way yet to obtain the database key (the access flow of acces-i-xifrat is not in this build).</summary>
    public static readonly Error KeyNotAvailable = new("Storage.KeyNotAvailable");
}
