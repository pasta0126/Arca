// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.SchoolYears;
using Arca.Domain.Common;
using Arca.Domain.ConceptAmounts;
using Arca.Domain.SchoolYears;

namespace Arca.Application.ConceptAmounts.SetConceptAmounts;

/// <summary>
/// Defines or changes the three amounts of a year in one operation (pagaments, D5). Only the concepts whose amount
/// actually changes are saved and get a history event; a charge already generated keeps the amount it was created with.
/// </summary>
public sealed class SetConceptAmountsHandler(
    IAcademicYearRepository years, IConceptAmountRepository amounts, IConceptAmountEventRepository events, IUnitOfWork unit, IClock clock)
{
    public Task<Result<ConceptAmountsView>> HandleAsync(SetConceptAmountsRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var year = await years.GetAsync(request.YearId, token);
            if (year is null)
            {
                return Result<ConceptAmountsView>.Failure(SchoolYearErrors.NotFound);
            }

            if (year.EndDate < clock.Today)
            {
                return Result<ConceptAmountsView>.Failure(ConceptAmountErrors.YearFinished);
            }

            var now = clock.UtcNow;
            var existing = await amounts.ListByYearAsync(year.Id, token);
            (ChargeConcept Concept, decimal Value)[] requested =
            [
                (ChargeConcept.Fee, request.Fee), (ChargeConcept.Deposit, request.Deposit), (ChargeConcept.KeyReplacementFee, request.KeyReplacementFee),
            ];

            foreach (var (concept, value) in requested)
            {
                var current = existing.FirstOrDefault(a => a.Concept == concept);
                if (current is null)
                {
                    var created = ConceptAmount.Create(Guid.NewGuid(), year.Id, concept, value, now);
                    if (!created.IsSuccess)
                    {
                        return Result<ConceptAmountsView>.Failure(created.Error!);
                    }

                    await amounts.AddAsync(created.Value!.Amount, token);
                    await events.AddAsync(created.Value.Event, token);
                }
                else if (current.Amount.Amount != value)
                {
                    var changed = current.ChangeAmount(value, now);
                    if (!changed.IsSuccess)
                    {
                        return Result<ConceptAmountsView>.Failure(changed.Error!);
                    }

                    await amounts.UpdateAsync(current, token);
                    await events.AddAsync(changed.Value!, token);
                }
            }

            var saved = await amounts.ListByYearAsync(year.Id, token);
            return Result<ConceptAmountsView>.Success(ConceptAmountsView.Of(year.Id, saved, isEditable: true, isProposed: false));
        }, ct);
}
