// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Catalog;
using Arca.Application.Common;
using Arca.Application.Enrollments;
using Arca.Application.SchoolYears;
using Arca.Domain.Common;
using Arca.Domain.Enrollments;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;

namespace Arca.Application.Students.ChangeStudentEnrollment;

/// <summary>Changes the level or group of a student in the active year and records it in their history.</summary>
public sealed class ChangeStudentEnrollmentHandler(
    IStudentRepository students, IEnrollmentRepository enrollments, ICatalogRepository catalog, IAcademicYearRepository years,
    IStudentEventRepository events, IStudentLockers lockers, IUnitOfWork unit, IClock clock)
{
    public Task<Result<StudentChangeResult>> HandleAsync(ChangeStudentEnrollmentRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var year = await ActiveYear.RequireAsync(years, YearOperation.ChangeEnrollment, token);
            if (!year.IsSuccess)
            {
                return Result<StudentChangeResult>.Failure(year.Error!);
            }

            var student = await students.GetAsync(request.StudentId, token);
            if (student is null)
            {
                return Result<StudentChangeResult>.Failure(StudentErrors.NotFound);
            }

            var enrollment = await enrollments.GetAsync(student.Id, year.Value!.Id, token);
            if (enrollment is null)
            {
                return Result<StudentChangeResult>.Failure(EnrollmentErrors.NotFound);
            }

            var choice = await CatalogResolver.ResolveAsync(catalog, request.LevelName, request.GroupName, token);
            if (!choice.IsSuccess)
            {
                return Result<StudentChangeResult>.Failure(choice.Error!);
            }

            if (choice.Value!.NewValues.Count > 0 && !request.ConfirmNewValues)
            {
                return StudentChangeResult.Confirm(choice.Value.NewValues);
            }

            var changed = enrollment.Change(year.Value, choice.Value.Level, choice.Value.Group, clock.UtcNow);
            if (!changed.IsSuccess)
            {
                return Result<StudentChangeResult>.Failure(changed.Error!);
            }

            await CatalogResolver.CreateNewAsync(catalog, choice.Value, token);
            await enrollments.UpdateAsync(enrollment, token);
            await events.AddAsync(changed.Value!, token);
            return StudentChangeResult.Done(await new StudentViews(years, enrollments, catalog, lockers).DetailAsync(student, year.Value, token));
        }, ct);
}
