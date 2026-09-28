// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.ConceptAmounts;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arca.Infrastructure.ConceptAmounts;

sealed class ConceptAmountEventConfiguration : IEntityTypeConfiguration<ConceptAmountEventRow>
{
    public void Configure(EntityTypeBuilder<ConceptAmountEventRow> row)
    {
        row.ToTable("ConceptAmountEvents");
        row.HasKey(e => e.Id);
        row.Property(e => e.Id).ValueGeneratedOnAdd();
        row.Property(e => e.Type).HasMaxLength(100).IsRequired();
        row.Property(e => e.OccurredAtUtc).HasConversion(Instants.RequiredConverter).IsRequired();
        row.HasOne<ConceptAmount>().WithMany().HasForeignKey(e => e.ConceptAmountId).OnDelete(DeleteBehavior.Restrict);
        row.HasIndex(e => new { e.ConceptAmountId, e.OccurredAtUtc });
    }
}
