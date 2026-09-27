// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.ConceptAmounts;

namespace Arca.Application.ConceptAmounts;

/// <summary>Where the amounts by concept and year are kept. Loads are explicit and complete: there is no lazy loading.</summary>
public interface IConceptAmountRepository
{
    /// <summary>Every amount defined for a year, at most one per concept.</summary>
    Task<IReadOnlyList<ConceptAmount>> ListByYearAsync(Guid yearId, CancellationToken ct);

    Task AddAsync(ConceptAmount amount, CancellationToken ct);

    /// <summary>Saves the changes made to an amount that was loaded from here.</summary>
    Task UpdateAsync(ConceptAmount amount, CancellationToken ct);
}
