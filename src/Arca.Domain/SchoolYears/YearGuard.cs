// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.SchoolYears;

/// <summary>
/// The single point that decides whether the enrolments and assignments of a year may be changed (alumnes-i-assignacions,
/// D12). Today only the active year accepts changes. It is a function of the year and the kind of operation, not just
/// "is it active", because cursos-i-historial extends it with the states of closing and closed.
/// </summary>
public static class YearGuard
{
    /// <summary>Null when the operation is allowed; otherwise the error that says why it is not.</summary>
    public static Error? Check(AcademicYear? year, YearOperation operation) => year switch
    {
        null => SchoolYearErrors.NoActiveYear,
        { IsActive: true } => null,
        _ => SchoolYearErrors.NotActive,
    };
}
