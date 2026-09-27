// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Assignments;
using Arca.Domain.Lockers;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arca.Infrastructure.Assignments;

/// <summary>
/// How assignments are stored (alumnes-i-assignacions, D7). The partial unique indexes on a current assignment (no end)
/// enforce, even without the domain rule, that a student holds at most one locker and a locker is held by at most one
/// student.
/// </summary>
sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> assignment)
    {
        assignment.ToTable("Assignments");
        assignment.HasKey(a => a.Id);
        assignment.Property(a => a.Id).ValueGeneratedNever();
        assignment.Property(a => a.StudentId).IsRequired();
        assignment.Property(a => a.LockerId).IsRequired();
        assignment.Property(a => a.YearId).IsRequired();
        assignment.Property(a => a.StartedAtUtc).HasConversion(Instants.RequiredConverter).IsRequired();
        assignment.Property(a => a.EndedAtUtc).HasConversion(Instants.Converter);
        assignment.Property(a => a.CloseReason).HasConversion<string>().HasMaxLength(30);
        assignment.Property(a => a.CloseNote).HasMaxLength(Assignment.MaximumNoteLength);
        assignment.Ignore(a => a.IsCurrent);

        assignment.HasOne<Student>().WithMany().HasForeignKey(a => a.StudentId).OnDelete(DeleteBehavior.Restrict);
        assignment.HasOne<Locker>().WithMany().HasForeignKey(a => a.LockerId).OnDelete(DeleteBehavior.Restrict);
        assignment.HasOne<AcademicYear>().WithMany().HasForeignKey(a => a.YearId).OnDelete(DeleteBehavior.Restrict);
        assignment.HasIndex(a => a.StudentId).IsUnique().HasFilter("\"EndedAtUtc\" IS NULL");
        assignment.HasIndex(a => a.LockerId).IsUnique().HasFilter("\"EndedAtUtc\" IS NULL");
    }
}
