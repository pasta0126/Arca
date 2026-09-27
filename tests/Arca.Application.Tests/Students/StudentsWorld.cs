// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
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
    public StudentsWorld(InMemoryInventory? store = null, FakeClock? clock = null, IStudentLockers? studentLockers = null)
    {
        Store = store ?? new InMemoryInventory();
        Clock = clock ?? new FakeClock(new DateTimeOffset(2026, 9, 25, 9, 0, 0, TimeSpan.Zero));
        StudentLockers = studentLockers ?? Store.StudentLockers;
    }

    public InMemoryInventory Store { get; }

    public FakeClock Clock { get; }

    /// <summary>The locker each student holds: the configurable stand-in, or the real one derived from the assignments.</summary>
    public IStudentLockers StudentLockers { get; }

    public ILocalizer Localizer { get; } = new ResxLocalizer();

    public AddStudentHandler Add => new(Store.Students, Store.Enrollments, Store.Catalog, Store.Years, Store.StudentEvents, StudentLockers, Store, Clock);

    public EditStudentHandler Edit => new(Store.Students, Store.Enrollments, Store.Catalog, Store.Years, Store.StudentEvents, StudentLockers, Store, Clock);

    /// <summary>The hooks of other capabilities, which tests fill in to see them called inside the transaction.</summary>
    public List<IAssignmentGuard> Guards { get; } = [];

    public List<IAssignmentOpenedHandler> OpenedHooks { get; } = [];

    public List<IAssignmentClosedHandler> ClosedHooks { get; } = [];

    public List<IStudentLifecycleHandler> LifecycleHooks { get; } = [];

    public AssignmentServices Services => new(
        Store.Assignments, Store.Students, Store.Lockers, Store.Zones, Store.Enrollments, Store.Years, Store.StudentEvents, Store.Events,
        Guards, OpenedHooks, ClosedHooks);

    public RetireStudentHandler Retire =>
        new(Store.Students, Store.Enrollments, Store.Catalog, Store.Years, Store.StudentEvents, StudentLockers, Services, LifecycleHooks, Store, Clock);

    public ReactivateStudentHandler Reactivate =>
        new(Store.Students, Store.Enrollments, Store.Catalog, Store.Years, Store.StudentEvents, StudentLockers, LifecycleHooks, Store, Clock);

    public ChangeStudentEnrollmentHandler ChangeEnrollment =>
        new(Store.Students, Store.Enrollments, Store.Catalog, Store.Years, Store.StudentEvents, StudentLockers, Store, Clock);

    public GetStudentHandler Get => new(Store.Students, Store.Enrollments, Store.Catalog, Store.Years, StudentLockers);

    public SearchStudentsHandler Search => new(Store.Students, Store.Enrollments, Store.Catalog, Store.Years, StudentLockers);

    public GetStudentHistoryHandler History => new(Store.Students, Store.StudentEvents, Store.Catalog, Store.Years, Store.Lockers, Localizer);

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
