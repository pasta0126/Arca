// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Assignments;

namespace Arca.Application.Assignments;

/// <summary>Where assignments are kept, current and closed. Loads are explicit and complete: there is no lazy loading.</summary>
public interface IAssignmentRepository
{
    /// <summary>Every current assignment: it decides who holds what.</summary>
    Task<IReadOnlyList<Assignment>> ListCurrentAsync(CancellationToken ct);

    /// <summary>The current assignment of a student, or null.</summary>
    Task<Assignment?> GetCurrentOfStudentAsync(Guid studentId, CancellationToken ct);

    /// <summary>The current assignment of a locker, or null.</summary>
    Task<Assignment?> GetCurrentOfLockerAsync(Guid lockerId, CancellationToken ct);

    /// <summary>Every assignment of a student, current and closed.</summary>
    Task<IReadOnlyList<Assignment>> ListByStudentAsync(Guid studentId, CancellationToken ct);

    /// <summary>Every assignment of a locker, current and closed.</summary>
    Task<IReadOnlyList<Assignment>> ListByLockerAsync(Guid lockerId, CancellationToken ct);

    Task AddAsync(Assignment assignment, CancellationToken ct);

    /// <summary>Saves the changes made to an assignment that was loaded from here.</summary>
    Task UpdateAsync(Assignment assignment, CancellationToken ct);
}
