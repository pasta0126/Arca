// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.SchoolYears;

/// <summary>
/// The kinds of change to the data of a school year that the guard tells apart (alumnes-i-assignacions, D12), so that
/// cursos-i-historial can allow some of them in a year that is closing and none in a closed one.
/// </summary>
public enum YearOperation
{
    AddEnrollment,
    ChangeEnrollment,
    OpenAssignment,
    ChangeAssignment,
    CloseAssignment,
}
