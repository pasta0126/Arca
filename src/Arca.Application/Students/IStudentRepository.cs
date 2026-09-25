// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Students;

namespace Arca.Application.Students;

/// <summary>Where students are kept. Loads are explicit and complete: there is no lazy loading.</summary>
public interface IStudentRepository
{
    /// <summary>Every student, retired ones included: the email is unique among all of them.</summary>
    Task<IReadOnlyList<Student>> ListAsync(CancellationToken ct);

    Task<Student?> GetAsync(Guid id, CancellationToken ct);

    Task AddAsync(Student student, CancellationToken ct);

    /// <summary>Saves the changes made to a student that was loaded from here.</summary>
    Task UpdateAsync(Student student, CancellationToken ct);
}
