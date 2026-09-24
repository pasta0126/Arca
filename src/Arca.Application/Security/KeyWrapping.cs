// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Security.Cryptography;
using System.Text;
using Arca.Application.Storage;
using Arca.Domain.Common;

namespace Arca.Application.Security;

/// <summary>
/// Builds and opens the key file's two wrappers (acces-i-xifrat, D1). The wrapped key is authenticated together with the
/// file header (version, algorithm, parameters and salt), so changing any of them makes the unwrap fail.
/// </summary>
public static class KeyWrapping
{
    public const string PasswordKdf = "argon2id";
    public const string RecoveryKdf = "hkdf-sha256";
    public const int SaltLength = 16;

    /// <summary>Wraps a database key with the password and with the recovery key, each with its own fresh salt.</summary>
    /// <param name="crypto">The primitives.</param>
    /// <param name="dataKey">The database key to protect.</param>
    /// <param name="password">The password as UTF-8 bytes of its NFC-normalised text.</param>
    /// <param name="recoveryKey">The recovery key in canonical form.</param>
    /// <param name="cost">Argon2id cost for the password wrapper.</param>
    public static KeyFile Create(
        IKeyCrypto crypto, DatabaseKey dataKey, ReadOnlySpan<byte> password, string recoveryKey, Argon2Parameters cost)
    {
        var dek = dataKey.ToArray();
        var passwordSalt = crypto.RandomBytes(SaltLength);
        var recoverySalt = crypto.RandomBytes(SaltLength);
        byte[]? passwordKey = null;
        byte[]? recoveryKeyBytes = null;
        var recoverySecret = RecoveryKey.SecretBytes(recoveryKey);
        try
        {
            passwordKey = crypto.DerivePasswordKey(password, passwordSalt, cost);
            recoveryKeyBytes = crypto.DeriveRecoveryKey(recoverySecret, recoverySalt);
            var passwordWrapped = crypto.Wrap(passwordKey, dek, PasswordAad(KeyFile.CurrentVersion, cost, passwordSalt));
            var recoveryWrapped = crypto.Wrap(recoveryKeyBytes, dek, RecoveryAad(KeyFile.CurrentVersion, recoverySalt));
            return new KeyFile(
                KeyFile.CurrentVersion,
                new PasswordSlot(PasswordKdf, cost, passwordSalt, passwordWrapped),
                new RecoverySlot(RecoveryKdf, recoverySalt, recoveryWrapped));
        }
        finally
        {
            Wipe(dek, passwordKey, recoveryKeyBytes, recoverySecret);
        }
    }

    /// <summary>Opens the wrapper of the password. Fails, with no detail, if the password is wrong or the header was altered.</summary>
    public static Result<DatabaseKey> UnwrapWithPassword(IKeyCrypto crypto, KeyFile file, ReadOnlySpan<byte> password)
    {
        var slot = file.Password;
        var key = crypto.DerivePasswordKey(password, slot.Salt, slot.Parameters);
        try
        {
            return Open(crypto, key, slot.Wrapped, PasswordAad(file.FormatVersion, slot.Parameters, slot.Salt));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    /// <summary>Opens the wrapper of the recovery key, given in canonical form.</summary>
    public static Result<DatabaseKey> UnwrapWithRecoveryKey(IKeyCrypto crypto, KeyFile file, string recoveryKey)
    {
        var slot = file.Recovery;
        var secret = RecoveryKey.SecretBytes(recoveryKey);
        var key = crypto.DeriveRecoveryKey(secret, slot.Salt);
        try
        {
            return Open(crypto, key, slot.Wrapped, RecoveryAad(file.FormatVersion, slot.Salt));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(secret);
        }
    }

    static Result<DatabaseKey> Open(IKeyCrypto crypto, byte[] wrappingKey, byte[] wrapped, byte[] associatedData)
    {
        if (!crypto.TryUnwrap(wrappingKey, wrapped, associatedData, out var dek))
        {
            return Result<DatabaseKey>.Failure(KeyErrors.WrongCredentials);
        }

        try
        {
            return Result<DatabaseKey>.Success(new DatabaseKey(dek));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dek);
        }
    }

    /// <summary>The header that the password wrapper is authenticated with.</summary>
    public static byte[] PasswordAad(int version, Argon2Parameters cost, byte[] salt) =>
        Encoding.UTF8.GetBytes(
            $"arca-keys|v{version}|password|{PasswordKdf}|m={cost.MemoryKiB}|t={cost.Passes}|p={cost.Parallelism}|salt={Convert.ToBase64String(salt)}");

    /// <summary>The header that the recovery wrapper is authenticated with.</summary>
    public static byte[] RecoveryAad(int version, byte[] salt) =>
        Encoding.UTF8.GetBytes($"arca-keys|v{version}|recovery|{RecoveryKdf}|salt={Convert.ToBase64String(salt)}");

    static void Wipe(params byte[]?[] buffers)
    {
        foreach (var buffer in buffers)
        {
            if (buffer is not null)
            {
                CryptographicOperations.ZeroMemory(buffer);
            }
        }
    }
}
