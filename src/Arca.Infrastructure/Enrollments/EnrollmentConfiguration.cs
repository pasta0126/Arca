// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Catalog;
using Arca.Domain.Enrollments;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arca.Infrastructure.Enrollments;

/// <summary>
/// How enrolments are stored (alumnes-i-assignacions). A student has at most one per year, enforced by a unique index; the
/// group is optional but, when present, must belong to the level, a rule kept in the domain.
/// </summary>
sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> enrollment)
    {
        enrollment.ToTable("Enrollments");
        enrollment.HasKey(e => e.Id);
        enrollment.Property(e => e.Id).ValueGeneratedNever();
        enrollment.Property(e => e.StudentId).IsRequired();
        enrollment.Property(e => e.YearId).IsRequired();
        enrollment.Property(e => e.LevelId).IsRequired();

        enrollment.HasOne<Student>().WithMany().HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Restrict);
        enrollment.HasOne<AcademicYear>().WithMany().HasForeignKey(e => e.YearId).OnDelete(DeleteBehavior.Restrict);
        enrollment.HasOne<Level>().WithMany().HasForeignKey(e => e.LevelId).OnDelete(DeleteBehavior.Restrict);
        enrollment.HasOne<Group>().WithMany().HasForeignKey(e => e.GroupId).OnDelete(DeleteBehavior.Restrict);
        enrollment.HasIndex(e => new { e.StudentId, e.YearId }).IsUnique();
    }
}
