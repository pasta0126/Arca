// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Students;
using Arca.Domain.Students;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Students;

sealed class EfStudentRepository(EfInventory owner) : IStudentRepository
{
    public async Task<IReadOnlyList<Student>> ListAsync(CancellationToken ct) =>
        await owner.UseAsync(async context => (IReadOnlyList<Student>)await context.Set<Student>().ToListAsync(ct));

    public Task<Student?> GetAsync(Guid id, CancellationToken ct) =>
        owner.UseAsync(context => context.Set<Student>().FirstOrDefaultAsync(s => s.Id == id, ct));

    public Task AddAsync(Student student, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync(ct);
    });

    public Task UpdateAsync(Student student, CancellationToken ct) => owner.WriteAsync(context => context.SaveChangesAsync(ct));
}
