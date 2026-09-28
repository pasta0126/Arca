// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Charges;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arca.Infrastructure.Charges;

sealed class ChargeEventConfiguration : IEntityTypeConfiguration<ChargeEventRow>
{
    public void Configure(EntityTypeBuilder<ChargeEventRow> row)
    {
        row.ToTable("ChargeEvents");
        row.HasKey(e => e.Id);
        row.Property(e => e.Id).ValueGeneratedOnAdd();
        row.Property(e => e.Type).HasMaxLength(100).IsRequired();
        row.Property(e => e.OccurredAtUtc).HasConversion(Instants.RequiredConverter).IsRequired();
        row.HasOne<Charge>().WithMany().HasForeignKey(e => e.ChargeId).OnDelete(DeleteBehavior.Restrict);
        row.HasIndex(e => new { e.ChargeId, e.OccurredAtUtc });
    }
}
