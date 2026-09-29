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

namespace Arca.Application.Charges.GetStudentChargesScreen;

/// <param name="StudentId">The student whose charges are opened.</param>
public sealed record GetStudentChargesScreenRequest(Guid StudentId);

/// <summary>
/// A charge as the list shows it, with the concept and the status by their stable names ("Fee", "Pending") so a screen never needs the
/// domain types, and for each operation the reason it is refused now, or null when it can be done.
/// </summary>
public sealed record ChargeLine(
    Guid Id, string Concept, string Status, Guid YearId, string YearName, bool IsActiveYear, decimal Amount, DateOnly? PaidOn, string? Reason,
    Error? PayBlocked, Error? ExemptBlocked, Error? WaiveBlocked, Error? VoidBlocked, Error? AdjustBlocked, Error? RevertBlocked);

/// <summary>
/// The charges of a student as their screen shows them (pantalles-cobraments): whether they are up to date or how much they owe, every
/// charge of any year with the reasons of its operations, and whether a key replacement can be charged and for how much.
/// </summary>
/// <param name="ByExemption">Up to date because every charge was exempted.</param>
/// <param name="ActiveYearId">The year a key replacement would be charged to, or null when there is no active year.</param>
/// <param name="KeyReplacementBlocked">Why a key replacement cannot be charged (no active year), or null.</param>
/// <param name="KeyReplacementAmount">The amount of the key replacement in the active year, or null when it is not defined yet.</param>
public sealed record StudentChargesScreen(
    Guid StudentId, string StudentName, bool UpToDate, bool ByExemption, decimal PendingTotal, IReadOnlyList<ChargeLine> Lines,
    Guid? ActiveYearId, string? ActiveYearName, Error? KeyReplacementBlocked, decimal? KeyReplacementAmount);

/// <summary>
/// Reads the charges of a student with the reasons of their operations. Each reason is what the domain rule itself answers when it is
/// tried on a copy of the charge, so the screen and the operation can never disagree and no rule is written twice. It changes nothing.
/// </summary>
public sealed class GetStudentChargesScreenHandler(
    IChargeRepository charges, IStudentRepository students, IAcademicYearRepository years, IConceptAmountRepository amounts, IClock clock)
{
    public async Task<Result<StudentChargesScreen>> HandleAsync(GetStudentChargesScreenRequest request, CancellationToken ct)
    {
        var student = await students.GetAsync(request.StudentId, ct);
        if (student is null)
        {
            return Result<StudentChargesScreen>.Failure(StudentErrors.NotFound);
        }

        var all = await charges.ListByStudentAsync(student.Id, ct);
        var active = await years.GetActiveAsync(ct);
        var names = (await years.ListAsync(ct)).ToDictionary(y => y.Id, y => y.Name);
        var standing = PaymentStanding.Of(all, active?.Id);
        var now = clock.UtcNow;
        var today = clock.Today;

        var lines = all
            .OrderByDescending(c => c.YearId == active?.Id)
            .ThenByDescending(c => names.GetValueOrDefault(c.YearId, string.Empty), StringComparer.Ordinal)
            .ThenBy(c => c.Concept)
            .Select(c =>
            {
                Charge Copy() => new(c.Id, c.StudentId, c.Concept, c.YearId, c.Amount, c.Status, c.PaidOn, c.Reason, c.Return, c.ReturnedOn, c.ReturnNote);
                return new ChargeLine(
                    c.Id, c.Concept.ToString(), c.Status.ToString(), c.YearId, names.GetValueOrDefault(c.YearId, string.Empty), c.YearId == active?.Id,
                    c.Amount.Amount, c.PaidOn, c.Reason,
                    Copy().MarkPaid(null, today, now).Error,
                    Copy().MarkExempt("-", now).Error,
                    Copy().Waive("-", now).Error,
                    Copy().Void("-", now).Error,
                    Copy().AdjustAmount(c.Amount.Amount, "-", now).Error,
                    Copy().Revert("-", now).Error);
            })
            .ToList();

        var replacement = active is null
            ? null
            : (await amounts.ListByYearAsync(active.Id, ct)).FirstOrDefault(a => a.Concept == ChargeConcept.KeyReplacementFee)?.Amount.Amount;
        return Result<StudentChargesScreen>.Success(new StudentChargesScreen(
            student.Id, student.FirstName + " " + student.LastName, standing.UpToDate, standing.ByExemption, standing.PendingTotal, lines,
            active?.Id, active?.Name, active is null ? SchoolYearErrors.NoActiveYear : null, replacement));
    }
}
