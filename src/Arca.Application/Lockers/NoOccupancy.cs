// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Lockers;

/// <summary>The stand-in for the assignments while they do not exist: no locker is occupied.</summary>
public sealed class NoOccupancy : ILockerOccupancy
{
    public Task<IReadOnlySet<Guid>> OccupiedAmongAsync(IReadOnlyCollection<Guid> lockerIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
}
