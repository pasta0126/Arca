// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;
using Arca.Domain.Lockers;

namespace Arca.Application.Lockers.MarkLockerOutOfService;

/// <summary>
/// Either the locker was put out of service, or nothing was changed because a decision is required first or because
/// reassigning the student raised warnings that the person has to confirm.
/// </summary>
/// <param name="Locker">The locker as it is now.</param>
/// <param name="DecisionRequired">True when the locker is occupied and no decision was given.</param>
/// <param name="DecisionsOffered">The options to choose from when a decision is required.</param>
/// <param name="Warnings">Warnings about the destination of a reassignment that need confirming; nothing was changed.</param>
public sealed record MarkLockerOutOfServiceResult(
    LockerRow Locker, bool DecisionRequired, IReadOnlyList<OutOfServiceDecision> DecisionsOffered, IReadOnlyList<Notice>? Warnings = null)
{
    public bool NeedsWarningConfirmation => Warnings is { Count: > 0 };
}
