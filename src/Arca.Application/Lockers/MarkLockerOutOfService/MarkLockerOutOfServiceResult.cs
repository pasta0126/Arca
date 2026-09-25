// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Lockers;

namespace Arca.Application.Lockers.MarkLockerOutOfService;

/// <summary>Either the locker was put out of service, or a decision is required first and nothing was changed.</summary>
/// <param name="Locker">The locker as it is now.</param>
/// <param name="DecisionRequired">True when the locker is occupied and no decision was given.</param>
/// <param name="DecisionsOffered">The options to choose from when a decision is required.</param>
public sealed record MarkLockerOutOfServiceResult(
    LockerRow Locker, bool DecisionRequired, IReadOnlyList<OutOfServiceDecision> DecisionsOffered);
