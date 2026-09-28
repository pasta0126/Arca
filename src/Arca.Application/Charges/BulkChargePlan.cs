// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Charges;

/// <summary>
/// The preview of a bulk operation on charges (pagaments, D6; docs/convenciones.md, section 5): which of the selected
/// charges can take part and which cannot, computed without saving anything. Immutable; confirming it revalidates
/// against the data as it is then.
/// </summary>
/// <param name="Eligible">The charges that would change, in the order selected. Empty if any is ineligible.</param>
/// <param name="Ineligible">The selected charges that no longer qualify, or that do not exist.</param>
/// <param name="TotalAmount">The total of the eligible charges.</param>
public sealed record BulkChargePlan(IReadOnlyList<Guid> Eligible, IReadOnlyList<Guid> Ineligible, decimal TotalAmount)
{
    public bool HasIneligible => Ineligible.Count > 0;
}

/// <summary>The outcome of confirming a bulk operation: either every charge changed, or nothing did and the plan is up to date.</summary>
/// <param name="Applied">True if the charges changed.</param>
/// <param name="Count">How many charges changed (zero if not applied).</param>
/// <param name="TotalAmount">Their total (zero if not applied).</param>
/// <param name="Plan">The plan as it is now: the one applied, or the fresh analysis when nothing changed.</param>
public sealed record BulkChargeResult(bool Applied, int Count, decimal TotalAmount, BulkChargePlan Plan);
