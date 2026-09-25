// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Students;

/// <summary>
/// A student as a list shows them (alumnes-i-assignacions, D11): name, level, group, state and locker. It never carries the
/// email, which only the detail of the student and the review of an import may show.
/// </summary>
public sealed record StudentRow(
    Guid Id, string FirstName, string LastName, string? LevelName, string? GroupName, bool IsRetired, int? LockerNumber);
