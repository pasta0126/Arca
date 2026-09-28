// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.ConceptAmounts;

/// <summary>The append-only history of the amounts by concept. It has no way to change or delete an event.</summary>
public interface IConceptAmountEventRepository
{
    Task AddAsync(HistoryEvent change, CancellationToken ct);

    /// <summary>The events of one amount, most recent first.</summary>
    Task<IReadOnlyList<HistoryEvent>> ListAsync(Guid conceptAmountId, CancellationToken ct);
}
