// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Students;
using Arca.Application.Zones;
using Arca.Domain.Common;
using Arca.Domain.Students;

namespace Arca.Application.Lockers.RemoveLockerReservation;

public sealed class RemoveLockerReservationHandler(
    ILockerRepository lockers, IZoneRepository zones, ILockerEventRepository events, ILockerOccupancy occupancy,
    IStudentEventRepository studentEvents, IUnitOfWork unit, IClock clock)
{
    public Task<Result<LockerRow>> HandleAsync(RemoveLockerReservationRequest request, CancellationToken ct) =>
        unit.RunAsync(
            token =>
            {
                HistoryEvent? pendingStudentEvent = null;
                return new LockerChanges(lockers, zones, events, occupancy).ApplyAsync(
                    request.LockerId,
                    (locker, _) =>
                    {
                        var forStudent = locker.ReservedForStudentId;
                        var done = locker.RemoveReservation(clock.UtcNow);
                        if (done.IsSuccess && forStudent is { } studentId)
                        {
                            pendingStudentEvent = new HistoryEvent(studentId, StudentEventTypes.LockerReservationRemoved, clock.UtcNow, null, null);
                        }

                        return done.IsSuccess ? Result<HistoryEvent?>.Success(done.Value) : Result<HistoryEvent?>.Failure(done.Error!);
                    },
                    token,
                    async _ =>
                    {
                        if (pendingStudentEvent is not null)
                        {
                            await studentEvents.AddAsync(pendingStudentEvent, token);
                        }

                        return null;
                    });
            },
            ct);
}
