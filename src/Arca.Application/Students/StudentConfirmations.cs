// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Localization;

namespace Arca.Application.Students;

/// <summary>
/// The confirmations that alumnes-i-assignacions asks for, prepared as localized data with their consequence
/// (docs/convenciones.md, section 5): the screen shows them through the confirmation service and only calls the use
/// case if the person explicitly confirms.
/// </summary>
public sealed class StudentConfirmations(ILocalizer localizer)
{
    /// <summary>
    /// Retiring a student is reversible (they can be reactivated), but it frees their locker right away, which the
    /// person must know before confirming.
    /// </summary>
    public ConfirmationRequest ForRetire(StudentDetail student) => new(
        localizer.Get("Students.Label.RetireTitle", student.FirstName, student.LastName),
        student.LockerNumber is { } number
            ? localizer.Get("Students.Label.RetireConsequenceWithLocker", student.FirstName, student.LastName, number)
            : localizer.Get("Students.Label.RetireConsequenceNoLocker", student.FirstName, student.LastName),
        localizer.Get("Students.Label.RetireConfirm"));
}
