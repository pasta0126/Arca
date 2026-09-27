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
    public ConfirmationRequest ForRelease(AssignmentRow current) => new(
        localizer.Get("Assignments.Label.ReleaseTitle", current.StudentName),
        localizer.Get("Assignments.Label.ReleaseConsequence", current.LockerNumber),
        localizer.Get("Assignments.Label.ReleaseConfirm"));
}
