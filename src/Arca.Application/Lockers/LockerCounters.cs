// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Lockers;

/// <summary>How many lockers that are not retired there are, in total and by visible status.</summary>
public sealed record LockerCounters(int Active, int Free, int Occupied, int Broken, int Maintenance, int Reserved);
