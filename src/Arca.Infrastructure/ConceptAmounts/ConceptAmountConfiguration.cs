// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.ConceptAmounts;
using Arca.Infrastructure.Common;
using Arca.Domain.SchoolYears;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arca.Infrastructure.ConceptAmounts;

sealed class ConceptAmountConfiguration : IEntityTypeConfiguration<ConceptAmount>
{
    public void Configure(EntityTypeBuilder<ConceptAmount> amount)
    {
        amount.ToTable("ConceptAmounts");
        amount.HasKey(a => a.Id);
        amount.Property(a => a.Id).ValueGeneratedNever();
        amount.Property(a => a.YearId).IsRequired();
        amount.Property(a => a.Concept).HasConversion<string>().HasMaxLength(30).IsRequired();
        amount.Property(a => a.Amount).HasConversion(MoneyConverter.Instance).IsRequired();
        amount.HasOne<AcademicYear>().WithMany().HasForeignKey(a => a.YearId).OnDelete(DeleteBehavior.Restrict);
        amount.HasIndex(a => new { a.YearId, a.Concept }).IsUnique();
    }
}
