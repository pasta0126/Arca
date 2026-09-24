// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Security.Cryptography;
using Arca.Application.Security;
using Arca.Application.Storage;
using NSec.Cryptography;

namespace Arca.Infrastructure.Security;

/// <summary>The key primitives over libsodium through NSec: Argon2id, HKDF-SHA256 and XChaCha20-Poly1305 (acces-i-xifrat, D2).</summary>
public sealed class NSecKeyCrypto : IKeyCrypto
{
    static readonly AeadAlgorithm _aead = AeadAlgorithm.XChaCha20Poly1305;

    public byte[] RandomBytes(int count) => RandomNumberGenerator.GetBytes(count);

    public DatabaseKey GenerateDataKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(IKeyCrypto.KeyLength);
        try
        {
            return new DatabaseKey(bytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    public byte[] DerivePasswordKey(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, Arca.Application.Security.Argon2Parameters parameters)
    {
        var argon = new Argon2id(new NSec.Cryptography.Argon2Parameters
        {
            DegreeOfParallelism = parameters.Parallelism,
            MemorySize = parameters.MemoryKiB,
            NumberOfPasses = parameters.Passes,
        });
        return argon.DeriveBytes(password, salt, IKeyCrypto.KeyLength);
    }

    public byte[] DeriveRecoveryKey(ReadOnlySpan<byte> recoverySecret, ReadOnlySpan<byte> salt) =>
        KeyDerivationAlgorithm.HkdfSha256.DeriveBytes(
            recoverySecret, salt, "arca recovery key wrapping"u8, IKeyCrypto.KeyLength);

    public byte[] Wrap(ReadOnlySpan<byte> wrappingKey, ReadOnlySpan<byte> keyToWrap, ReadOnlySpan<byte> associatedData)
    {
        using var key = Key.Import(_aead, wrappingKey, KeyBlobFormat.RawSymmetricKey);
        var nonce = RandomNumberGenerator.GetBytes(_aead.NonceSize);
        var sealedKey = _aead.Encrypt(key, nonce, associatedData, keyToWrap);
        return [.. nonce, .. sealedKey];
    }

    public bool TryUnwrap(
        ReadOnlySpan<byte> wrappingKey, ReadOnlySpan<byte> wrapped, ReadOnlySpan<byte> associatedData, out byte[] key)
    {
        key = [];
        if (wrapped.Length <= _aead.NonceSize + _aead.TagSize)
        {
            return false;
        }

        using var wrapper = Key.Import(_aead, wrappingKey, KeyBlobFormat.RawSymmetricKey);
        var plain = _aead.Decrypt(wrapper, wrapped[.._aead.NonceSize], associatedData, wrapped[_aead.NonceSize..]);
        if (plain is null)
        {
            return false;
        }

        key = plain;
        return true;
    }
}
