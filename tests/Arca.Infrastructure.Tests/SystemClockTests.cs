// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Infrastructure.Common;
using Arca.Testing;
using Xunit;

namespace Arca.Infrastructure.Tests;

public sealed class SystemClockTests
{
    sealed class FixedProvider(DateTimeOffset instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instant;
    }

    static TimeZoneInfo Zone(string id) => TimeZoneInfo.FindSystemTimeZoneById(id);

    [Fact]
    [Trait("spec", "arquitectura-base/design: D7 Tiempo y dinero")]
    public void Same_instant_gives_the_local_calendar_date_of_each_zone()
    {
        // 2026-12-31 23:30 UTC is already 1 January in Madrid and still 31 December in Los Angeles.
        var instant = new DateTimeOffset(2026, 12, 31, 23, 30, 0, TimeSpan.Zero);

        var madrid = new SystemClock(new FixedProvider(instant), Zone("Europe/Madrid"));
        var losAngeles = new SystemClock(new FixedProvider(instant), Zone("America/Los_Angeles"));

        Assert.Equal(new DateOnly(2027, 1, 1), madrid.Today);
        Assert.Equal(new DateOnly(2026, 12, 31), losAngeles.Today);
        Assert.Equal(madrid.UtcNow, losAngeles.UtcNow);
    }

    [Fact]
    [Trait("spec", "arquitectura-base/design: D7 Tiempo y dinero")]
    public void Utc_instant_is_the_same_in_every_time_zone()
    {
        var instant = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

        var tokyo = new SystemClock(new FixedProvider(instant), Zone("Asia/Tokyo"));

        Assert.Equal(instant, tokyo.UtcNow);
        Assert.Equal(TimeSpan.Zero, tokyo.UtcNow.Offset);
    }

    [Fact]
    public void Fake_and_system_clocks_agree_on_the_calendar_date()
    {
        var instant = new DateTimeOffset(2026, 3, 29, 0, 30, 0, TimeSpan.Zero);
        var zone = Zone("Europe/Madrid");

        Assert.Equal(new SystemClock(new FixedProvider(instant), zone).Today, new FakeClock(instant, zone).Today);
    }

    [Fact]
    public void Default_clock_returns_a_recent_utc_instant()
    {
        var clock = new SystemClock();

        Assert.True(Math.Abs((DateTimeOffset.UtcNow - clock.UtcNow).TotalMinutes) < 1);
    }
}
