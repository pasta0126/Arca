// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Security.Cryptography;

namespace Arca.Application.Storage;

/// <summary>
/// The 256-bit key that opens the database file. It is produced by the access flow of
/// acces-i-xifrat (centre password or recovery key) and is never stored in code or on disk in the clear.
/// The bytes are wiped on dispose.
/// </summary>
public sealed class DatabaseKey : IDisposable
{
    public const int Length = 32;

    readonly byte[] _bytes;

    public DatabaseKey(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Length)
        {
            throw new ArgumentException($"A database key has exactly {Length} bytes.", nameof(bytes));
        }

        _bytes = bytes.ToArray();
    }

    /// <summary>Copies the key so the caller can use it without keeping this object alive.</summary>
    public byte[] ToArray() => (byte[])_bytes.Clone();

    public void Dispose() => CryptographicOperations.ZeroMemory(_bytes);
}
