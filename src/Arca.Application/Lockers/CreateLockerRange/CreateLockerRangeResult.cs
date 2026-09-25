// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Lockers.CreateLockerRange;

/// <summary>The outcome of confirming a range: either the lockers were created, or nothing was and the plan is up to date.</summary>
/// <param name="Applied">True if the lockers were created.</param>
/// <param name="Created">How many lockers were created (zero if not applied).</param>
/// <param name="Plan">The plan as it is now: the one that was applied, or the fresh analysis when nothing was created.</param>
public sealed record CreateLockerRangeResult(bool Applied, int Created, CreateLockerRangePlan Plan);
