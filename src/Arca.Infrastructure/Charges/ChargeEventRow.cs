// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Infrastructure.Charges;

sealed class ChargeEventRow
{
    public long Id { get; set; }

    public Guid ChargeId { get; set; }

    public string Type { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string? BeforeJson { get; set; }

    public string? AfterJson { get; set; }

    public string? Reason { get; set; }
}
