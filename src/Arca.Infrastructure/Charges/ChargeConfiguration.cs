// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Charges;
using Arca.Infrastructure.Common;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;
using Arca.Infrastructure.ConceptAmounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arca.Infrastructure.Charges;

sealed class ChargeConfiguration : IEntityTypeConfiguration<Charge>
{
    public void Configure(EntityTypeBuilder<Charge> charge)
    {
        charge.ToTable("Charges");
        charge.HasKey(c => c.Id);
        charge.Property(c => c.Id).ValueGeneratedNever();
        charge.Property(c => c.StudentId).IsRequired();
        charge.Property(c => c.YearId).IsRequired();
        charge.Property(c => c.Concept).HasConversion<string>().HasMaxLength(30).IsRequired();
        charge.Property(c => c.Amount).HasConversion(MoneyConverter.Instance).IsRequired();
        charge.Property(c => c.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        charge.Property(c => c.PaidOn);
        charge.Property(c => c.Reason).HasMaxLength(Charge.MaximumReasonLength);
        charge.Property(c => c.Return).HasColumnName("ReturnStatus").HasConversion<string>().HasMaxLength(30).IsRequired();
        charge.Property(c => c.ReturnedOn);
        charge.Property(c => c.ReturnNote).HasMaxLength(Charge.MaximumReasonLength);
        charge.Ignore(c => c.CountsAsDebt);
        charge.Ignore(c => c.IsCurrentDeposit);
        charge.Ignore(c => c.IsDueBack);

        charge.HasOne<Student>().WithMany().HasForeignKey(c => c.StudentId).OnDelete(DeleteBehavior.Restrict);
        charge.HasOne<AcademicYear>().WithMany().HasForeignKey(c => c.YearId).OnDelete(DeleteBehavior.Restrict);

        // One fee per student and year, and one current deposit per student: the database refuses a second one even if a
        // race or a bug got past the use cases (pagaments, D4).
        charge.HasIndex(c => new { c.StudentId, c.YearId }).IsUnique().HasFilter("\"Concept\" = 'Fee'");
        charge.HasIndex(c => c.StudentId).IsUnique().HasFilter("\"Concept\" = 'Deposit' AND \"Status\" <> 'Voided' AND \"ReturnStatus\" <> 'Returned'");

        // What the debt queries ask for: the pending charges of a student, and of a year.
        charge.HasIndex(c => new { c.StudentId, c.Status });
        charge.HasIndex(c => new { c.YearId, c.Status });
    }
}
