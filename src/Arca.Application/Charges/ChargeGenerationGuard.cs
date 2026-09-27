// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.ConceptAmounts;
using Arca.Domain.Charges;
using Arca.Domain.Common;
using Arca.Domain.ConceptAmounts;

namespace Arca.Application.Charges;

/// <summary>
/// The check pagaments adds to every assignment (pagaments, D4): a blocker when the year's fee or deposit amount is not
/// defined, so nothing is assigned until it is, and a warning about pending charges of years other than the one being
/// assigned into, with their count and total, so the person can confirm before continuing.
/// </summary>
public sealed class ChargeGenerationGuard(IConceptAmountRepository amounts, IChargeRepository charges) : IAssignmentGuard
{
    public async Task<IReadOnlyList<AssignmentFinding>> CheckAsync(ProposedAssignment proposal, CancellationToken ct)
    {
        var yearAmounts = await amounts.ListByYearAsync(proposal.Year.Id, ct);
        if (!yearAmounts.Any(a => a.Concept == ChargeConcept.Fee) || !yearAmounts.Any(a => a.Concept == ChargeConcept.Deposit))
        {
            return [new AssignmentFinding(AssignmentFindingKind.Blocker, ConceptAmountErrors.NotDefined.Code)];
        }

        var priorDebt = (await charges.ListByStudentAsync(proposal.Student.Id, ct))
            .Where(c => c.YearId != proposal.Year.Id && c.CountsAsDebt)
            .ToList();
        if (priorDebt.Count == 0)
        {
            return [];
        }

        var total = priorDebt.Aggregate(Money.Zero, (sum, c) => sum + c.Amount);
        return [new AssignmentFinding(AssignmentFindingKind.Warning, "Charges.PriorDebt", [priorDebt.Count, total.Amount])];
    }
}
