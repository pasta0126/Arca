// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Common;
using Arca.Application.Localization;
using Arca.Application.Lockers;
using Arca.Application.Lockers.AddLocker;
using Arca.Application.Lockers.ChangeLockerNumber;
using Arca.Application.Lockers.ChangeLockerZone;
using Arca.Application.Lockers.CreateLockerRange;
using Arca.Application.Lockers.GetLockerHistory;
using Arca.Application.Lockers.GetLockerScreen;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Lockers.MarkLockerOutOfService;
using Arca.Application.Lockers.RemoveLockerReservation;
using Arca.Application.Lockers.RestoreLockerService;
using Arca.Application.Lockers.ReserveLocker;
using Arca.Application.Lockers.RetireLocker;
using Arca.Application.Zones;
using Arca.Application.Zones.CreateZone;
using Arca.Application.Zones.DeactivateZone;
using Arca.Application.Zones.DeleteZone;
using Arca.Application.Zones.ListZoneRows;
using Arca.Application.Zones.ReactivateZone;
using Arca.Application.Zones.RenameZone;
using Arca.Domain.Common;
using Arca.Domain.Lockers;
using Arca.Infrastructure.Inventory;
using Arca.UI.Lockers;

namespace Arca.Desktop.Composition;

/// <summary>
/// Joins the Lockers section to its use cases (the composition root is the only place of the desktop project that knows them): each
/// operation answers with the sentence that says what was done, made from the same result texts the rest of the application uses.
/// </summary>
static class LockersComposition
{
    public static LockerServices Create(EfInventory store, IClock clock, ILocalizer localizer)
    {
        var occupancy = new AssignmentOccupancy(store.Assignments, store.Lockers);
        var rows = new ListLockerRowsHandler(store.Lockers, store.Zones, store.Assignments, store.Students, store.Charges);
        var text = new InventoryResultTexts(localizer);
        var add = new AddLockerHandler(store.Lockers, store.Zones, store.Events, store, clock);
        var range = new CreateLockerRangeHandler(store.Lockers, store.Zones, store.Events, store, clock);
        var number = new ChangeLockerNumberHandler(store.Lockers, store.Zones, store.Events, occupancy, store, clock);
        var zone = new ChangeLockerZoneHandler(store.Lockers, store.Zones, store.Events, occupancy, store, clock);
        var reserve = new ReserveLockerHandler(store.Lockers, store.Zones, store.Events, occupancy, store, clock);
        var removeReservation = new RemoveLockerReservationHandler(store.Lockers, store.Zones, store.Events, occupancy, store.StudentEvents, store, clock);
        var outOfService = new MarkLockerOutOfServiceHandler(store.Lockers, store.Zones, store.Events, occupancy, LockerHomeComposition.Services(store, clock), store, clock);
        var restore = new RestoreLockerServiceHandler(store.Lockers, store.Zones, store.Events, occupancy, store, clock);
        var retire = new RetireLockerHandler(store.Lockers, store.Zones, store.Events, occupancy, [], store, clock);
        var release = new Arca.Application.Assignments.ReleaseStudentLocker.ReleaseStudentLockerHandler(LockerHomeComposition.Services(store, clock), store, clock);
        var createZone = new CreateZoneHandler(store.Zones, store);
        var renameZone = new RenameZoneHandler(store.Zones, store.Lockers, store);
        var deactivateZone = new DeactivateZoneHandler(store.Zones, store.Lockers, store);
        var reactivateZone = new ReactivateZoneHandler(store.Zones, store.Lockers, store);
        var deleteZone = new DeleteZoneHandler(store.Zones, store.Lockers, store);

        static async Task<Result<string>> Said<T>(Task<Result<T>> run, Func<T, string> say)
        {
            var result = await run;
            return result.IsSuccess ? Result<string>.Success(say(result.Value!), [.. result.Notices]) : Result<string>.Failure(result.Error!);
        }

        return new LockerServices(
            rows.HandleAsync,
            (id, ct) => new GetLockerScreenHandler(rows, store.Lockers, occupancy, clock).HandleAsync(new GetLockerScreenRequest(id), ct),
            async (id, ct) =>
            {
                var history = await new GetLockerHistoryHandler(store.Lockers, store.Zones, store.Events, store.Students, localizer)
                    .HandleAsync(new GetLockerHistoryRequest(id), ct);
                return history.IsSuccess
                    ? Result<IReadOnlyList<string>>.Success([.. history.Value!.Select(e => $"{localizer.Format(e.At)}: {e.Text}")])
                    : Result<IReadOnlyList<string>>.Failure(history.Error!);
            },
            new ListZoneRowsHandler(store.Zones, store.Lockers).HandleAsync,
            (request, ct) => Said(add.HandleAsync(request, ct), text.LockerAdded),
            range.AnalyzeAsync,
            range.ApplyAsync,
            text.RangeCreated,
            (id, value, ct) => Said(number.HandleAsync(new ChangeLockerNumberRequest(id, value), ct), text.NumberChanged),
            (id, target, ct) => Said(zone.HandleAsync(new ChangeLockerZoneRequest(id, target), ct), text.ZoneChanged),
            (id, note, ct) => Said(reserve.HandleAsync(new ReserveLockerRequest(id, note), ct), text.LockerReserved),
            (id, ct) => Said(removeReservation.HandleAsync(new RemoveLockerReservationRequest(id), ct), text.ReservationRemoved),
            async (id, kind, decision, target, confirm, ct) =>
            {
                var result = await outOfService.HandleAsync(
                    new MarkLockerOutOfServiceRequest(
                        id, Enum.Parse<OutOfServiceKind>(kind.ToString()), decision is { } d ? Enum.Parse<OutOfServiceDecision>(d.ToString()) : null, target, confirm), ct);
                if (!result.IsSuccess)
                {
                    return Result<OutOfServiceView>.Failure(result.Error!);
                }

                var done = result.Value!;
                if (done.NeedsWarningConfirmation)
                {
                    return Result<OutOfServiceView>.Success(new OutOfServiceView(null, [], [.. done.Warnings!.Select(localizer.Message)]));
                }

                return Result<OutOfServiceView>.Success(done.DecisionRequired
                    ? new OutOfServiceView(null, [.. done.DecisionsOffered.Select(o => Enum.Parse<OutOfServiceDecisionView>(o.ToString()))], [])
                    : new OutOfServiceView(text.OutOfService(done.Locker), [], []));
            },
            (id, ct) => Said(restore.HandleAsync(new RestoreLockerServiceRequest(id), ct), text.ServiceRestored),
            (id, ct) => Said(retire.HandleAsync(new RetireLockerRequest(id), ct), text.Retired),
            (name, ct) => Said(createZone.HandleAsync(new CreateZoneRequest(name), ct), text.ZoneCreated),
            (id, name, ct) => Said(renameZone.HandleAsync(new RenameZoneRequest(id, name), ct), text.ZoneRenamed),
            (id, ct) => Said(deactivateZone.HandleAsync(new DeactivateZoneRequest(id), ct), text.ZoneDeactivated),
            (id, ct) => Said(reactivateZone.HandleAsync(new ReactivateZoneRequest(id), ct), text.ZoneReactivated),
            (id, ct) => Said(deleteZone.HandleAsync(new DeleteZoneRequest(id), ct), _ => text.ZoneDeleted()),
            (student, ct) => Said(release.HandleAsync(new Arca.Application.Assignments.ReleaseStudentLocker.ReleaseStudentLockerRequest(student), ct), new AssignmentResultTexts(localizer).Released));
    }
}
