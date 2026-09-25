// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Enrollments;

namespace Arca.Application.Enrollments;

public interface IEnrollmentRepository
{
    /// <summary>The enrolments of one school year.</summary>
    Task<IReadOnlyList<Enrollment>> ListByYearAsync(Guid yearId, CancellationToken ct);

    /// <summary>The enrolment of a student in a year, or null.</summary>
    Task<Enrollment?> GetAsync(Guid studentId, Guid yearId, CancellationToken ct);

    Task AddAsync(Enrollment enrollment, CancellationToken ct);

    Task UpdateAsync(Enrollment enrollment, CancellationToken ct);
}
