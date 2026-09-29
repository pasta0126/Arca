// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Assignments.ChangeStudentLocker;
using Arca.Application.Assignments.CheckAssignmentTarget;
using Arca.Application.Assignments.GetStudentAssignments;
using Arca.Application.Assignments.ReleaseStudentLocker;
using Arca.Application.Assignments.SuggestLocker;
using Arca.Application.Catalog.ListCatalog;
using Arca.Application.Charges;
using Arca.Application.Common;
using Arca.Application.Localization;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Students;
using Arca.Application.Students.AddStudent;
using Arca.Application.Students.ChangeStudentEnrollment;
using Arca.Application.Students.EditStudent;
using Arca.Application.Students.GetStudentHistory;
using Arca.Application.Students.GetStudentScreen;
using Arca.Application.Students.ListStudentRows;
using Arca.Application.Students.ReactivateStudent;
using Arca.Application.Students.RetireStudent;
using Arca.Application.Students.SearchStudents;
using Arca.Domain.Common;
using Arca.Infrastructure.Inventory;
using Arca.UI.Assigning;
using Arca.UI.Students;

namespace Arca.Desktop.Composition;

/// <summary>
/// Joins the Students section and the selectors of assignment to their use cases (the composition root is the only place of the
/// desktop project that knows them). The retirement and the reactivation carry the deposit hook of payments, as everywhere else.
/// </summary>
static class StudentsComposition
{
    public static (StudentServices Students, AssignmentPickerServices Pickers) Create(EfInventory store, IClock clock, ILocalizer localizer)
    {
        var occupancy = new AssignmentOccupancy(store.Assignments, store.Lockers);
        var services = LockerHomeComposition.Services(store, clock);
        var lifecycle = new IStudentLifecycleHandler[] { new DepositLifecycleHandler(store.Charges, store.ChargeEvents, clock) };
        var search = new SearchStudentsHandler(store.Students, store.Enrollments, store.Catalog, store.Years, occupancy);
        var listStudents = new ListStudentRowsHandler(search, store.Charges);
        var add = new AddStudentHandler(store.Students, store.Enrollments, store.Catalog, store.Years, store.StudentEvents, occupancy, store, clock);
        var edit = new EditStudentHandler(store.Students, store.Enrollments, store.Catalog, store.Years, store.StudentEvents, occupancy, store, clock);
        var change = new ChangeStudentEnrollmentHandler(store.Students, store.Enrollments, store.Catalog, store.Years, store.StudentEvents, occupancy, store, clock);
        var retire = new RetireStudentHandler(store.Students, store.Enrollments, store.Catalog, store.Years, store.StudentEvents, occupancy, services, lifecycle, store, clock);
        var reactivate = new ReactivateStudentHandler(store.Students, store.Enrollments, store.Catalog, store.Years, store.StudentEvents, occupancy, lifecycle, store, clock);
        var release = new ReleaseStudentLockerHandler(services, store, clock);
        var assign = new AssignLockerHandler(services, store, clock);
        var changeLocker = new ChangeStudentLockerHandler(services, store, clock);
        var texts = new AssignmentResultTexts(localizer);

        var students = new StudentServices(
            listStudents.HandleAsync,
            new ListCatalogHandler(store.Catalog).HandleAsync,
            (id, ct) => new GetStudentScreenHandler(store.Students, store.Enrollments, store.Catalog, store.Years, occupancy, store.Charges, clock)
                .HandleAsync(new GetStudentScreenRequest(id), ct),
            async (id, ct) =>
            {
                var history = await new GetStudentHistoryHandler(store.Students, store.StudentEvents, store.Catalog, store.Years, store.Lockers, localizer)
                    .HandleAsync(new GetStudentHistoryRequest(id), ct);
                return history.IsSuccess
                    ? Result<IReadOnlyList<string>>.Success([.. history.Value!.Select(e => $"{localizer.Format(e.At)}: {e.Text}")])
                    : Result<IReadOnlyList<string>>.Failure(history.Error!);
            },
            async (id, ct) =>
            {
                var rows = await new GetStudentAssignmentsHandler(services).HandleAsync(new GetStudentAssignmentsRequest(id), ct);
                return rows.IsSuccess
                    ? Result<IReadOnlyList<string>>.Success([.. rows.Value!.Select(r => r.IsCurrent
                        ? localizer.Get("Students.Label.AssignmentCurrent", r.LockerNumber, r.ZoneName, r.YearName, localizer.Format(r.StartedAtUtc))
                        : localizer.Get("Students.Label.AssignmentPast", r.LockerNumber, r.ZoneName, r.YearName, localizer.Format(r.StartedAtUtc), localizer.Format(r.EndedAtUtc!.Value)))])
                    : Result<IReadOnlyList<string>>.Failure(rows.Error!);
            },
            add.HandleAsync,
            edit.HandleAsync,
            change.HandleAsync,
            (id, reason, ct) => retire.HandleAsync(new RetireStudentRequest(id, reason), ct),
            reactivate.HandleAsync,
            async (id, ct) =>
            {
                var released = await release.HandleAsync(new ReleaseStudentLockerRequest(id), ct);
                return released.IsSuccess ? Result<string>.Success(texts.Released(released.Value!)) : Result<string>.Failure(released.Error!);
            });

        var pickers = new AssignmentPickerServices(
            new ListLockerRowsHandler(store.Lockers, store.Zones, store.Assignments, store.Students, store.Charges).HandleAsync,
            (zone, ct) => new SuggestLockerHandler(store.Lockers, store.Zones, occupancy).HandleAsync(new SuggestLockerRequest(zone), ct),
            (student, locker, ct) => new CheckAssignmentTargetHandler(services, clock).HandleAsync(new CheckAssignmentTargetRequest(student, locker), ct),
            assign.HandleAsync,
            (request, ct) => changeLocker.HandleAsync(new ChangeStudentLockerRequest(request.StudentId, request.LockerId, request.ConfirmWarnings), ct),
            listStudents.HandleAsync,
            ChargesComposition.DebtLines(store, clock, localizer));
        return (students, pickers);
    }
}
