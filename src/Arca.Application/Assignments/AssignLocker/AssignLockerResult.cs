// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Assignments.AssignLocker;

/// <summary>
/// The outcome of assigning or changing a locker: either the assignment was made, or it raised warnings that need the
/// person's explicit confirmation and nothing was changed.
/// </summary>
/// <param name="Assignment">The assignment as it is now, or null when a confirmation is needed.</param>
/// <param name="Warnings">The warnings to confirm; empty when it was done.</param>
public sealed record AssignLockerResult(AssignmentRow? Assignment, IReadOnlyList<Notice> Warnings)
{
    public bool NeedsConfirmation => Assignment is null;
}
