// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;
using Arca.Domain.Students;

namespace Arca.Application.Assignments.GetStudentAssignments;

/// <summary>Every locker a student has had, current and closed, with dates and year, most recent first, asked for when their record is opened.</summary>
public sealed class GetStudentAssignmentsHandler(AssignmentServices assignments)
{
    public async Task<Result<IReadOnlyList<AssignmentRow>>> HandleAsync(GetStudentAssignmentsRequest request, CancellationToken ct)
    {
        if (await assignments.Students.GetAsync(request.StudentId, ct) is null)
        {
            return Result<IReadOnlyList<AssignmentRow>>.Failure(StudentErrors.NotFound);
        }

        var rows = new List<AssignmentRow>();
        foreach (var assignment in await assignments.Assignments.ListByStudentAsync(request.StudentId, ct))
        {
            rows.Add(await assignments.Flow.RowAsync(assignment, ct));
        }

        return Result<IReadOnlyList<AssignmentRow>>.Success([.. rows.OrderByDescending(r => r.StartedAtUtc).ThenBy(r => r.IsCurrent ? 0 : 1)]);
    }
}
