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

namespace Arca.Application.Students.AddStudent;

/// <summary>
/// Signs up a student by hand in the active year, with their enrolment (alumnes-i-assignacions). If the level or group is
/// not in the catalogue, nothing is saved until the person confirms creating it. The student, the enrolment, the new
/// catalogue values and the events are saved in one transaction.
/// </summary>
public sealed class AddStudentHandler(
    IStudentRepository students, IEnrollmentRepository enrollments, ICatalogRepository catalog, IAcademicYearRepository years,
    IStudentEventRepository events, IStudentLockers lockers, IUnitOfWork unit, IClock clock)
{
    public Task<Result<StudentChangeResult>> HandleAsync(AddStudentRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var year = await ActiveYear.RequireAsync(years, YearOperation.AddEnrollment, token);
            if (!year.IsSuccess)
            {
                return Result<StudentChangeResult>.Failure(year.Error!);
            }

            var now = clock.UtcNow;
            var created = Student.Create(Guid.NewGuid(), request.FirstName, request.LastName, request.Email, await students.ListAsync(token), now);
            if (!created.IsSuccess)
            {
                return Result<StudentChangeResult>.Failure(created.Error!);
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

            var student = created.Value!.Student;
            var enrolled = Enrollment.Create(Guid.NewGuid(), student, year.Value, choice.Value.Level, choice.Value.Group, [], now);
            if (!enrolled.IsSuccess)
            {
                return Result<StudentChangeResult>.Failure(enrolled.Error!);
            }

            await CatalogResolver.CreateNewAsync(catalog, choice.Value, token);
            await students.AddAsync(student, token);
            await enrollments.AddAsync(enrolled.Value!.Enrollment, token);
            await events.AddAsync(created.Value.Event, token);
            await events.AddAsync(enrolled.Value.Event, token);
            var detail = await new StudentViews(years, enrollments, catalog, lockers).DetailAsync(student, year.Value, token);
            return StudentChangeResult.Done(detail);
        }, ct);
}
