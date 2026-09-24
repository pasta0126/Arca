// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;

namespace Arca.Infrastructure.Common;

public sealed class SystemClock(TimeProvider? provider = null, TimeZoneInfo? zone = null) : IClock
{
    readonly TimeProvider _provider = provider ?? TimeProvider.System;
    readonly TimeZoneInfo _zone = zone ?? TimeZoneInfo.Local;

    public DateTimeOffset UtcNow => _provider.GetUtcNow();

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(UtcNow, _zone).DateTime);
}
