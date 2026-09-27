// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Assignments;

namespace Arca.Application.Assignments;

/// <summary>An assignment as results and histories show it: who, which locker and zone, which year, from when and how it ended.</summary>
public sealed record AssignmentRow(
    Guid Id, Guid StudentId, string StudentName, Guid LockerId, int LockerNumber, string ZoneName, string YearName,
    DateTimeOffset StartedAtUtc, DateTimeOffset? EndedAtUtc, AssignmentCloseReason? CloseReason, string? CloseNote)
{
    public bool IsCurrent => EndedAtUtc is null;
}
