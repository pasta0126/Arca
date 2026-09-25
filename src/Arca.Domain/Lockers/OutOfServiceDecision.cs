// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.Lockers;

/// <summary>What to do with the student when an occupied locker is put out of service (taquilles-i-zones, D3).</summary>
public enum OutOfServiceDecision
{
    /// <summary>Give the student another locker. Needs the assignments, so it is not available yet.</summary>
    Reassign,

    /// <summary>Leave the student assigned to the locker while it is out of service.</summary>
    Keep,

    /// <summary>Free the student from the locker. Needs the assignments, so it is not available yet.</summary>
    Release,
}
