// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Students;

namespace Arca.Testing.Inventory;

/// <summary>The stand-in for the assignments in the tests: the lockers a test gives to students are the ones they hold.</summary>
public sealed class ConfigurableStudentLockers : IStudentLockers
{
    readonly Dictionary<Guid, AssignedLocker> _held = [];

    public void Give(Guid studentId, int number) => _held[studentId] = new AssignedLocker(Guid.NewGuid(), number);

    public void Take(Guid studentId) => _held.Remove(studentId);

    public Task<IReadOnlyDictionary<Guid, AssignedLocker>> CurrentAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlyDictionary<Guid, AssignedLocker>>(
            studentIds.Where(_held.ContainsKey).ToDictionary(id => id, id => _held[id]));
}
