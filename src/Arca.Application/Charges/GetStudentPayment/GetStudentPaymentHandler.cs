// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.SchoolYears;
using Arca.Application.Students;
using Arca.Domain.Charges;
using Arca.Domain.Common;

namespace Arca.Application.Charges.GetStudentPayment;

/// <param name="StudentId">The student.</param>
public sealed record GetStudentPaymentRequest(Guid StudentId);

/// <summary>A student's standing and all their charges, of any year, for their record. The reasons show here and nowhere else.</summary>
/// <param name="Standing">Up to date or what they owe.</param>
/// <param name="Charges">Every charge, the most recent year first. Empty if none has been generated yet.</param>
public sealed record StudentPayment(PaymentStanding Standing, IReadOnlyList<ChargeRow> Charges);

/// <summary>The payment record of a student: whether they are up to date, and their charges with the reason of each.</summary>
public sealed class GetStudentPaymentHandler(IChargeRepository charges, IStudentRepository students, IAcademicYearRepository years)
{
    public async Task<Result<StudentPayment>> HandleAsync(GetStudentPaymentRequest request, CancellationToken ct)
    {
        if (await students.GetAsync(request.StudentId, ct) is null)
        {
            return Result<StudentPayment>.Failure(Domain.Students.StudentErrors.NotFound);
        }

        var all = await charges.ListByStudentAsync(request.StudentId, ct);
        var active = await years.GetActiveAsync(ct);
        var views = new ChargeViews(students, years);
        var rows = new List<ChargeRow>(all.Count);
        foreach (var charge in all.OrderByDescending(c => c.YearId == active?.Id).ThenBy(c => c.Concept))
        {
            rows.Add(await views.RowAsync(charge, ct));
        }

        return Result<StudentPayment>.Success(new StudentPayment(PaymentStanding.Of(all, active?.Id), rows));
    }
}
