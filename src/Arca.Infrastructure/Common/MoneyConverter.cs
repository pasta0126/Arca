// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Arca.Infrastructure.Common;

/// <summary>Stores an amount of money as whole cents in an integer column, so no rounding can ever creep in.</summary>
static class MoneyConverter
{
    public static ValueConverter<Money, long> Instance { get; } = new(m => m.Cents, cents => Money.FromCents(cents));
}
