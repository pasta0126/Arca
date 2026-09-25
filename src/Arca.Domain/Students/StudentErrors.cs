// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Students;

/// <summary>Error codes of the students. Resource keys: Students.Error.&lt;Name&gt;.</summary>
public static class StudentErrors
{
    public static readonly Error FirstNameRequired = new("Students.FirstNameRequired");

    public static readonly Error LastNameRequired = new("Students.LastNameRequired");

    /// <summary>The name or the surname is too long. Args: {0} maximum length.</summary>
    public static Error NameTooLong(int maximum) => new("Students.NameTooLong", Args: [maximum]);

    /// <summary>The email is missing or does not look like an email address.</summary>
    public static readonly Error EmailInvalid = new("Students.EmailInvalid");

    /// <summary>Another student, active or retired, already has the email.</summary>
    public static readonly Error EmailInUse = new("Students.EmailInUse");

    /// <summary>The student is already retired.</summary>
    public static readonly Error AlreadyRetired = new("Students.AlreadyRetired");

    /// <summary>The student is not retired, so there is nothing to reactivate.</summary>
    public static readonly Error AlreadyActive = new("Students.AlreadyActive");

    /// <summary>Retiring a student needs a reason.</summary>
    public static readonly Error ReasonRequired = new("Students.ReasonRequired");

    /// <summary>The reason is too long. Args: {0} maximum length.</summary>
    public static Error ReasonTooLong(int maximum) => new("Students.ReasonTooLong", Args: [maximum]);

    /// <summary>The new values are the ones the student already has.</summary>
    public static readonly Error Unchanged = new("Students.Unchanged");

    public static readonly Error NotFound = new("Students.NotFound");
}
