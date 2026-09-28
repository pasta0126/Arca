// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Localization;

namespace Arca.Application.Assignments;

/// <summary>
/// The confirmations that the assignments ask for, prepared as localized data with their consequence
/// (docs/convenciones.md, section 5): the screen shows them through the confirmation service and only calls the use
/// case if the person explicitly confirms.
/// </summary>
public sealed class AssignmentConfirmations(ILocalizer localizer)
{
    /// <summary>Releasing a locker is reversible (it can be assigned again), but it happens right away once confirmed.</summary>
    public ConfirmationRequest ForRelease(AssignmentRow current) => ForRelease(current.StudentName, current.LockerNumber);

    /// <summary>The same confirmation when only the name of the student and the number of the locker are at hand, as in the detail of a locker.</summary>
    public ConfirmationRequest ForRelease(string studentName, int lockerNumber) => new(
        localizer.Get("Assignments.Label.ReleaseTitle", studentName),
        localizer.Get("Assignments.Label.ReleaseConsequence", lockerNumber),
        localizer.Get("Assignments.Label.ReleaseConfirm"));
}
