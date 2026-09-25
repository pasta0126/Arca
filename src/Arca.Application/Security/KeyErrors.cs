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

    /// <summary>No password was given.</summary>
    public static readonly Error PasswordRequired = new("Keys.PasswordRequired");

    /// <summary>Shorter than the minimum. Args: {0} minimum length.</summary>
    public static Error PasswordTooShort(int minimum) => new("Keys.PasswordTooShort", Args: [minimum]);

    /// <summary>In the list of common passwords, or an obvious repetition or sequence.</summary>
    public static readonly Error PasswordTooCommon = new("Keys.PasswordTooCommon");

    /// <summary>The two passwords typed are different.</summary>
    public static readonly Error PasswordMismatch = new("Keys.PasswordMismatch");

    /// <summary>The new key file could not be written; the previous password still works.</summary>
    public static readonly Error ChangeFailed = new("Keys.ChangeFailed");

    /// <summary>The person cancelled the password request; the application closes without opening the data.</summary>
    public static readonly Error UnlockCancelled = new("Keys.UnlockCancelled");

    /// <summary>The groups typed to confirm the recovery key are missing.</summary>
    public static readonly Error ConfirmationRequired = new("Keys.ConfirmationRequired");

    /// <summary>The groups typed do not match the recovery key; nothing was changed.</summary>
    public static readonly Error ConfirmationIncorrect = new("Keys.ConfirmationIncorrect");
}
