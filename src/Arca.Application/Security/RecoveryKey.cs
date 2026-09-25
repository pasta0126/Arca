// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Security.Cryptography;
using System.Text;
using Arca.Application.Storage;
using Arca.Domain.Common;

namespace Arca.Application.Security;

/// <summary>
/// The recovery key (acces-i-xifrat, D4): 26 characters of the Crockford base32 alphabet, which has no characters that
/// look alike, giving 130 bits. It is written in five groups (four of five characters and a last of six) so it can be read
/// and copied. The canonical form is the 26 characters in capitals; anything a person types is normalised to it.
/// </summary>
public static class RecoveryKey
{
    public const int Length = 26;

    /// <summary>Crockford base32: digits and letters without I, L, O and U.</summary>
    public const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    static readonly int[] _groupSizes = [5, 5, 5, 5, 6];

    /// <summary>A new random key in canonical form. Random, and derived from nothing.</summary>
    public static string Generate()
    {
        var chars = new char[Length];
        for (var i = 0; i < Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }

    /// <summary>
    /// Reads what the person typed: any case, with or without hyphens or spaces, and the usual look-alikes
    /// (0 for O, 1 for I or L). Returns the canonical 26 characters, or an error when it cannot be a recovery key.
    /// </summary>
    public static Result<string> Normalize(string? typed)
    {
        if (string.IsNullOrWhiteSpace(typed))
        {
            return Result<string>.Failure(KeyErrors.RecoveryKeyInvalid);
        }

        var builder = new StringBuilder(Length);
        foreach (var raw in typed)
        {
            if (raw is '-' or ' ' or '\t' or '‐' or '‑' or '‒' or '–' || char.IsWhiteSpace(raw))
            {
                continue;
            }

            var c = char.ToUpperInvariant(raw);
            c = c switch
            {
                'O' => '0',
                'I' or 'L' => '1',
                _ => c,
            };
            if (Alphabet.IndexOf(c, StringComparison.Ordinal) < 0)
            {
                return Result<string>.Failure(KeyErrors.RecoveryKeyInvalid);
            }

            builder.Append(c);
        }

        return builder.Length == Length
            ? Result<string>.Success(builder.ToString())
            : Result<string>.Failure(KeyErrors.RecoveryKeyInvalid);
    }

    /// <summary>A group as typed, read the same tolerant way as the whole key: case, spaces, hyphens and look-alikes.</summary>
    public static string NormalizeGroup(string? typed)
    {
        var builder = new StringBuilder();
        foreach (var raw in typed ?? string.Empty)
        {
            if (raw is '-' or ' ' or '\t' || char.IsWhiteSpace(raw))
            {
                continue;
            }

            builder.Append(char.ToUpperInvariant(raw) switch { 'O' => '0', 'I' or 'L' => '1', var c => c });
        }

        return builder.ToString();
    }

    /// <summary>The groups of a canonical key, in order: five groups, the last with six characters.</summary>
    public static IReadOnlyList<string> Groups(string canonical)
    {
        var groups = new List<string>(_groupSizes.Length);
        var position = 0;
        foreach (var size in _groupSizes)
        {
            groups.Add(canonical.Substring(position, size));
            position += size;
        }

        return groups;
    }

    /// <summary>The key as shown to the person: groups separated by hyphens.</summary>
    public static string Format(string canonical) => string.Join('-', Groups(canonical));

    /// <summary>The bytes fed to the derivation: the canonical characters, so any way of typing the key gives the same wrapping key.</summary>
    public static byte[] SecretBytes(string canonical) => Encoding.ASCII.GetBytes(canonical);
}
