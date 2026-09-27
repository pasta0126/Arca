// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Students;
using Arca.Domain.Common;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Students;

/// <summary>The history over the database. It only adds and lists: there is no way to update or delete an event.</summary>
sealed class EfStudentEventRepository(EfInventory owner) : IStudentEventRepository
{
    public Task AddAsync(HistoryEvent change, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<StudentEventRow>().Add(new StudentEventRow
        {
            StudentId = change.EntityId,
            Type = change.Type,
            OccurredAtUtc = change.OccurredAtUtc,
            BeforeJson = change.BeforeJson,
            AfterJson = change.AfterJson,
            Reason = change.Reason,
        });
        await context.SaveChangesAsync(ct);
    });

    public async Task<IReadOnlyList<HistoryEvent>> ListAsync(Guid studentId, CancellationToken ct) =>
        await owner.UseAsync(async context =>
        {
            var rows = await context.Set<StudentEventRow>()
                .AsNoTracking()
                .Where(e => e.StudentId == studentId)
                .OrderByDescending(e => e.OccurredAtUtc)
                .ThenByDescending(e => e.Id)
                .ToListAsync(ct);
            return (IReadOnlyList<HistoryEvent>)[.. rows.Select(e => new HistoryEvent(e.StudentId, e.Type, e.OccurredAtUtc, e.BeforeJson, e.AfterJson, e.Reason))];
        });
}
