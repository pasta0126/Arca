// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Lockers;

/// <summary>Why the inventory has nothing to show, so the screen can guide instead of leaving it blank (taquilles-i-zones, D9b).</summary>
public enum LockerEmptyState
{
    /// <summary>There is something to show.</summary>
    None,

    /// <summary>No active zone exists, so no locker can be added yet.</summary>
    NoZones,

    /// <summary>There are zones but no locker that is not retired.</summary>
    NoLockers,

    /// <summary>There are lockers, but none matches the filters.</summary>
    NoResults,
}
