// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Catalog;
using Arca.Application.Common;
using Arca.Application.Enrollments;
using Arca.Application.SchoolYears;
using Arca.Domain.Common;
using Arca.Domain.Students;

namespace Arca.Application.Students.RetireStudent;

/// <summary>Retires a student with a reason, keeping the record and the history. Freeing the locker is added by the assignments.</summary>
public sealed class RetireStudentHandler(
    IStudentRepository students, IEnrollmentRepository enrollments, ICatalogRepository catalog, IAcademicYearRepository years,
    IStudentEventRepository events, IStudentLockers lockers, IUnitOfWork unit, IClock clock)
{
    public Task<Result<StudentDetail>> HandleAsync(RetireStudentRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var student = await students.GetAsync(request.StudentId, token);
            if (student is null)
            {
                return Result<StudentDetail>.Failure(StudentErrors.NotFound);
            }

            var retired = student.Retire(request.Reason, clock.UtcNow);
            if (!retired.IsSuccess)
            {
                return Result<StudentDetail>.Failure(retired.Error!);
            }

            await students.UpdateAsync(student, token);
            await events.AddAsync(retired.Value!, token);
            return Result<StudentDetail>.Success(await new StudentViews(years, enrollments, catalog, lockers).DetailAsync(student, null, token));
        }, ct);
}
