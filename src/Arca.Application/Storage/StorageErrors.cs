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
}
