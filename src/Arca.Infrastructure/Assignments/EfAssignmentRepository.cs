// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Domain.Assignments;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Assignments;

sealed class EfAssignmentRepository(EfInventory owner) : IAssignmentRepository
{
    public async Task<IReadOnlyList<Assignment>> ListCurrentAsync(CancellationToken ct) =>
        await owner.UseAsync(async context => (IReadOnlyList<Assignment>)await context.Set<Assignment>().Where(a => a.EndedAtUtc == null).ToListAsync(ct));

    public Task<Assignment?> GetCurrentOfStudentAsync(Guid studentId, CancellationToken ct) =>
        owner.UseAsync(context => context.Set<Assignment>().FirstOrDefaultAsync(a => a.EndedAtUtc == null && a.StudentId == studentId, ct));

    public Task<Assignment?> GetCurrentOfLockerAsync(Guid lockerId, CancellationToken ct) =>
        owner.UseAsync(context => context.Set<Assignment>().FirstOrDefaultAsync(a => a.EndedAtUtc == null && a.LockerId == lockerId, ct));

    public async Task<IReadOnlyList<Assignment>> ListByStudentAsync(Guid studentId, CancellationToken ct) =>
        await owner.UseAsync(async context => (IReadOnlyList<Assignment>)await context.Set<Assignment>()
            .Where(a => a.StudentId == studentId).OrderByDescending(a => a.StartedAtUtc).ToListAsync(ct));

    public async Task<IReadOnlyList<Assignment>> ListByLockerAsync(Guid lockerId, CancellationToken ct) =>
        await owner.UseAsync(async context => (IReadOnlyList<Assignment>)await context.Set<Assignment>()
            .Where(a => a.LockerId == lockerId).OrderByDescending(a => a.StartedAtUtc).ToListAsync(ct));

    public Task AddAsync(Assignment assignment, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<Assignment>().Add(assignment);
        await context.SaveChangesAsync(ct);
    });

    public Task UpdateAsync(Assignment assignment, CancellationToken ct) => owner.WriteAsync(context => context.SaveChangesAsync(ct));
}
