// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arca.Infrastructure.Catalog;

/// <summary>How levels are stored (alumnes-i-assignacions, D6). The catalogue only grows: the unique index on the normalised name is the safety net.</summary>
sealed class LevelConfiguration : IEntityTypeConfiguration<Level>
{
    public void Configure(EntityTypeBuilder<Level> level)
    {
        level.ToTable("Levels");
        level.HasKey(l => l.Id);
        level.Property(l => l.Id).ValueGeneratedNever();
        level.Property(l => l.Name).HasMaxLength(Level.MaximumNameLength).IsRequired();
        level.Property(l => l.NameKey).HasMaxLength(Level.MaximumNameLength * 2).IsRequired();
        level.HasIndex(l => l.NameKey).IsUnique();
    }
}
