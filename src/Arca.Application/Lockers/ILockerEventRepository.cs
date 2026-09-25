// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Lockers;

/// <summary>
/// The append-only history of the lockers. It has no way to change or delete an event, so nothing above it can either.
/// </summary>
public interface ILockerEventRepository
{
    Task AddAsync(HistoryEvent change, CancellationToken ct);

    /// <summary>The events of one locker, most recent first.</summary>
    Task<IReadOnlyList<HistoryEvent>> ListAsync(Guid lockerId, CancellationToken ct);
}
