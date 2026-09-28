// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;
using Arca.Domain.ConceptAmounts;

namespace Arca.Domain.Charges;

/// <summary>One line of a debt: what is owed for a concept in a year, and whether that year is the active one.</summary>
public sealed record DebtLine(ChargeConcept Concept, Guid YearId, bool IsActiveYear, decimal Amount);

/// <summary>
/// Whether a student is up to date with their payments and, if not, what they owe (pagaments, D7). It is derived from the
/// charges every time and never stored, so it cannot get out of step with them.
/// </summary>
/// <param name="UpToDate">True when no charge, of any year, is pending. A student with no charges is up to date.</param>
/// <param name="ByExemption">True when they are up to date only because every charge is exempt.</param>
/// <param name="PendingTotal">The total owed.</param>
/// <param name="Breakdown">What is owed by concept and year, the active year first and then the earlier ones.</param>
public sealed record PaymentStanding(bool UpToDate, bool ByExemption, decimal PendingTotal, IReadOnlyList<DebtLine> Breakdown)
{
    /// <summary>The one function that knows the standing of a student, from all their charges.</summary>
    public static PaymentStanding Of(IEnumerable<Charge> charges, Guid? activeYearId)
    {
        var list = charges.Where(c => c.Status != ChargeStatus.Voided).ToList();
        var pending = list.Where(c => c.CountsAsDebt).ToList();
        if (pending.Count == 0)
        {
            return new PaymentStanding(true, list.Count > 0 && list.All(c => c.Status == ChargeStatus.Exempt), 0m, []);
        }

        IReadOnlyList<DebtLine> breakdown =
        [
            .. pending
                .GroupBy(c => (c.Concept, c.YearId))
                .Select(g => new DebtLine(g.Key.Concept, g.Key.YearId, g.Key.YearId == activeYearId, g.Aggregate(Money.Zero, (sum, c) => sum + c.Amount).Amount))
                .OrderByDescending(l => l.IsActiveYear)
                .ThenBy(l => l.YearId)
                .ThenBy(l => l.Concept),
        ];
        return new PaymentStanding(false, false, breakdown.Sum(l => l.Amount), breakdown);
    }
}
