// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.Lockers;

/// <summary>
/// The visible status plus what a locker keeps underneath it: a broken locker can still hold its student's assignment or
/// a reservation, and both come back into view when the breakdown is resolved.
/// </summary>
public readonly record struct LockerState(LockerStatus Status, bool HasAssignment, bool IsReserved);
