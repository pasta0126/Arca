// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Lockers;
using Arca.Application.Students;

namespace Arca.Application.Assignments;

/// <summary>
/// The real occupancy (alumnes-i-assignacions, D10): a locker is occupied when it has a current assignment, and a student
/// holds the locker of their current assignment. It replaces the stand-ins of taquilles-i-zones and of the students, and it
/// answers for a whole set at once from the current assignments, so no one asks locker by locker.
/// </summary>
public sealed class AssignmentOccupancy(IAssignmentRepository assignments, ILockerRepository lockers) : ILockerOccupancy, IStudentLockers
{
    public async Task<IReadOnlySet<Guid>> OccupiedAmongAsync(IReadOnlyCollection<Guid> lockerIds, CancellationToken ct)
    {
        var current = await assignments.ListCurrentAsync(ct);
        return current.Select(a => a.LockerId).Where(lockerIds.Contains).ToHashSet();
    }

    public async Task<IReadOnlyDictionary<Guid, AssignedLocker>> CurrentAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct)
    {
        var current = (await assignments.ListCurrentAsync(ct)).Where(a => studentIds.Contains(a.StudentId)).ToList();
        if (current.Count == 0)
        {
            return new Dictionary<Guid, AssignedLocker>();
        }

        var numbers = (await lockers.ListAsync(includeRetired: true, ct)).ToDictionary(l => l.Id, l => l.Number);
        return current.ToDictionary(a => a.StudentId, a => new AssignedLocker(a.LockerId, numbers.GetValueOrDefault(a.LockerId)));
    }
}
