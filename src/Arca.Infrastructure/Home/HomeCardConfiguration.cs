// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Home;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arca.Infrastructure.Home;

sealed class HomeCardConfiguration : IEntityTypeConfiguration<HomeCard>
{
    public void Configure(EntityTypeBuilder<HomeCard> card)
    {
        card.ToTable("HomeCards");
        card.HasKey(c => c.Id);
        card.Property(c => c.Id).ValueGeneratedNever();
        card.Property(c => c.Title).HasMaxLength(HomeCard.MaximumTitleLength).IsRequired();
        card.Property(c => c.Target).HasConversion<string>().HasMaxLength(20).IsRequired();
        card.Property(c => c.CriteriaText).HasField("_criteriaText").UsePropertyAccessMode(PropertyAccessMode.Field).HasMaxLength(2000).IsRequired();
        card.Property(c => c.Position).IsRequired();
        card.Property(c => c.SeedKey).HasMaxLength(40);
        card.Ignore(c => c.Criteria);
        card.HasIndex(c => c.Position);
        card.HasIndex(c => c.SeedKey).IsUnique();
    }
}

sealed class HomeCardsStateConfiguration : IEntityTypeConfiguration<HomeCardsState>
{
    public void Configure(EntityTypeBuilder<HomeCardsState> state)
    {
        state.ToTable("HomeCardsState");
        state.HasKey(s => s.Id);
        state.Property(s => s.Id).ValueGeneratedNever();
        state.Property(s => s.DefaultsCreatedAtUtc).HasConversion(Instants.RequiredConverter).IsRequired();
    }
}
