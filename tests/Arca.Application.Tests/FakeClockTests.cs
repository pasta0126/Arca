// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Testing;
using Xunit;

namespace Arca.Application.Tests;

public sealed class FakeClockTests
{
    [Fact]
    public void Advance_moves_time_forward()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero));

        clock.Advance(TimeSpan.FromHours(3));

        Assert.Equal(new DateTimeOffset(2026, 9, 1, 11, 0, 0, TimeSpan.Zero), clock.UtcNow);
        Assert.Equal(new DateOnly(2026, 9, 1), clock.Today);
    }

    [Fact]
    public void Now_is_always_utc_even_if_given_with_an_offset()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.FromHours(2)));

        Assert.Equal(TimeSpan.Zero, clock.UtcNow.Offset);
        Assert.Equal(6, clock.UtcNow.Hour);
    }
}
