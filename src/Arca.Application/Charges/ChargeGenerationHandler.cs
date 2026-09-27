// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Common;
using Arca.Application.ConceptAmounts;
using Arca.Domain.Charges;
using Arca.Domain.ConceptAmounts;

namespace Arca.Application.Charges;

/// <summary>
/// Generates the charges that follow from opening an assignment (pagaments, D4): the annual fee if the student does not
/// already have one for the year, and the deposit if the student does not already have one that is still current. Both
/// checks and both generations run inside the same transaction as the assignment, so nothing is left half done. Whether
/// the year's amounts are defined is checked by <see cref="ChargeGenerationGuard"/> before the assignment is committed, so
/// this hook can assume they are.
/// </summary>
public sealed class ChargeGenerationHandler(IConceptAmountRepository amounts, IChargeRepository charges, IChargeEventRepository events, IClock clock)
    : IAssignmentOpenedHandler
{
    public async Task HandleAsync(AssignmentHookContext context, CancellationToken ct)
    {
        var assignment = context.Assignment;
        var existing = await charges.ListByStudentAsync(assignment.StudentId, ct);
        var yearAmounts = await amounts.ListByYearAsync(assignment.YearId, ct);

        if (!existing.Any(c => c.YearId == assignment.YearId && c.Concept == ChargeConcept.Fee))
        {
            await GenerateAsync(assignment.StudentId, ChargeConcept.Fee, assignment.YearId, yearAmounts, ct);
        }

        if (!existing.Any(c => c.Concept == ChargeConcept.Deposit && c.Status != ChargeStatus.Voided))
        {
            await GenerateAsync(assignment.StudentId, ChargeConcept.Deposit, assignment.YearId, yearAmounts, ct);
        }
    }

    async Task GenerateAsync(Guid studentId, ChargeConcept concept, Guid yearId, IReadOnlyList<ConceptAmount> yearAmounts, CancellationToken ct)
    {
        var amount = yearAmounts.First(a => a.Concept == concept).Amount;
        var created = Charge.Create(Guid.NewGuid(), studentId, concept, yearId, amount, clock.UtcNow);
        await charges.AddAsync(created.Charge, ct);
        await events.AddAsync(created.Event, ct);
    }
}
