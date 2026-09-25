// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Lockers;
using Arca.Domain.Zones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arca.Infrastructure.Inventory;

/// <summary>
/// How lockers are stored (taquilles-i-zones). The number is unique only among lockers that are not retired, through a
/// partial index (D4): a retired locker may share its number with an active one or with another retired one. The
/// status is not stored, only the facts it is derived from.
/// </summary>
sealed class LockerConfiguration : IEntityTypeConfiguration<Locker>
{
    public void Configure(EntityTypeBuilder<Locker> locker)
    {
        locker.ToTable("Lockers");
        locker.HasKey(l => l.Id);
        locker.Property(l => l.Id).ValueGeneratedNever();
        locker.Property(l => l.Number).IsRequired();
        locker.Property(l => l.ZoneId).IsRequired();
        locker.Property(l => l.Note).HasMaxLength(Locker.MaximumNoteLength);
        locker.Property(l => l.OutOfService).HasConversion<string>().HasMaxLength(20);
        locker.Property(l => l.IsReserved).IsRequired();
        locker.Property(l => l.ReservationNote).HasMaxLength(Locker.MaximumNoteLength);
        locker.Property(l => l.RetiredAtUtc).HasConversion(Instants.Converter);
        locker.Ignore(l => l.IsRetired);

        locker.HasOne<Zone>().WithMany().HasForeignKey(l => l.ZoneId).OnDelete(DeleteBehavior.Restrict);
        locker.HasIndex(l => l.Number).IsUnique().HasFilter("\"RetiredAtUtc\" IS NULL");
        locker.HasIndex(l => l.ZoneId);
    }
}
