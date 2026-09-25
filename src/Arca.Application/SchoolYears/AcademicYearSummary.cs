// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.SchoolYears;

namespace Arca.Application.SchoolYears;

/// <summary>A school year as the lists and results show it.</summary>
public sealed record AcademicYearSummary(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate, bool IsActive)
{
    public static AcademicYearSummary Of(AcademicYear year) => new(year.Id, year.Name, year.StartDate, year.EndDate, year.IsActive);
}
