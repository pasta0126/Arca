// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.SchoolYears.CreateAcademicYear;

/// <param name="StartDate">First day of the course. Its year gives the name: 2026 gives "2026-2027".</param>
/// <param name="EndDate">Last day of the course.</param>
public sealed record CreateAcademicYearRequest(DateOnly StartDate, DateOnly EndDate);
