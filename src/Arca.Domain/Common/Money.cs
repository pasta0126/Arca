// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;

namespace Arca.Domain.Common;

/// <summary>
/// An exact amount of euros held as whole cents, so sums never drift (arquitectura-base, D7).
/// The store keeps the cents as an integer. Range limits of a specific charge belong to that capability.
/// </summary>
public readonly record struct Money : IComparable<Money>
{
    Money(long cents) => Cents = cents;

    public static Money Zero => new(0);

    public long Cents { get; }

    public decimal Amount => Cents / 100m;

    public static Money FromCents(long cents) => new(cents);

    /// <summary>Converts an exact decimal; fails when it has more than two decimals or does not fit.</summary>
    public static bool TryFromDecimal(decimal amount, out Money money)
    {
        var cents = amount * 100m;
        if (cents != decimal.Truncate(cents) || cents < long.MinValue || cents > long.MaxValue)
        {
            money = default;
            return false;
        }

        money = new Money((long)cents);
        return true;
    }

    public static Money operator +(Money left, Money right) => new(checked(left.Cents + right.Cents));

    public static Money operator -(Money left, Money right) => new(checked(left.Cents - right.Cents));

    public static Money operator -(Money value) => new(checked(-value.Cents));

    public static bool operator <(Money left, Money right) => left.Cents < right.Cents;

    public static bool operator >(Money left, Money right) => left.Cents > right.Cents;

    public static bool operator <=(Money left, Money right) => left.Cents <= right.Cents;

    public static bool operator >=(Money left, Money right) => left.Cents >= right.Cents;

    public static Money Add(Money left, Money right) => left + right;

    public static Money Subtract(Money left, Money right) => left - right;

    public static Money Negate(Money value) => -value;

    public int CompareTo(Money other) => Cents.CompareTo(other.Cents);

    /// <summary>Formats with the Catalan culture, e.g. "1.234,50 €".</summary>
    public override string ToString() => Amount.ToString("C", Cultures.Catalan);

    public string ToString(IFormatProvider provider) => Amount.ToString("C", provider);
}
