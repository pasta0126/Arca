// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Lockers;

namespace Arca.Application.Lockers;

/// <summary>
/// A locker as the lists and results show it. It carries no student data. The status is computed here from the facts,
/// with the occupancy given by the assignments.
/// </summary>
public sealed record LockerRow(
    Guid Id, int Number, Guid ZoneId, string ZoneName, LockerStatus Status, bool HasAssignment, bool IsReserved,
    string? Note, string? ReservationNote, bool IsRetired)
{
    public static LockerRow Of(Locker locker, string zoneName, bool hasAssignment)
    {
        var state = locker.StateWith(hasAssignment);
        return new LockerRow(
            locker.Id, locker.Number, locker.ZoneId, zoneName, state.Status, state.HasAssignment, state.IsReserved, locker.Note,
            locker.ReservationNote, locker.IsRetired);
    }
}
