// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Students.ChangeStudentEnrollment;

/// <param name="StudentId">The student.</param>
/// <param name="LevelName">The new level, as typed.</param>
/// <param name="GroupName">The new group of that level, as typed; optional.</param>
/// <param name="ConfirmNewValues">True once the person has confirmed a level or group that is new.</param>
public sealed record ChangeStudentEnrollmentRequest(Guid StudentId, string? LevelName, string? GroupName = null, bool ConfirmNewValues = false);
