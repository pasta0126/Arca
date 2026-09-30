// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Assignments.ChangeStudentLocker;
using Arca.Application.Assignments.CheckAssignmentTarget;
using Arca.Application.Assignments.ReleaseStudentLocker;
using Arca.Application.Charges;
using Arca.Application.Common;
using Arca.Application.LockerMap;
using Arca.Application.Localization;
using Arca.Application.Lockers;
using Arca.Application.Lockers.MarkLockerOutOfService;
using Arca.Application.Lockers.RemoveLockerReservation;
using Arca.Application.Lockers.RestoreLockerService;
using Arca.Application.Lockers.ReserveLocker;
using Arca.Application.Students;
using Arca.Application.Students.SearchStudents;
using Arca.Domain.Common;
using Arca.Domain.Lockers;
using Arca.Infrastructure.Inventory;
using Arca.UI.Lockers;

namespace Arca.Desktop.Composition;

/// <summary>
/// Joins the map of the Lockers section to the use cases that assign (the composition root is the only place of the desktop project that knows them): what
/// the screen reads and what it does are handed over as delegates, and each operation answers with the sentence that says what
/// was done, made from the same result texts the rest of the application uses.
/// </summary>
static class LockerHomeComposition
{
    /// <summary>The assignment services with the hooks of payments, the same wherever an assignment is opened or closed.</summary>
    internal static AssignmentServices Services(EfInventory store, IClock clock) => new(
        store.Assignments, store.Students, store.Lockers, store.Zones, store.Enrollments, store.Years, store.StudentEvents, store.Events,
        [new ChargeGenerationGuard(store.ConceptAmounts, store.Charges)],
        [new ChargeGenerationHandler(store.ConceptAmounts, store.Charges, store.ChargeEvents, clock)], []);

    public static LockerAssignmentServices Create(EfInventory store, IClock clock, ILocalizer localizer)
    {
        var occupancy = new AssignmentOccupancy(store.Assignments, store.Lockers);
        var services = Services(store, clock);
        var assign = new AssignLockerHandler(services, store, clock);
        var change = new ChangeStudentLockerHandler(services, store, clock);
        var search = new SearchStudentsHandler(store.Students, store.Enrollments, store.Catalog, store.Years, occupancy);

        return new LockerAssignmentServices(
            ct => search.HandleAsync(new SearchStudentsRequest(new StudentFilter(LockerState: StudentLockerState.WithoutLocker)), ct),
            (student, locker, ct) => new CheckAssignmentTargetHandler(services, clock).HandleAsync(new CheckAssignmentTargetRequest(student, locker), ct),
            assign.HandleAsync,
            (request, ct) => change.HandleAsync(new ChangeStudentLockerRequest(request.StudentId, request.LockerId, request.ConfirmWarnings), ct),
            ChargesComposition.DebtLines(store, clock, localizer));
    }

    /// <summary>The summary of the start screen: the counts of lockers and students, from the same queries the sections use.</summary>
    public static Arca.Application.Home.GetHomeSummaryHandler HomeSummary(EfInventory store) => new(
        new Arca.Application.Lockers.ListLockerRows.ListLockerRowsHandler(store.Lockers, store.Zones, store.Assignments, store.Students, store.Charges),
        new SearchStudentsHandler(store.Students, store.Enrollments, store.Catalog, store.Years, new AssignmentOccupancy(store.Assignments, store.Lockers)),
        new Arca.Application.GlobalState.GetGlobalStateHandler(store.Years, store.Charges));
}
