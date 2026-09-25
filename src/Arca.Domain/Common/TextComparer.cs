// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using System.Text;

namespace Arca.Domain.Common;

/// <summary>
/// The single place for ordering and searching user text (arquitectura-base, D9): Catalan rules,
/// case-insensitive; searches also ignore accents. Never use ToLower() or == on user text.
/// </summary>
public static class TextComparer
{
    const CompareOptions Ordering = CompareOptions.IgnoreCase;
    const CompareOptions Searching = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    static CompareInfo Info => Cultures.Catalan.CompareInfo;

    /// <summary>
    /// A stable key for "the same text": trimmed, without accents, case or repeated spaces. Unlike a culture sort key it is
    /// the same on every operating system and version, so it can be stored and given a unique index (taquilles-i-zones, D5).
    /// </summary>
    public static string Key(string? text)
    {
        var builder = new StringBuilder();
        var pendingSpace = false;
        foreach (var c in (text ?? string.Empty).Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static IComparer<string?> Comparer { get; } = Comparer<string?>.Create(Compare);

    /// <summary>Orders according to Catalan rules (ç after c, l·l with the l's), ignoring case.</summary>
    public static int Compare(string? left, string? right) => Info.Compare(left, right, Ordering);

    /// <summary>Equality for recognising the same text: ignores case and accents.</summary>
    public static bool SearchEquals(string? left, string? right) => Info.Compare(left, right, Searching) == 0;

    /// <summary>True when the text contains the query, ignoring case and accents. An empty query matches everything.</summary>
    public static bool Contains(string? text, string? query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return true;
        }

        return text is not null && Info.IndexOf(text, query, Searching) >= 0;
    }

    public static bool StartsWith(string? text, string? prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return true;
        }

        return text is not null && Info.IsPrefix(text, prefix, Searching);
    }
}
