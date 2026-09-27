// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.SchoolYears;
using Arca.Domain.Assignments;
using Arca.Domain.Enrollments;
using Arca.Domain.SchoolYears;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.SchoolYears;

sealed class EfAcademicYearRepository(EfInventory owner) : IAcademicYearRepository
{
    public async Task<IReadOnlyList<AcademicYear>> ListAsync(CancellationToken ct) =>
        await owner.UseAsync(async context => (IReadOnlyList<AcademicYear>)await context.Set<AcademicYear>().ToListAsync(ct));

    public Task<AcademicYear?> GetAsync(Guid id, CancellationToken ct) =>
        owner.UseAsync(context => context.Set<AcademicYear>().FirstOrDefaultAsync(y => y.Id == id, ct));

    public Task<AcademicYear?> GetActiveAsync(CancellationToken ct) =>
        owner.UseAsync(context => context.Set<AcademicYear>().FirstOrDefaultAsync(y => y.IsActive, ct));

    public Task AddAsync(AcademicYear year, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<AcademicYear>().Add(year);
        await context.SaveChangesAsync(ct);
    });

    public Task UpdateAsync(AcademicYear year, CancellationToken ct) => owner.WriteAsync(context => context.SaveChangesAsync(ct));

    public Task RemoveAsync(AcademicYear year, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<AcademicYear>().Remove(year);
        await context.SaveChangesAsync(ct);
    });

    public Task<bool> HasDataAsync(Guid yearId, CancellationToken ct) => owner.UseAsync(async context =>
        await context.Set<Enrollment>().AnyAsync(e => e.YearId == yearId, ct) || await context.Set<Assignment>().AnyAsync(a => a.YearId == yearId, ct));
}
