// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arca.Infrastructure.Identity;

sealed class CentreIdentityConfiguration : IEntityTypeConfiguration<CentreIdentity>
{
    public void Configure(EntityTypeBuilder<CentreIdentity> identity)
    {
        identity.ToTable("CentreIdentity");
        identity.HasKey(i => i.Id);
        identity.Property(i => i.Id).ValueGeneratedNever();
        identity.Property(i => i.Name).HasMaxLength(CentreIdentity.MaximumNameLength);
        identity.Property(i => i.Logo);
        identity.Property(i => i.LogoContentType).HasMaxLength(20);
        identity.Property(i => i.Accent).HasMaxLength(7);
    }
}
