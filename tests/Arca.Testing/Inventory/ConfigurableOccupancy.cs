// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Lockers;

namespace Arca.Testing.Inventory;

/// <summary>The stand-in for the assignments in the tests: the lockers a test marks as occupied are the occupied ones.</summary>
public sealed class ConfigurableOccupancy : ILockerOccupancy
{
    readonly HashSet<Guid> _occupied = [];

    public void Occupy(Guid lockerId) => _occupied.Add(lockerId);

    public void Free(Guid lockerId) => _occupied.Remove(lockerId);

    public Task<IReadOnlySet<Guid>> OccupiedAmongAsync(IReadOnlyCollection<Guid> lockerIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>(lockerIds.Where(_occupied.Contains)));
}
