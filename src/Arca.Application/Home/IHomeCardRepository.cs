// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Home;

namespace Arca.Application.Home;

/// <summary>The cards of the start screen and what the centre remembers about them, kept in the database of the centre (targetes-d-inici).</summary>
public interface IHomeCardRepository
{
    /// <summary>Every card, by position.</summary>
    Task<IReadOnlyList<HomeCard>> ListAsync(CancellationToken ct);

    Task<HomeCard?> GetAsync(Guid id, CancellationToken ct);

    Task AddAsync(HomeCard card, CancellationToken ct);

    /// <summary>Saves the changes made to a card that was loaded from here.</summary>
    Task UpdateAsync(HomeCard card, CancellationToken ct);

    Task RemoveAsync(HomeCard card, CancellationToken ct);

    /// <summary>When the cards of a new centre were created, or null while they have not been.</summary>
    Task<DateTimeOffset?> GetDefaultsCreatedAtAsync(CancellationToken ct);

    /// <summary>Remembers that the cards of a new centre were created.</summary>
    Task MarkDefaultsCreatedAsync(DateTimeOffset at, CancellationToken ct);
}
