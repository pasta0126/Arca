// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Enrollments;

/// <summary>Error codes of the enrolments. Resource keys: Enrollments.Error.&lt;Name&gt;.</summary>
public static class EnrollmentErrors
{
    /// <summary>A student cannot be enrolled without a level.</summary>
    public static readonly Error LevelRequired = new("Enrollments.LevelRequired");

    /// <summary>The group does not belong to the chosen level.</summary>
    public static readonly Error GroupNotInLevel = new("Enrollments.GroupNotInLevel");

    /// <summary>The student already has an enrolment in that school year.</summary>
    public static readonly Error AlreadyEnrolled = new("Enrollments.AlreadyEnrolled");

    /// <summary>A retired student cannot be enrolled; it has to be reactivated first.</summary>
    public static readonly Error StudentRetired = new("Enrollments.StudentRetired");

    /// <summary>The new level and group are the ones the enrolment already has.</summary>
    public static readonly Error Unchanged = new("Enrollments.Unchanged");

    public static readonly Error NotFound = new("Enrollments.NotFound");
}
