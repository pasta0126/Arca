// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Lockers;

/// <summary>
/// The result of putting a locker out of service: either it was done, and the history event says what changed, or a
/// decision is needed first and nothing was changed.
/// </summary>
/// <param name="Event">The event of the change, or null if a decision is required.</param>
/// <param name="DecisionsOffered">The options to choose from when a decision is required; empty when it was done.</param>
public sealed record OutOfServiceOutcome(HistoryEvent? Event, IReadOnlyList<OutOfServiceDecision> DecisionsOffered)
{
    public bool NeedsDecision => Event is null;

    public static OutOfServiceOutcome Done(HistoryEvent change) => new(change, []);

    public static OutOfServiceOutcome DecisionRequired() =>
        new(null, [OutOfServiceDecision.Reassign, OutOfServiceDecision.Keep, OutOfServiceDecision.Release]);
}
