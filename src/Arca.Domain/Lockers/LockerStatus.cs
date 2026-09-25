// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.Lockers;

/// <summary>The status a person sees for a locker. It is derived from facts and never stored (taquilles-i-zones, D1).</summary>
public enum LockerStatus
{
    Free,
    Occupied,
    Reserved,
    Broken,
    Maintenance,
    Retired,
}
