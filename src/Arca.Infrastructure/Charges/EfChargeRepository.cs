// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Charges;
using Arca.Domain.Charges;
using Arca.Domain.ConceptAmounts;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Charges;

sealed class EfChargeRepository(EfInventory owner) : IChargeRepository
{
    public Task<Charge?> GetAsync(Guid id, CancellationToken ct) =>
        owner.UseAsync(context => context.Set<Charge>().FirstOrDefaultAsync(c => c.Id == id, ct));

    public async Task<IReadOnlyList<Charge>> ListByStudentAsync(Guid studentId, CancellationToken ct) =>
        await owner.UseAsync(async context => (IReadOnlyList<Charge>)await context.Set<Charge>().Where(c => c.StudentId == studentId).ToListAsync(ct));

    public async Task<IReadOnlyList<Charge>> ListPendingAsync(CancellationToken ct) =>
        await owner.UseAsync(async context => (IReadOnlyList<Charge>)await context.Set<Charge>().Where(c => c.Status == ChargeStatus.Pending).ToListAsync(ct));

    public async Task<IReadOnlyList<Charge>> ListDepositsDueBackAsync(CancellationToken ct) =>
        await owner.UseAsync(async context => (IReadOnlyList<Charge>)await context.Set<Charge>()
            .Where(c => c.Concept == ChargeConcept.Deposit && c.Status == ChargeStatus.Paid && c.Return == DepositReturnStatus.ToReturn)
            .ToListAsync(ct));

    public Task<bool> AnyInYearAsync(Guid yearId, CancellationToken ct) =>
        owner.UseAsync(context => context.Set<Charge>().AnyAsync(c => c.YearId == yearId, ct));

    public Task AddAsync(Charge charge, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<Charge>().Add(charge);
        await context.SaveChangesAsync(ct);
    });

    public Task UpdateAsync(Charge charge, CancellationToken ct) => owner.WriteAsync(context => context.SaveChangesAsync(ct));
}
