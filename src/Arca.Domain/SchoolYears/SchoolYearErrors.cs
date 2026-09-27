// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.SchoolYears;

/// <summary>Error codes of the school year. Resource keys: SchoolYears.Error.&lt;Name&gt;.</summary>
public static class SchoolYearErrors
{
    /// <summary>The end is not after the start.</summary>
    public static readonly Error DatesInvalid = new("SchoolYears.DatesInvalid");

    /// <summary>The dates overlap those of another year. Args: {0} the name of that year.</summary>
    public static Error Overlaps(string name) => new("SchoolYears.Overlaps", Args: [name]);

    /// <summary>A year with the same start year already exists. Args: {0} the name.</summary>
    public static Error AlreadyExists(string name) => new("SchoolYears.AlreadyExists", Args: [name]);

    /// <summary>Another year is active, so this one cannot be activated before the active one starts closing.</summary>
    public static readonly Error AnotherActive = new("SchoolYears.AnotherActive");

    /// <summary>The year is not the active one, so its enrolments and assignments cannot be changed.</summary>
    public static readonly Error NotActive = new("SchoolYears.NotActive");

    /// <summary>There is no active year: one has to be created or activated first.</summary>
    public static readonly Error NoActiveYear = new("SchoolYears.NoActiveYear");

    /// <summary>The year has enrolments or assignments, so it cannot be deleted.</summary>
    public static readonly Error HasData = new("SchoolYears.HasData");

    /// <summary>There is no year with that identity.</summary>
    public static readonly Error NotFound = new("SchoolYears.NotFound");
}
