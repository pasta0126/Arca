// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Lockers.CreateLockerRange;
using Arca.Application.Zones;

namespace Arca.Application.Lockers;

/// <summary>
/// The success messages of the zones and lockers, with their counts, in the active language. A screen passes one to the
/// command that runs the use case, so every action ends with a visible result (arquitectura-base, D14).
/// </summary>
public sealed class InventoryResultTexts(ILocalizer localizer)
{
    public string ZoneCreated(ZoneSummary zone) => localizer.Get("Zones.Result.Created", zone.Name);

    public string ZoneRenamed(ZoneSummary zone) => localizer.Get("Zones.Result.Renamed", zone.Name);

    public string ZoneDeactivated(ZoneSummary zone) => localizer.Get("Zones.Result.Deactivated", zone.Name);

    public string ZoneReactivated(ZoneSummary zone) => localizer.Get("Zones.Result.Reactivated", zone.Name);

    public string ZoneDeleted() => localizer.Get("Zones.Result.Deleted");

    public string LockerAdded(LockerRow locker) => localizer.Get("Lockers.Result.Added", locker.Number, locker.ZoneName);

    public string LockerReserved(LockerRow locker) => localizer.Get("Lockers.Result.Reserved", locker.Number);

    public string ReservationRemoved(LockerRow locker) => localizer.Get("Lockers.Result.ReservationRemoved", locker.Number);

    public string OutOfService(LockerRow locker) => localizer.Get("Lockers.Result.OutOfService", locker.Number);

    public string ServiceRestored(LockerRow locker) => localizer.Get("Lockers.Result.ServiceRestored", locker.Number);

    public string NumberChanged(LockerRow locker) => localizer.Get("Lockers.Result.NumberChanged", locker.Number);

    public string ZoneChanged(LockerRow locker) => localizer.Get("Lockers.Result.ZoneChanged", locker.Number, locker.ZoneName);

    public string Retired(LockerRow locker) => localizer.Get("Lockers.Result.Retired", locker.Number);

    /// <summary>What confirming a range did: how many lockers were created, or that nothing was.</summary>
    public string RangeCreated(CreateLockerRangeResult result) => result.Applied
        ? localizer.Get("Lockers.Result.RangeCreated", result.Created, result.Plan.ZoneName)
        : localizer.Get("Lockers.Result.RangeNotCreated");
}
