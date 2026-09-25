// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Students.ReactivateStudent;

/// <param name="StudentId">The retired student.</param>
/// <param name="LevelName">The level of the enrolment in the active year, as typed.</param>
/// <param name="GroupName">The group of that level, as typed; optional.</param>
/// <param name="ConfirmNewValues">True once the person has confirmed a level or group that is new.</param>
public sealed record ReactivateStudentRequest(Guid StudentId, string? LevelName, string? GroupName = null, bool ConfirmNewValues = false);
