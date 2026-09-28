// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.SchoolYears;
using Arca.Application.Students;
using Arca.Domain.Charges;

namespace Arca.Application.Charges;

/// <summary>Builds the row of a charge for the results of the use cases, loading what it needs explicitly.</summary>
internal sealed class ChargeViews(IStudentRepository students, IAcademicYearRepository years)
{
    public async Task<ChargeRow> RowAsync(Charge charge, CancellationToken ct)
    {
        var student = await students.GetAsync(charge.StudentId, ct);
        var year = await years.GetAsync(charge.YearId, ct);
        return new ChargeRow(
            charge.Id, charge.StudentId, student is null ? "?" : student.FirstName + " " + student.LastName, charge.Concept,
            charge.YearId, year?.Name ?? string.Empty, charge.Amount.Amount, charge.Status, charge.PaidOn, charge.Reason,
            charge.Return, charge.ReturnedOn, charge.ReturnNote);
    }
}
