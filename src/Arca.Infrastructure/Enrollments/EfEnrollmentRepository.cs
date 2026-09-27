// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Enrollments;
using Arca.Domain.Enrollments;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Enrollments;

sealed class EfEnrollmentRepository(EfInventory owner) : IEnrollmentRepository
{
    public async Task<IReadOnlyList<Enrollment>> ListByYearAsync(Guid yearId, CancellationToken ct) =>
        await owner.UseAsync(async context => (IReadOnlyList<Enrollment>)await context.Set<Enrollment>().Where(e => e.YearId == yearId).ToListAsync(ct));

    public Task<Enrollment?> GetAsync(Guid studentId, Guid yearId, CancellationToken ct) =>
        owner.UseAsync(context => context.Set<Enrollment>().FirstOrDefaultAsync(e => e.StudentId == studentId && e.YearId == yearId, ct));

    public Task AddAsync(Enrollment enrollment, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync(ct);
    });

    public Task UpdateAsync(Enrollment enrollment, CancellationToken ct) => owner.WriteAsync(context => context.SaveChangesAsync(ct));
}
