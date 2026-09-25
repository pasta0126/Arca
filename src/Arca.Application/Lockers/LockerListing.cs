// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Lockers;

/// <summary>
/// The inventory: the rows that match the filters in a stable order, ready to be shown in a virtualised list, and the
/// counters of every locker that is not retired, in total and by zone, whatever the filters are.
/// </summary>
public sealed record LockerListing(IReadOnlyList<LockerRow> Rows, LockerCounters Total, IReadOnlyList<ZoneCounters> ByZone);
