// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Students;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arca.Infrastructure.Students;

/// <summary>The history table of the students, tied to the student by its identity.</summary>
sealed class StudentEventConfiguration : IEntityTypeConfiguration<StudentEventRow>
{
    public void Configure(EntityTypeBuilder<StudentEventRow> row)
    {
        row.ToTable("StudentEvents");
        row.HasKey(e => e.Id);
        row.Property(e => e.Id).ValueGeneratedOnAdd();
        row.Property(e => e.Type).HasMaxLength(100).IsRequired();
        row.Property(e => e.OccurredAtUtc).HasConversion(Instants.RequiredConverter).IsRequired();
        row.HasOne<Student>().WithMany().HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Restrict);
        row.HasIndex(e => new { e.StudentId, e.OccurredAtUtc });
    }
}
