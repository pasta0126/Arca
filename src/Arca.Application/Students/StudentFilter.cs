// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Students;

/// <summary>What the list of students is filtered by. Everything is optional and the filters combine.</summary>
/// <param name="Text">Words of the name or surnames; every word has to appear, ignoring case and accents.</param>
/// <param name="LevelId">Only students enrolled in this level.</param>
/// <param name="GroupId">Only students enrolled in this group.</param>
/// <param name="LockerNumber">Only the student that holds the locker with this number.</param>
/// <param name="LockerState">With a locker, without one, or any.</param>
/// <param name="IncludeRetired">Also list the retired students, shown apart. By default only the active ones.</param>
public sealed record StudentFilter(
    string? Text = null, Guid? LevelId = null, Guid? GroupId = null, int? LockerNumber = null,
    StudentLockerState LockerState = StudentLockerState.Any, bool IncludeRetired = false);
