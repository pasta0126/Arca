// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Charges;

/// <summary>The append-only history of the charges. It has no way to change or delete an event.</summary>
public interface IChargeEventRepository
{
    Task AddAsync(HistoryEvent change, CancellationToken ct);

    /// <summary>The events of one charge, most recent first.</summary>
    Task<IReadOnlyList<HistoryEvent>> ListAsync(Guid chargeId, CancellationToken ct);
}
