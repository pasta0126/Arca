// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Identity;

namespace Arca.Application.Identity;

/// <summary>Where the identity of the centre is kept: one row in the database.</summary>
public interface ICentreIdentityRepository
{
    /// <summary>The identity, or null while the centre has not defined any.</summary>
    Task<CentreIdentity?> GetAsync(CancellationToken ct);

    Task AddAsync(CentreIdentity identity, CancellationToken ct);

    /// <summary>Saves the changes made to the identity that was loaded from here.</summary>
    Task UpdateAsync(CentreIdentity identity, CancellationToken ct);
}
