// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;

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
