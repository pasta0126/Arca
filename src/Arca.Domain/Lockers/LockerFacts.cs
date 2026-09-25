// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.Lockers;

/// <summary>What is known about a locker that decides its status. The occupancy comes from the assignments, not from the locker.</summary>
/// <param name="IsRetired">Retired for good.</param>
/// <param name="OutOfService">Broken or in maintenance, or null when in service.</param>
/// <param name="HasAssignment">A student holds the locker.</param>
/// <param name="IsReserved">Set aside, with or without a note.</param>
public readonly record struct LockerFacts(bool IsRetired, OutOfServiceKind? OutOfService, bool HasAssignment, bool IsReserved);
