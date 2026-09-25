// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Lockers;

namespace Arca.Application.Lockers;

/// <summary>What the inventory is filtered by. Everything is optional and the filters combine.</summary>
/// <param name="ZoneId">Only the lockers of this zone.</param>
/// <param name="Status">Only lockers with this visible status. Asking for retired ones includes them.</param>
/// <param name="Number">Only lockers with exactly this number.</param>
/// <param name="IncludeRetired">Also list the retired lockers, after the active ones that share their number.</param>
public sealed record LockerFilter(Guid? ZoneId = null, LockerStatus? Status = null, int? Number = null, bool IncludeRetired = false);
