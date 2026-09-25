// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Zones;

namespace Arca.Application.Zones;

/// <summary>Where zones are kept. Loads are explicit and complete: there is no lazy loading.</summary>
public interface IZoneRepository
{
    /// <summary>Every zone, active or not.</summary>
    Task<IReadOnlyList<Zone>> ListAsync(CancellationToken ct);

    Task<Zone?> GetAsync(Guid id, CancellationToken ct);

    Task AddAsync(Zone zone, CancellationToken ct);

    /// <summary>Saves the changes made to a zone that was loaded from here.</summary>
    Task UpdateAsync(Zone zone, CancellationToken ct);

    Task RemoveAsync(Zone zone, CancellationToken ct);
}
