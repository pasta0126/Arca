// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Domain.Common;
using Arca.Domain.Lockers;

namespace Arca.Application.Lockers.GetLockerScreen;

/// <param name="LockerId">The locker to open.</param>
public sealed record GetLockerScreenRequest(Guid LockerId);

/// <summary>
/// A locker as its detail shows it (pantalles-de-domini, D2): the row of the list and, for each operation offered, the reason it is
/// refused now, or null when it can be done. The screen enables or disables each action from these and never works them out.
/// </summary>
/// <param name="EditBlocked">Why the number and the zone cannot be edited (the locker is retired), or null.</param>
/// <param name="BrokenBlocked">Why it cannot be marked broken (it already is, or it is retired), or null. An occupied locker is allowed: the decision comes next.</param>
/// <param name="MaintenanceBlocked">Why it cannot be marked in maintenance, or null.</param>
public sealed record LockerScreenDetail(
    LockerListRow Row, Error? EditBlocked, Error? ReserveBlocked, Error? RemoveReservationBlocked, Error? BrokenBlocked,
    Error? MaintenanceBlocked, Error? RestoreBlocked, Error? RetireBlocked);

/// <summary>
/// Reads a locker with the reasons of its operations. Each reason is what the domain rule itself answers when it is tried on a
/// copy of the locker, so the screen and the operation can never disagree and no rule is written twice. It changes nothing.
/// </summary>
public sealed class GetLockerScreenHandler(ListLockerRowsHandler rows, ILockerRepository lockers, ILockerOccupancy occupancy, IClock clock)
{
    public async Task<Result<LockerScreenDetail>> HandleAsync(GetLockerScreenRequest request, CancellationToken ct)
    {
        var locker = await lockers.GetAsync(request.LockerId, ct);
        if (locker is null)
        {
            return Result<LockerScreenDetail>.Failure(LockerErrors.NotFound);
        }

        var listing = await rows.HandleAsync(ct);
        if (!listing.IsSuccess)
        {
            return Result<LockerScreenDetail>.Failure(listing.Error!);
        }

        var occupied = (await occupancy.OccupiedAmongAsync([locker.Id], ct)).Contains(locker.Id);
        var now = clock.UtcNow;
        Locker Copy() => new(
            locker.Id, locker.Number, locker.ZoneId, locker.Note, locker.OutOfService, locker.IsReserved, locker.ReservationNote,
            locker.RetiredAtUtc, locker.ReservedForStudentId);

        return Result<LockerScreenDetail>.Success(new LockerScreenDetail(
            listing.Value!.Rows.Single(r => r.Id == locker.Id),
            locker.IsRetired ? LockerErrors.Retired : null,
            Copy().Reserve(null, occupied, now).Error,
            Copy().RemoveReservation(now).Error,
            Copy().MarkOutOfService(OutOfServiceKind.Broken, null, occupied, now).Error,
            Copy().MarkOutOfService(OutOfServiceKind.Maintenance, null, occupied, now).Error,
            Copy().RestoreService(now).Error,
            Copy().Retire(occupied, now).Error));
    }
}
