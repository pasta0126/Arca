// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Catalog;
using Arca.Application.Enrollments;
using Arca.Application.SchoolYears;
using Arca.Domain.Catalog;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;

namespace Arca.Application.Students;

/// <summary>Builds the detail of a student for the results of the use cases, loading what it needs explicitly.</summary>
internal sealed class StudentViews(
    IAcademicYearRepository years, IEnrollmentRepository enrollments, ICatalogRepository catalog, IStudentLockers lockers)
{
    /// <summary>The detail with the enrolment of the active year (or of the year given) and the locker now held.</summary>
    public async Task<StudentDetail> DetailAsync(Student student, AcademicYear? year, CancellationToken ct)
    {
        year ??= await years.GetActiveAsync(ct);
        string? level = null, group = null;
        if (year is not null && await enrollments.GetAsync(student.Id, year.Id, ct) is { } enrollment)
        {
            level = (await catalog.ListLevelsAsync(ct)).FirstOrDefault(l => l.Id == enrollment.LevelId)?.Name;
            group = enrollment.GroupId is { } id ? (await catalog.ListGroupsAsync(ct)).FirstOrDefault(g => g.Id == id)?.Name : null;
        }

        var locker = (await lockers.CurrentAsync([student.Id], ct)).GetValueOrDefault(student.Id);
        return new StudentDetail(
            student.Id, student.FirstName, student.LastName, student.Email, student.IsRetired, student.RetirementReason,
            student.RetiredAtUtc, year?.Name, level, group, locker?.Number);
    }
}
