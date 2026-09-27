// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.ConceptAmounts;
using Arca.Application.SchoolYears;
using Arca.Application.Students;
using Arca.Domain.Charges;
using Arca.Domain.Common;
using Arca.Domain.ConceptAmounts;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;

namespace Arca.Application.Charges.ChargeKeyReplacement;

/// <summary>
/// Generates a key replacement charge only when the person decides to charge for it (pagaments): claus decides whether
/// and when to call this; a lost key with no charge simply generates nothing. Several may be charged to the same
/// student in the same year, each its own charge.
/// </summary>
public sealed class ChargeKeyReplacementHandler(
    IStudentRepository students, IAcademicYearRepository years, IConceptAmountRepository amounts, IChargeRepository charges,
    IChargeEventRepository events, IUnitOfWork unit, IClock clock)
{
    public Task<Result<ChargeRow>> HandleAsync(ChargeKeyReplacementRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var student = await students.GetAsync(request.StudentId, token);
            if (student is null)
            {
                return Result<ChargeRow>.Failure(StudentErrors.NotFound);
            }

            var year = await years.GetAsync(request.YearId, token);
            if (year is null)
            {
                return Result<ChargeRow>.Failure(SchoolYearErrors.NotFound);
            }

            var amount = (await amounts.ListByYearAsync(year.Id, token)).FirstOrDefault(a => a.Concept == ChargeConcept.KeyReplacementFee);
            if (amount is null)
            {
                return Result<ChargeRow>.Failure(ConceptAmountErrors.NotDefined);
            }

            var created = Charge.Create(Guid.NewGuid(), student.Id, ChargeConcept.KeyReplacementFee, year.Id, amount.Amount, clock.UtcNow);
            await charges.AddAsync(created.Charge, token);
            await events.AddAsync(created.Event, token);
            return Result<ChargeRow>.Success(await new ChargeViews(students, years).RowAsync(created.Charge, token));
        }, ct);
}
