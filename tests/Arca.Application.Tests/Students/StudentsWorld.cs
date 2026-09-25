// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.SchoolYears.CreateAcademicYear;
using Arca.Application.Students;
using Arca.Application.Students.AddStudent;
using Arca.Application.Students.ChangeStudentEnrollment;
using Arca.Application.Students.EditStudent;
using Arca.Application.Students.GetStudent;
using Arca.Application.Students.GetStudentHistory;
using Arca.Application.Students.ReactivateStudent;
using Arca.Application.Students.RetireStudent;
using Arca.Application.Students.SearchStudents;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.Testing.Inventory;

namespace Arca.Application.Tests.Students;

/// <summary>Every use case of students wired over the in-memory inventory, so a test reads as what a person does.</summary>
public sealed class StudentsWorld
{
    public InMemoryInventory Store { get; } = new();

    public FakeClock Clock { get; } = new(new DateTimeOffset(2026, 9, 25, 9, 0, 0, TimeSpan.Zero));

    public ILocalizer Localizer { get; } = new ResxLocalizer();

    public AddStudentHandler Add => new(Store.Students, Store.Enrollments, Store.Catalog, Store.Years, Store.StudentEvents, Store.StudentLockers, Store, Clock);

    public EditStudentHandler Edit => new(Store.Students, Store.Enrollments, Store.Catalog, Store.Years, Store.StudentEvents, Store.StudentLockers, Store, Clock);

    public RetireStudentHandler Retire => new(Store.Students, Store.Enrollments, Store.Catalog, Store.Years, Store.StudentEvents, Store.StudentLockers, Store, Clock);

    public ReactivateStudentHandler Reactivate => new(Store.Students, Store.Enrollments, Store.Catalog, Store.Years, Store.StudentEvents, Store.StudentLockers, Store, Clock);

    public ChangeStudentEnrollmentHandler ChangeEnrollment =>
        new(Store.Students, Store.Enrollments, Store.Catalog, Store.Years, Store.StudentEvents, Store.StudentLockers, Store, Clock);

    public GetStudentHandler Get => new(Store.Students, Store.Enrollments, Store.Catalog, Store.Years, Store.StudentLockers);

    public SearchStudentsHandler Search => new(Store.Students, Store.Enrollments, Store.Catalog, Store.Years, Store.StudentLockers);

    public GetStudentHistoryHandler History => new(Store.Students, Store.StudentEvents, Store.Catalog, Store.Years, Localizer);

    public Task<Result<Arca.Application.SchoolYears.AcademicYearSummary>> CreateYearAsync(int startYear = 2026) =>
        new CreateAcademicYearHandler(Store.Years, Store)
            .HandleAsync(new CreateAcademicYearRequest(new DateOnly(startYear, 9, 1), new DateOnly(startYear + 1, 6, 30)), default);

    /// <summary>An active year and a student enrolled in it, with the values already confirmed.</summary>
    public async Task<StudentDetail> StudentAsync(
        string first, string last, string email, string level = "1r ESO", string? group = "A")
    {
        if (Store.YearList.Count == 0)
        {
            await CreateYearAsync();
        }

        Clock.Advance(TimeSpan.FromMinutes(1));
        var added = await Add.HandleAsync(new AddStudentRequest(first, last, email, level, group, ConfirmNewValues: true), default);
        return added.Value!.Student!;
    }
}
