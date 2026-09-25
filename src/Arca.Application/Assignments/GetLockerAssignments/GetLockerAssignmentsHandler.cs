// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;
using Arca.Domain.Lockers;

namespace Arca.Application.Assignments.GetLockerAssignments;

/// <summary>
/// Every student that has had a locker, current and closed, most recent first. It goes by the identity of the locker, so a
/// retired locker and an active one that share a number each show only their own assignments.
/// </summary>
public sealed class GetLockerAssignmentsHandler(AssignmentServices assignments)
{
    public async Task<Result<IReadOnlyList<AssignmentRow>>> HandleAsync(GetLockerAssignmentsRequest request, CancellationToken ct)
    {
        if (await assignments.Lockers.GetAsync(request.LockerId, ct) is null)
        {
            return Result<IReadOnlyList<AssignmentRow>>.Failure(LockerErrors.NotFound);
        }

        var rows = new List<AssignmentRow>();
        foreach (var assignment in await assignments.Assignments.ListByLockerAsync(request.LockerId, ct))
        {
            rows.Add(await assignments.Flow.RowAsync(assignment, ct));
        }

        return Result<IReadOnlyList<AssignmentRow>>.Success([.. rows.OrderByDescending(r => r.StartedAtUtc).ThenBy(r => r.IsCurrent ? 0 : 1)]);
    }
}
