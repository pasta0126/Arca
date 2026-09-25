// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using System.Text;

namespace Arca.Application.Security;

/// <summary>
/// The one way a typed password becomes the bytes that feed the derivation (acces-i-xifrat, D5 and D9): Unicode
/// NFC, so the same word typed with a composed or a decomposed accent opens the same data on every system, and
/// UTF-8. Nothing else is changed: spaces, accents, "ç" and "l·l" stay as they are.
/// </summary>
public static class PasswordText
{
    public static string Normalize(string password) => password.Normalize(NormalizationForm.FormC);

    /// <summary>The bytes of the password. The caller wipes them as soon as it has used them.</summary>
    public static byte[] ToBytes(string password) => Encoding.UTF8.GetBytes(Normalize(password));

    /// <summary>Number of characters as a person counts them: code points of the normalised text.</summary>
    public static int Length(string password) => Normalize(password).EnumerateRunes().Count();

    /// <summary>The text compared without case or accents, for the common-password and pattern checks.</summary>
    internal static string Fold(string password)
    {
        var decomposed = password.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }
}
