// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Lockers;

namespace Arca.Application.Lockers;

/// <summary>Where lockers are kept. Loads are explicit and complete: there is no lazy loading.</summary>
public interface ILockerRepository
{
    /// <summary>Every locker, with the retired ones only if asked for.</summary>
    Task<IReadOnlyList<Locker>> ListAsync(bool includeRetired, CancellationToken ct);

    Task<Locker?> GetAsync(Guid id, CancellationToken ct);

    Task AddAsync(Locker locker, CancellationToken ct);

    /// <summary>Saves the changes made to a locker that was loaded from here.</summary>
    Task UpdateAsync(Locker locker, CancellationToken ct);

    /// <summary>How many lockers of the zone are not retired.</summary>
    Task<int> CountActiveInZoneAsync(Guid zoneId, CancellationToken ct);

    /// <summary>Whether any locker, retired or not, belongs or belonged to the zone.</summary>
    Task<bool> HasEverHadLockersAsync(Guid zoneId, CancellationToken ct);
}
