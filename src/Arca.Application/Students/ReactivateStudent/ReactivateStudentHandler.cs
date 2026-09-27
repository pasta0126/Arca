// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Catalog;
using Arca.Application.Common;
using Arca.Application.Enrollments;
using Arca.Application.SchoolYears;
using Arca.Domain.Common;
using Arca.Domain.Enrollments;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;

namespace Arca.Application.Students.ReactivateStudent;

/// <summary>
/// Reactivates a retired student with the same record and gives them an enrolment in the active year with the level and
/// group indicated. If they already had one this year it is updated instead of duplicated.
/// </summary>
public sealed class ReactivateStudentHandler(
    IStudentRepository students, IEnrollmentRepository enrollments, ICatalogRepository catalog, IAcademicYearRepository years,
    IStudentEventRepository events, IStudentLockers lockers, IEnumerable<IStudentLifecycleHandler> lifecycle, IUnitOfWork unit, IClock clock)
{
    public Task<Result<StudentChangeResult>> HandleAsync(ReactivateStudentRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var year = await ActiveYear.RequireAsync(years, YearOperation.AddEnrollment, token);
            if (!year.IsSuccess)
            {
                return Result<StudentChangeResult>.Failure(year.Error!);
            }

            var student = await students.GetAsync(request.StudentId, token);
            if (student is null)
            {
                return Result<StudentChangeResult>.Failure(StudentErrors.NotFound);
            }

            if (!student.IsRetired)
            {
                return Result<StudentChangeResult>.Failure(StudentErrors.AlreadyActive);
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

            var now = clock.UtcNow;
            var reactivated = student.Reactivate(now);
            if (!reactivated.IsSuccess)
            {
                return Result<StudentChangeResult>.Failure(reactivated.Error!);
            }

            await CatalogResolver.CreateNewAsync(catalog, choice.Value, token);
            await students.UpdateAsync(student, token);
            await events.AddAsync(reactivated.Value!, token);

            var existing = await enrollments.GetAsync(student.Id, year.Value!.Id, token);
            if (existing is null)
            {
                var enrolled = Enrollment.Create(Guid.NewGuid(), student, year.Value, choice.Value.Level, choice.Value.Group, [], now);
                if (!enrolled.IsSuccess)
                {
                    return Result<StudentChangeResult>.Failure(enrolled.Error!);
                }

                await enrollments.AddAsync(enrolled.Value!.Enrollment, token);
                await events.AddAsync(enrolled.Value.Event, token);
            }
            else if (existing.LevelId != choice.Value.Level.Id || existing.GroupId != choice.Value.Group?.Id)
            {
                var changed = existing.Change(year.Value, choice.Value.Level, choice.Value.Group, now);
                if (!changed.IsSuccess)
                {
                    return Result<StudentChangeResult>.Failure(changed.Error!);
                }

                await enrollments.UpdateAsync(existing, token);
                await events.AddAsync(changed.Value!, token);
            }

            foreach (var hook in lifecycle)
            {
                await hook.OnReactivatedAsync(student, OperationContext.None, token);
            }

            return StudentChangeResult.Done(await new StudentViews(years, enrollments, catalog, lockers).DetailAsync(student, year.Value, token));
        }, ct);
}
