// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;
using Xunit;

namespace Arca.Domain.Tests.Common;

public sealed class MoneyTests
{
    [Fact]
    [Trait("spec", "arquitectura-base/design: D7 Tiempo y dinero")]
    public void Sum_of_partial_payments_is_exact()
    {
        // 3 x 33,33 + 0,01 must be exactly 100,00 (a double would drift).
        var third = Money.FromCents(3333);
        var total = third + third + third + Money.FromCents(1);

        Assert.Equal(Money.FromCents(10000), total);
        Assert.Equal(100.00m, total.Amount);
    }

    [Fact]
    [Trait("spec", "arquitectura-base/design: D7 Tiempo y dinero")]
    public void Ten_times_point_one_is_exactly_one()
    {
        var sum = Money.Zero;
        for (var i = 0; i < 10; i++)
        {
            sum += Money.FromCents(10);
        }

        Assert.Equal(1.00m, sum.Amount);
    }

    [Theory]
    [InlineData(50.00, 5000)]
    [InlineData(0.01, 1)]
    [InlineData(9999.99, 999999)]
    [InlineData(50.5, 5050)]
    [InlineData(-3.25, -325)]
    public void Decimal_with_up_to_two_decimals_converts_to_cents(double value, long cents)
    {
        Assert.True(Money.TryFromDecimal((decimal)value, out var money));
        Assert.Equal(cents, money.Cents);
    }

    [Theory]
    [InlineData("10.001")]
    [InlineData("0.005")]
    [InlineData("1.999")]
    public void Decimal_with_more_than_two_decimals_is_rejected(string value)
    {
        var parsed = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.False(Money.TryFromDecimal(parsed, out _));
    }

    [Fact]
    public void Cents_round_trip_through_amount()
    {
        Assert.True(Money.TryFromDecimal(Money.FromCents(123456).Amount, out var back));
        Assert.Equal(123456, back.Cents);
    }

    [Fact]
    public void Subtraction_negation_and_ordering_work()
    {
        var a = Money.FromCents(500);
        var b = Money.FromCents(200);

        Assert.Equal(Money.FromCents(300), a - b);
        Assert.Equal(Money.FromCents(-500), -a);
        Assert.True(a > b);
        Assert.True(b < a);
        Assert.Equal(0, a.CompareTo(Money.FromCents(500)));
    }

    [Fact]
    [Trait("spec", "arquitectura-base/internacionalitzacio: Formatos según la cultura catalana")]
    public void Formats_with_catalan_decimal_comma_and_thousands_point()
    {
        var text = Money.FromCents(123450).ToString();

        // Non-breaking space before the euro sign is culture data, so normalise it for the comparison.
        Assert.Equal("1.234,50 €", text.Replace(' ', ' '));
    }

    [Fact]
    [Trait("spec", "arquitectura-base/internacionalitzacio: Formatos según la cultura catalana")]
    public void Formatting_ignores_the_current_culture()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            Assert.Equal("1.234,50 €", Money.FromCents(123450).ToString().Replace(' ', ' '));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }
}
