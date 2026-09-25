// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using Arca.Domain.Common;

namespace Arca.Domain.Lockers;

/// <summary>The number a person sees on a locker: a whole number from 1 to 99999 (taquilles-i-zones).</summary>
public static class LockerNumber
{
    public const int Lowest = 1;
    public const int Highest = 99999;

    public static Result<int> Validate(int number) =>
        number is >= Lowest and <= Highest
            ? Result<int>.Success(number)
            : Result<int>.Failure(LockerErrors.NumberInvalid(Lowest, Highest));

    /// <summary>Reads a number typed or found in a file. Decimals, signs, letters, empty text and out-of-range values are invalid.</summary>
    public static Result<int> Parse(string? text)
    {
        var trimmed = text?.Trim();
        return !string.IsNullOrEmpty(trimmed)
            && trimmed.All(char.IsAsciiDigit)
            && trimmed.Length <= 9
            ? Validate(int.Parse(trimmed, CultureInfo.InvariantCulture))
            : Result<int>.Failure(LockerErrors.NumberInvalid(Lowest, Highest));
    }
}
