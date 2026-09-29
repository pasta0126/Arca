// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.SchoolYears;
using Arca.Domain.Common;
using Arca.Domain.SchoolYears;

namespace Arca.Application.ConceptAmounts.GetConceptAmounts;

/// <summary>
/// The amounts of a year: what is defined for it, or the previous year's amounts proposed for confirmation when none is
/// defined yet and a previous year exists (pagaments, D5).
/// </summary>
public sealed class GetConceptAmountsHandler(IAcademicYearRepository years, IConceptAmountRepository amounts, IClock clock)
{
    public async Task<Result<ConceptAmountsView>> HandleAsync(GetConceptAmountsRequest request, CancellationToken ct)
    {
        var year = await years.GetAsync(request.YearId, ct);
        if (year is null)
        {
            return Result<ConceptAmountsView>.Failure(SchoolYearErrors.NotFound);
        }

        var editable = year.EndDate >= clock.Today;
        var current = await amounts.ListByYearAsync(year.Id, ct);
        if (current.Count > 0)
        {
            return Result<ConceptAmountsView>.Success(ConceptAmountsView.Of(year.Id, current, editable, isProposed: false));
        }

        var previous = (await years.ListAsync(ct)).Where(y => y.StartDate < year.StartDate).OrderByDescending(y => y.StartDate).FirstOrDefault();
        var proposed = previous is null ? [] : await amounts.ListByYearAsync(previous.Id, ct);
        return Result<ConceptAmountsView>.Success(ConceptAmountsView.Of(year.Id, proposed, editable, isProposed: proposed.Count > 0, proposedFrom: proposed.Count > 0 ? previous!.Name : null));
    }
}
