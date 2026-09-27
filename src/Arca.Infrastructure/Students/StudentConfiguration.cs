// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Students;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arca.Infrastructure.Students;

/// <summary>
/// How students are stored (alumnes-i-assignacions, D2). The email is unique among every student, retired ones included, so
/// a reappearing student is recognised, not duplicated; the name key has no uniqueness, since namesakes are legitimate.
/// </summary>
sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> student)
    {
        student.ToTable("Students");
        student.HasKey(s => s.Id);
        student.Property(s => s.Id).ValueGeneratedNever();
        student.Property(s => s.FirstName).HasMaxLength(Student.MaximumNameLength).IsRequired();
        student.Property(s => s.LastName).HasMaxLength(Student.MaximumNameLength).IsRequired();
        student.Property(s => s.Email).HasMaxLength(EmailAddress.MaximumLength).IsRequired();
        student.Property(s => s.NameKey).HasMaxLength(Student.MaximumNameLength * 2 + 1).IsRequired(); // first name + " " + last name
        student.Property(s => s.RetiredAtUtc).HasConversion(Instants.Converter);
        student.Property(s => s.RetirementReason).HasMaxLength(Student.MaximumReasonLength);
        student.Ignore(s => s.IsRetired);

        student.HasIndex(s => s.Email).IsUnique();
        student.HasIndex(s => s.NameKey);
    }
}
