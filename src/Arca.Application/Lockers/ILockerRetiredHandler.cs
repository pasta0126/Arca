// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Lockers;

/// <summary>
/// A hook called inside the same transaction that retires a locker (taquilles-i-zones, D3b), so other capabilities, such
/// as the incidents, can close what depended on it. If it throws, the retirement is undone. This change defines and calls
/// it; it has no implementations yet.
/// </summary>
public interface ILockerRetiredHandler
{
    Task HandleAsync(Guid lockerId, DateTimeOffset retiredAtUtc, CancellationToken ct);
}
