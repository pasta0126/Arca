// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Students.AddStudent;

/// <param name="FirstName">Name of the student.</param>
/// <param name="LastName">Surnames of the student.</param>
/// <param name="Email">The email that identifies the student.</param>
/// <param name="LevelName">The level, as typed. It is required.</param>
/// <param name="GroupName">The group of that level, as typed; optional.</param>
/// <param name="ConfirmNewValues">True once the person has confirmed the creation of a level or group that is new.</param>
public sealed record AddStudentRequest(
    string? FirstName, string? LastName, string? Email, string? LevelName, string? GroupName = null, bool ConfirmNewValues = false);
