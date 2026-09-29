// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Lockers;

/// <summary>Which kind of out of service a screen asks for, without the domain type.</summary>
public enum OutOfServiceKindView
{
    Broken,
    Maintenance,
}

/// <summary>What to do with the student of a locker that goes out of service, without the domain type.</summary>
public enum OutOfServiceDecisionView
{
    Reassign,
    Keep,
    Release,
}

/// <summary>
/// What marking a locker out of service answered: it was done (with the sentence that says so), or nothing was changed because a
/// decision about the student is needed first, or because moving the student raised warnings to confirm.
/// </summary>
/// <param name="Sentence">What was done, when it was done; null when nothing changed.</param>
/// <param name="DecisionsOffered">The options to choose from when a decision is needed; empty otherwise.</param>
/// <param name="Warnings">The warnings about the destination of a reassignment, in words; nothing was changed until they are confirmed.</param>
public sealed record OutOfServiceView(string? Sentence, IReadOnlyList<OutOfServiceDecisionView> DecisionsOffered, IReadOnlyList<string> Warnings)
{
    public bool DecisionRequired => DecisionsOffered.Count > 0;

    public bool NeedsWarningConfirmation => Warnings.Count > 0;

    public bool IsDone => Sentence is not null;
}
