// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Lockers.CreateLockerRange;

/// <param name="First">The first number of the range.</param>
/// <param name="Last">The last number, included. Equal to the first for a single locker.</param>
/// <param name="ZoneId">The active zone where the lockers go.</param>
public sealed record CreateLockerRangeRequest(int First, int Last, Guid ZoneId);
