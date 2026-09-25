// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Lockers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arca.Infrastructure.Inventory;

/// <summary>The history table (taquilles-i-zones, D7): tied to the locker by its identity, never to its number.</summary>
sealed class LockerEventConfiguration : IEntityTypeConfiguration<LockerEventRow>
{
    public void Configure(EntityTypeBuilder<LockerEventRow> row)
    {
        row.ToTable("LockerEvents");
        row.HasKey(e => e.Id);
        row.Property(e => e.Id).ValueGeneratedOnAdd();
        row.Property(e => e.Type).HasMaxLength(100).IsRequired();
        row.Property(e => e.OccurredAtUtc).HasConversion(Instants.RequiredConverter).IsRequired();
        row.HasOne<Locker>().WithMany().HasForeignKey(e => e.LockerId).OnDelete(DeleteBehavior.Restrict);
        row.HasIndex(e => new { e.LockerId, e.OccurredAtUtc });
    }
}
