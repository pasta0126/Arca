// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Catalog;
using Arca.Application.Enrollments;
using Arca.Application.SchoolYears;
using Arca.Domain.Common;
using Arca.Domain.Students;

namespace Arca.Application.Students.GetStudent;

/// <summary>The record of one student, asked for when it is opened: the only lookup that returns the email besides the import review.</summary>
public sealed class GetStudentHandler(
    IStudentRepository students, IEnrollmentRepository enrollments, ICatalogRepository catalog, IAcademicYearRepository years, IStudentLockers lockers)
{
    public async Task<Result<StudentDetail>> HandleAsync(GetStudentRequest request, CancellationToken ct)
    {
        var student = await students.GetAsync(request.StudentId, ct);
        return student is null
            ? Result<StudentDetail>.Failure(StudentErrors.NotFound)
            : Result<StudentDetail>.Success(await new StudentViews(years, enrollments, catalog, lockers).DetailAsync(student, null, ct));
    }
}
