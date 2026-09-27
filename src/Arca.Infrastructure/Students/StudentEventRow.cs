// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Infrastructure.Students;

/// <summary>
/// A row of the append-only history of the students. It only exists to be stored: the rest of the application sees a
/// <c>HistoryEvent</c>. Nothing here is ever updated or deleted.
/// </summary>
sealed class StudentEventRow
{
    public long Id { get; set; }

    public Guid StudentId { get; set; }

    public string Type { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string? BeforeJson { get; set; }

    public string? AfterJson { get; set; }

    public string? Reason { get; set; }
}
