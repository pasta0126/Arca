// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Lockers;

namespace Arca.Application.Assignments;

/// <summary>The free locker suggested for a student and whether it is in another zone than the one chosen, so the screen says so.</summary>
public sealed record LockerSuggestion(LockerRow Locker, bool FromAnotherZone);
