// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using Arca.Domain.Common;

namespace Arca.Domain.Catalog;

/// <summary>
/// Orders names the way a person reads them (alumnes-i-assignacions, D6): runs of digits are compared as numbers, so "2n"
/// comes before "10è", and the rest is compared with the Catalan rules of <see cref="TextComparer"/>.
/// </summary>
public sealed class NaturalComparer : IComparer<string?>
{
    public static NaturalComparer Instance { get; } = new();

    public int Compare(string? left, string? right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left is null || right is null)
        {
            return left is null ? -1 : 1;
        }

        int i = 0, j = 0;
        while (i < left.Length && j < right.Length)
        {
            if (char.IsAsciiDigit(left[i]) && char.IsAsciiDigit(right[j]))
            {
                var (numberLeft, endLeft) = ReadNumber(left, i);
                var (numberRight, endRight) = ReadNumber(right, j);
                var byValue = numberLeft.CompareTo(numberRight);
                if (byValue != 0)
                {
                    return byValue;
                }

                (i, j) = (endLeft, endRight);
                continue;
            }

            var (textLeft, nextLeft) = ReadText(left, i);
            var (textRight, nextRight) = ReadText(right, j);
            var byText = TextComparer.Compare(textLeft, textRight);
            if (byText != 0)
            {
                return byText;
            }

            (i, j) = (nextLeft, nextRight);
        }

        return (left.Length - i).CompareTo(right.Length - j);
    }

    static (decimal Value, int End) ReadNumber(string text, int start)
    {
        var end = start;
        while (end < text.Length && char.IsAsciiDigit(text[end]))
        {
            end++;
        }

        return (decimal.Parse(text.AsSpan(start, Math.Min(end - start, 28)), CultureInfo.InvariantCulture), end);
    }

    static (string Text, int End) ReadText(string text, int start)
    {
        var end = start;
        while (end < text.Length && !char.IsAsciiDigit(text[end]))
        {
            end++;
        }

        return (text[start..end], end);
    }
}
