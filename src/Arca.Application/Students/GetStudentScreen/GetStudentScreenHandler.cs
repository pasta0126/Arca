// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Catalog;
using Arca.Application.Charges;
using Arca.Application.Common;
using Arca.Application.Enrollments;
using Arca.Application.SchoolYears;
using Arca.Domain.Assignments;
using Arca.Domain.Common;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;

namespace Arca.Application.Students.GetStudentScreen;

/// <param name="StudentId">The student to open.</param>
public sealed record GetStudentScreenRequest(Guid StudentId);

/// <summary>
/// A student as the record shows it (pantalles-de-domini, D2): their data, where their locker is, whether they owe anything and,
/// for each operation offered, the reason it is refused now, or null when it can be done. The screen enables or disables each
/// action from these and never works them out.
/// </summary>
/// <param name="LockerId">The locker they hold now, if any.</param>
/// <param name="RetireBlocked">Why they cannot be retired (they already are), or null.</param>
/// <param name="ReactivateBlocked">Why they cannot be reactivated (they are active, or there is no active year), or null.</param>
/// <param name="AssignBlocked">Why a locker cannot be assigned to them (no active year, retired, not enrolled, or they already have one), or null.</param>
/// <param name="ChangeBlocked">Why their locker cannot be changed (no active year, or they have none), or null.</param>
/// <param name="ReleaseBlocked">Why their locker cannot be released (no active year, or they have none), or null.</param>
public sealed record StudentScreenDetail(
    StudentDetail Student, Guid? LockerId, bool HasDebt, decimal PendingTotal, Error? RetireBlocked, Error? ReactivateBlocked,
    Error? AssignBlocked, Error? ChangeBlocked, Error? ReleaseBlocked);

/// <summary>
/// Reads a student with the reasons of their operations. Retiring is answered by the domain rule itself, tried on a copy of the
/// student; the others by the same conditions the assignment operations check first, so the screen and the operation never disagree.
/// It changes nothing.
/// </summary>
public sealed class GetStudentScreenHandler(
    IStudentRepository students, IEnrollmentRepository enrollments, ICatalogRepository catalog, IAcademicYearRepository years,
    IStudentLockers lockers, IChargeRepository charges, IClock clock)
{
    public async Task<Result<StudentScreenDetail>> HandleAsync(GetStudentScreenRequest request, CancellationToken ct)
    {
        var student = await students.GetAsync(request.StudentId, ct);
        if (student is null)
        {
            return Result<StudentScreenDetail>.Failure(StudentErrors.NotFound);
        }

        var year = await years.GetActiveAsync(ct);
        var detail = await new StudentViews(years, enrollments, catalog, lockers).DetailAsync(student, null, ct);
        var locker = (await lockers.CurrentAsync([student.Id], ct)).GetValueOrDefault(student.Id);
        var owed = (await charges.ListByStudentAsync(student.Id, ct)).Where(c => c.CountsAsDebt).Aggregate(Money.Zero, (sum, c) => sum + c.Amount).Amount;
        var noYear = year is null ? SchoolYearErrors.NoActiveYear : null;
        var copy = new Student(student.Id, student.FirstName, student.LastName, student.Email, student.NameKey, student.RetiredAtUtc, student.RetirementReason);
        var enrolled = year is not null && await enrollments.GetAsync(student.Id, year.Id, ct) is not null;

        return Result<StudentScreenDetail>.Success(new StudentScreenDetail(
            detail, locker?.LockerId, owed > 0, owed,
            copy.Retire("-", clock.UtcNow).Error,
            student.IsRetired ? noYear : StudentErrors.AlreadyActive,
            noYear ?? (student.IsRetired ? AssignmentErrors.StudentRetired : !enrolled ? AssignmentErrors.StudentNotEnrolled : locker is not null ? AssignmentErrors.StudentHasLocker : null),
            noYear ?? (locker is null ? AssignmentErrors.NoAssignment : null),
            noYear ?? (locker is null ? AssignmentErrors.NoAssignment : null)));
    }
}
