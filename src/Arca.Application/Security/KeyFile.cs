// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Security;

/// <summary>
/// What the key file holds (acces-i-xifrat, D3): the format version, the derivation parameters and salts, and the
/// database key wrapped twice. Nothing else, and nothing personal.
/// </summary>
/// <param name="FormatVersion">Version of this format, so future versions can be recognised or refused.</param>
/// <param name="Password">The key wrapped with the password.</param>
/// <param name="Recovery">The same key wrapped with the recovery key.</param>
public sealed record KeyFile(int FormatVersion, PasswordSlot Password, RecoverySlot Recovery)
{
    public const int CurrentVersion = 1;
}

/// <param name="Kdf">Derivation algorithm name, "argon2id".</param>
/// <param name="Parameters">Argon2id cost, kept so it can be raised later.</param>
/// <param name="Salt">Random salt of the derivation.</param>
/// <param name="Wrapped">Nonce, encrypted database key and tag.</param>
public sealed record PasswordSlot(string Kdf, Argon2Parameters Parameters, byte[] Salt, byte[] Wrapped);

/// <param name="Kdf">Derivation algorithm name, "hkdf-sha256".</param>
/// <param name="Salt">Random salt of the derivation.</param>
/// <param name="Wrapped">Nonce, encrypted database key and tag.</param>
public sealed record RecoverySlot(string Kdf, byte[] Salt, byte[] Wrapped);
