// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Lockers;

/// <summary>
/// Which lockers a student holds (taquilles-i-zones, D2). The assignments provide the real answer; until they exist
/// <see cref="NoOccupancy"/> says that no locker is occupied.
/// </summary>
public interface ILockerOccupancy
{
    /// <summary>The lockers, among the ones asked about, that are held by a student.</summary>
    Task<IReadOnlySet<Guid>> OccupiedAmongAsync(IReadOnlyCollection<Guid> lockerIds, CancellationToken ct);
}
