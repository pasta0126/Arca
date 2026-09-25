// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Zones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arca.Infrastructure.Inventory;

/// <summary>How zones are stored. The unique index on the normalised name is the safety net of the domain rule (taquilles-i-zones, D5).</summary>
sealed class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
    public void Configure(EntityTypeBuilder<Zone> zone)
    {
        zone.ToTable("Zones");
        zone.HasKey(z => z.Id);
        zone.Property(z => z.Id).ValueGeneratedNever();
        zone.Property(z => z.Name).HasMaxLength(ZoneName.MaximumLength).IsRequired();
        zone.Property(z => z.NameKey).HasMaxLength(ZoneName.MaximumLength * 2).IsRequired();
        zone.Property(z => z.IsActive).IsRequired();
        zone.HasIndex(z => z.NameKey).IsUnique();
    }
}
