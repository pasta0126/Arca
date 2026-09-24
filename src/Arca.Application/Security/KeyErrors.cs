// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Security;

/// <summary>Error codes of the keys and the key file. Resource keys: Keys.Error.&lt;Name&gt;.</summary>
public static class KeyErrors
{
    /// <summary>There is no key file next to the database. Args: {0} path.</summary>
    public static Error FileMissing(string path) => new("Keys.FileMissing", Args: [path]);

    /// <summary>The key file is damaged or has been altered. Args: {0} path.</summary>
    public static Error FileDamaged(string path) => new("Keys.FileDamaged", Args: [path]);

    /// <summary>The key file was written by a newer version of ARCA. Args: {0} path.</summary>
    public static Error UnknownVersion(string path) => new("Keys.UnknownVersion", Args: [path]);

    /// <summary>What was typed cannot be a recovery key.</summary>
    public static readonly Error RecoveryKeyInvalid = new("Keys.RecoveryKeyInvalid");

    /// <summary>The password or the recovery key does not open the key file. It says nothing more.</summary>
    public static readonly Error WrongCredentials = new("Keys.WrongCredentials");
}
