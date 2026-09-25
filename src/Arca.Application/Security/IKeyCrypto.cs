// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;

namespace Arca.Application.Security;

/// <summary>
/// The cryptographic primitives that protect the database key (acces-i-xifrat, D1 and D2). Application defines the
/// port and never references the library; Infrastructure implements it with NSec (libsodium). Key material is returned
/// as byte arrays that the caller wipes when it no longer needs them.
/// </summary>
public interface IKeyCrypto
{
    /// <summary>Length in bytes of every key handled here (256 bits).</summary>
    const int KeyLength = 32;

    /// <summary>Random bytes from the operating system, for salts and keys.</summary>
    byte[] RandomBytes(int count);

    /// <summary>A new random database key (the data encryption key).</summary>
    DatabaseKey GenerateDataKey();

    /// <summary>Wrapping key derived from the centre password with Argon2id.</summary>
    byte[] DerivePasswordKey(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, Argon2Parameters parameters);

    /// <summary>Wrapping key derived from the recovery key with HKDF (it is already high-entropy random).</summary>
    byte[] DeriveRecoveryKey(ReadOnlySpan<byte> recoverySecret, ReadOnlySpan<byte> salt);

    /// <summary>Encrypts and authenticates a key with XChaCha20-Poly1305. The result is nonce followed by ciphertext and tag.</summary>
    /// <param name="wrappingKey">The key that protects.</param>
    /// <param name="keyToWrap">The database key.</param>
    /// <param name="associatedData">Authenticated but not encrypted: ties the wrapped key to its file header.</param>
    byte[] Wrap(ReadOnlySpan<byte> wrappingKey, ReadOnlySpan<byte> keyToWrap, ReadOnlySpan<byte> associatedData);

    /// <summary>Returns false, with no detail, when the key, the data or the associated data do not match.</summary>
    bool TryUnwrap(ReadOnlySpan<byte> wrappingKey, ReadOnlySpan<byte> wrapped, ReadOnlySpan<byte> associatedData, out byte[] key);
}
