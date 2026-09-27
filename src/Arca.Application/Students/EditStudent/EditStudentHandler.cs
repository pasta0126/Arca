// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Catalog;
using Arca.Application.Common;
using Arca.Application.Enrollments;
using Arca.Application.SchoolYears;
using Arca.Domain.Common;
using Arca.Domain.Students;

namespace Arca.Application.Students.EditStudent;

/// <summary>Corrects the name, surnames or email of a student. The event keeps what they were before.</summary>
public sealed class EditStudentHandler(
    IStudentRepository students, IEnrollmentRepository enrollments, ICatalogRepository catalog, IAcademicYearRepository years,
    IStudentEventRepository events, IStudentLockers lockers, IUnitOfWork unit, IClock clock)
{
    public Task<Result<StudentDetail>> HandleAsync(EditStudentRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var student = await students.GetAsync(request.StudentId, token);
            if (student is null)
            {
                return Result<StudentDetail>.Failure(StudentErrors.NotFound);
            }

            var changed = student.UpdateDetails(request.FirstName, request.LastName, request.Email, await students.ListAsync(token), clock.UtcNow);
            if (!changed.IsSuccess)
            {
                return Result<StudentDetail>.Failure(changed.Error!);
            }

            await students.UpdateAsync(student, token);
            await events.AddAsync(changed.Value!, token);
            return Result<StudentDetail>.Success(await new StudentViews(years, enrollments, catalog, lockers).DetailAsync(student, null, token));
        }, ct);
}
