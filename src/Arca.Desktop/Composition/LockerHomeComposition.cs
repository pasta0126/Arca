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
using Arca.UI.Map;

namespace Arca.Desktop.Composition;

/// <summary>
/// Joins the start screen to the use cases (the composition root is the only place of the desktop project that knows them): what
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

    public static LockerHomeServices Create(EfInventory store, IClock clock, ILocalizer localizer)
    {
        var occupancy = new AssignmentOccupancy(store.Assignments, store.Lockers);
        var services = Services(store, clock);
        var assign = new AssignLockerHandler(services, store, clock);
        var change = new ChangeStudentLockerHandler(services, store, clock);
        var release = new ReleaseStudentLockerHandler(services, store, clock);
        var reserve = new ReserveLockerHandler(store.Lockers, store.Zones, store.Events, occupancy, store, clock);
        var removeReservation = new RemoveLockerReservationHandler(store.Lockers, store.Zones, store.Events, occupancy, store.StudentEvents, store, clock);
        var outOfService = new MarkLockerOutOfServiceHandler(store.Lockers, store.Zones, store.Events, occupancy, services, store, clock);
        var restore = new RestoreLockerServiceHandler(store.Lockers, store.Zones, store.Events, occupancy, store, clock);
        var search = new SearchStudentsHandler(store.Students, store.Enrollments, store.Catalog, store.Years, occupancy);
        var assignments = new AssignmentResultTexts(localizer);
        var lockersText = new InventoryResultTexts(localizer);

        static async Task<Result<string>> Said<T>(Task<Result<T>> run, Func<T, string> say)
        {
            var result = await run;
            return result.IsSuccess ? Result<string>.Success(say(result.Value!), [.. result.Notices]) : Result<string>.Failure(result.Error!);
        }

        return new LockerHomeServices(
            new GetLockerMapHandler(store.Zones, store.Lockers, store.Assignments, store.Students, store.Charges).HandleAsync,
            new GetMapLockerHandler(store.Lockers, store.Assignments, store.Students, store.Charges).HandleAsync,
            new GetLockerDetailHandler(store.Lockers, store.Zones, store.Assignments, store.Students, store.Enrollments, store.Catalog, store.Years, store.Charges).HandleAsync,
            ct => search.HandleAsync(new SearchStudentsRequest(new StudentFilter(LockerState: StudentLockerState.WithoutLocker)), ct),
            (student, locker, ct) => new CheckAssignmentTargetHandler(services, clock).HandleAsync(new CheckAssignmentTargetRequest(student, locker), ct),
            assign.HandleAsync,
            (request, ct) => change.HandleAsync(new ChangeStudentLockerRequest(request.StudentId, request.LockerId, request.ConfirmWarnings), ct),
            new LockerOperations(
                (student, ct) => Said(release.HandleAsync(new ReleaseStudentLockerRequest(student), ct), assignments.Released),
                (locker, ct) => Said(reserve.HandleAsync(new ReserveLockerRequest(locker), ct), lockersText.LockerReserved),
                (locker, ct) => Said(removeReservation.HandleAsync(new RemoveLockerReservationRequest(locker), ct), lockersText.ReservationRemoved),
                (locker, ct) => Said(outOfService.HandleAsync(new MarkLockerOutOfServiceRequest(locker, OutOfServiceKind.Broken), ct), done => lockersText.OutOfService(done.Locker)),
                (locker, ct) => Said(restore.HandleAsync(new RestoreLockerServiceRequest(locker), ct), lockersText.ServiceRestored)));
    }
}
