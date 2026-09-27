// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;

namespace Arca.Application.Students;

/// <summary>
/// The success messages of the students, in the active language. A screen passes one to the command that runs the use
/// case, so every action ends with a visible result (arquitectura-base, D14).
/// </summary>
public sealed class StudentResultTexts(ILocalizer localizer)
{
    public string Added(StudentDetail student) => localizer.Get("Students.Result.Added", student.FirstName, student.LastName);

    public string Edited(StudentDetail student) => localizer.Get("Students.Result.Edited", student.FirstName, student.LastName);

    public string Retired(StudentDetail student) => localizer.Get("Students.Result.Retired", student.FirstName, student.LastName);

    public string Reactivated(StudentDetail student) => localizer.Get("Students.Result.Reactivated", student.FirstName, student.LastName);

    public string EnrollmentChanged(StudentDetail student) =>
        localizer.Get("Students.Result.EnrollmentChanged", student.FirstName, student.LastName);
}
