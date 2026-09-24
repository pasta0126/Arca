// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;

namespace Arca.Testing;

/// <summary>A clock tests can set and advance. Today follows the time zone given at creation.</summary>
public sealed class FakeClock(DateTimeOffset start, TimeZoneInfo? zone = null) : IClock
{
    readonly TimeZoneInfo _zone = zone ?? TimeZoneInfo.Utc;

    public DateTimeOffset UtcNow { get; private set; } = start.ToUniversalTime();

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(UtcNow, _zone).DateTime);

    public void Advance(TimeSpan by) => UtcNow += by;

    public void Set(DateTimeOffset instant) => UtcNow = instant.ToUniversalTime();
}
