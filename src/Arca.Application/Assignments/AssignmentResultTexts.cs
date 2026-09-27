// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;

namespace Arca.Application.Assignments;

/// <summary>
/// The success messages of the assignments, in the active language: who has now which locker, in which zone. A screen
/// passes one to the command that runs the use case, so every action ends with a visible result (arquitectura-base, D14).
/// </summary>
public sealed class AssignmentResultTexts(ILocalizer localizer)
{
    public string Assigned(AssignmentRow assignment) =>
        localizer.Get("Assignments.Result.Assigned", assignment.StudentName, assignment.LockerNumber, assignment.ZoneName);

    public string Changed(AssignmentRow assignment) =>
        localizer.Get("Assignments.Result.Changed", assignment.StudentName, assignment.LockerNumber, assignment.ZoneName);

    public string Released(AssignmentRow assignment) => localizer.Get("Assignments.Result.Released", assignment.StudentName);
}
