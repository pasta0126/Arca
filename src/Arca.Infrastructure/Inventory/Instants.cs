// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Arca.Infrastructure.Inventory;

/// <summary>
/// Instants are stored as UTC ticks, so they compare and sort as plain numbers in SQLite and always come back in UTC
/// (docs/convenciones.md: an instant is a DateTimeOffset in UTC).
/// </summary>
static class Instants
{
    public static ValueConverter<DateTimeOffset, long> RequiredConverter { get; } =
        new(value => value.UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));

    public static ValueConverter<DateTimeOffset?, long?> Converter { get; } =
        new(value => value == null ? null : value.Value.UtcTicks, ticks => ticks == null ? null : new DateTimeOffset(ticks.Value, TimeSpan.Zero));
}
