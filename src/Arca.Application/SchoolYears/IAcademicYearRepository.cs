// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.SchoolYears;

namespace Arca.Application.SchoolYears;

/// <summary>Where school years are kept. Loads are explicit and complete: there is no lazy loading.</summary>
public interface IAcademicYearRepository
{
    Task<IReadOnlyList<AcademicYear>> ListAsync(CancellationToken ct);

    Task<AcademicYear?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>The active year, or null if there is none.</summary>
    Task<AcademicYear?> GetActiveAsync(CancellationToken ct);

    Task AddAsync(AcademicYear year, CancellationToken ct);

    /// <summary>Saves the changes made to a year that was loaded from here.</summary>
    Task UpdateAsync(AcademicYear year, CancellationToken ct);

    Task RemoveAsync(AcademicYear year, CancellationToken ct);

    /// <summary>Whether any enrolment or assignment belongs to the year.</summary>
    Task<bool> HasDataAsync(Guid yearId, CancellationToken ct);
}
