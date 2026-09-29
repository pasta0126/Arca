// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Charges;
using Arca.Application.Common;
using Arca.Application.ConceptAmounts;
using Arca.Domain.Common;
using Arca.Domain.ConceptAmounts;
using Arca.Domain.SchoolYears;

namespace Arca.Application.SchoolYears.GetYearScreen;

/// <param name="YearId">The year to open.</param>
public sealed record GetYearScreenRequest(Guid YearId);

/// <summary>
/// A year as its detail shows it (pantalles-de-domini, D2): what it is and, for each operation the screen offers, the reason it
/// is refused now, or null when it can be done. The screen enables or disables the action from these and never works them out.
/// </summary>
/// <param name="ActivationBlocked">Why the year cannot be activated now (another is active), or null.</param>
/// <param name="DeletionBlocked">Why the year cannot be deleted (it has enrolments or assignments), or null.</param>
/// <param name="HasCharges">Whether charges were generated in it: changing an amount then only affects the charges generated afterwards.</param>
/// <param name="HasAmounts">Whether its amounts are defined; without them no charge is generated.</param>
/// <param name="AmountsBlocked">Why its amounts cannot be changed (the year has ended), or null.</param>
/// <param name="IsHistoric">Whether it is a year that has ended and is not the active one: it is shown as a consultation.</param>
public sealed record YearScreenDetail(
    AcademicYearSummary Year, Error? ActivationBlocked, Error? DeletionBlocked, bool HasCharges, bool HasAmounts, Error? AmountsBlocked, bool IsHistoric);

/// <summary>Reads the detail of a year with the reasons of its operations, composed from the rules that apply them. It changes nothing.</summary>
public sealed class GetYearScreenHandler(
    IAcademicYearRepository years, IConceptAmountRepository amounts, IChargeRepository charges, IClock clock)
{
    public async Task<Result<YearScreenDetail>> HandleAsync(GetYearScreenRequest request, CancellationToken ct)
    {
        var year = await years.GetAsync(request.YearId, ct);
        if (year is null)
        {
            return Result<YearScreenDetail>.Failure(SchoolYearErrors.NotFound);
        }

        var all = await years.ListAsync(ct);
        return Result<YearScreenDetail>.Success(new YearScreenDetail(
            AcademicYearSummary.Of(year),
            year.IsActive ? null : year.CheckCanActivate(all).Error,
            year.CheckCanDelete(await years.HasDataAsync(year.Id, ct)).Error,
            await charges.AnyInYearAsync(year.Id, ct),
            (await amounts.ListByYearAsync(year.Id, ct)).Count > 0,
            year.EndDate < clock.Today ? ConceptAmountErrors.YearFinished : null,
            !year.IsActive && year.EndDate < clock.Today));
    }
}
