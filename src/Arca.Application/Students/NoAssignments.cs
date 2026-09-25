// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Students;

/// <summary>The stand-in for the assignments while they do not exist: no student holds a locker.</summary>
public sealed class NoAssignments : IStudentLockers
{
    public Task<IReadOnlyDictionary<Guid, AssignedLocker>> CurrentAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlyDictionary<Guid, AssignedLocker>>(new Dictionary<Guid, AssignedLocker>());
}
