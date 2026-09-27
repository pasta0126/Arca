// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.SchoolYears;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arca.Infrastructure.SchoolYears;

/// <summary>How school years are stored (alumnes-i-assignacions). The dates are plain calendar dates, with no time or zone.</summary>
sealed class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
{
    public void Configure(EntityTypeBuilder<AcademicYear> year)
    {
        year.ToTable("AcademicYears");
        year.HasKey(y => y.Id);
        year.Property(y => y.Id).ValueGeneratedNever();
        year.Property(y => y.StartDate).IsRequired();
        year.Property(y => y.EndDate).IsRequired();
        year.Property(y => y.IsActive).IsRequired();
        year.Ignore(y => y.StartYear);
        year.Ignore(y => y.Name);
    }
}
