// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Students;

/// <summary>The locker a student holds now: its identity and the number a person sees.</summary>
public sealed record AssignedLocker(Guid LockerId, int Number);

/// <summary>
/// Which locker each student holds (alumnes-i-assignacions). The assignments provide the real answer; until they exist
/// <see cref="NoAssignments"/> says that nobody holds a locker. Asked for a set of students in one query.
/// </summary>
public interface IStudentLockers
{
    Task<IReadOnlyDictionary<Guid, AssignedLocker>> CurrentAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct);
}
