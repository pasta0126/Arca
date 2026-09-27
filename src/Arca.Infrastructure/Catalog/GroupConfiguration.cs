// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arca.Infrastructure.Catalog;

/// <summary>How groups are stored (alumnes-i-assignacions, D6). A group belongs to a level: its name is unique only within that level.</summary>
sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> group)
    {
        group.ToTable("Groups");
        group.HasKey(g => g.Id);
        group.Property(g => g.Id).ValueGeneratedNever();
        group.Property(g => g.LevelId).IsRequired();
        group.Property(g => g.Name).HasMaxLength(Level.MaximumNameLength).IsRequired();
        group.Property(g => g.NameKey).HasMaxLength(Level.MaximumNameLength * 2).IsRequired();

        group.HasOne<Level>().WithMany().HasForeignKey(g => g.LevelId).OnDelete(DeleteBehavior.Restrict);
        group.HasIndex(g => new { g.LevelId, g.NameKey }).IsUnique();
    }
}
