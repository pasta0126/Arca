// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Lockers;
using Arca.Application.Lockers.AddLocker;
using Arca.Application.Lockers.ChangeLockerNumber;
using Arca.Application.Lockers.ChangeLockerZone;
using Arca.Application.Lockers.CreateLockerRange;
using Arca.Application.Lockers.GetLockerHistory;
using Arca.Application.Lockers.ListLockers;
using Arca.Application.Lockers.MarkLockerOutOfService;
using Arca.Application.Lockers.RemoveLockerReservation;
using Arca.Application.Lockers.ReserveLocker;
using Arca.Application.Lockers.RestoreLockerService;
using Arca.Application.Lockers.RetireLocker;
using Arca.Application.Zones;
using Arca.Application.Zones.CreateZone;
using Arca.Application.Zones.DeactivateZone;
using Arca.Application.Zones.DeleteZone;
using Arca.Application.Zones.ListZones;
using Arca.Application.Zones.ReactivateZone;
using Arca.Application.Zones.RenameZone;
using Arca.Domain.Lockers;
using Arca.Testing;
using Arca.Testing.Inventory;

namespace Arca.Application.Tests.Inventory;

/// <summary>Every use case of zones and lockers wired over the in-memory inventory, so a test reads as what a person does.</summary>
public sealed class InventoryWorld
{
    public InventoryWorld()
    {
        Clock = new FakeClock(new DateTimeOffset(2026, 9, 25, 9, 0, 0, TimeSpan.Zero));
        Hooks = [];
    }

    public InMemoryInventory Store { get; } = new();

    public FakeClock Clock { get; }

    public List<ILockerRetiredHandler> Hooks { get; }

    public ILocalizer Localizer { get; } = new ResxLocalizer();

    public CreateZoneHandler CreateZone => new(Store.Zones, Store);

    public RenameZoneHandler RenameZone => new(Store.Zones, Store.Lockers, Store);

    public DeactivateZoneHandler DeactivateZone => new(Store.Zones, Store.Lockers, Store);

    public ReactivateZoneHandler ReactivateZone => new(Store.Zones, Store.Lockers, Store);

    public DeleteZoneHandler DeleteZone => new(Store.Zones, Store.Lockers, Store);

    public ListZonesHandler ListZones => new(Store.Zones, Store.Lockers);

    public AddLockerHandler AddLocker => new(Store.Lockers, Store.Zones, Store.Events, Store, Clock);

    public ReserveLockerHandler ReserveLocker => new(Store.Lockers, Store.Zones, Store.Events, Store.Occupancy, Store, Clock);

    public RemoveLockerReservationHandler RemoveReservation => new(Store.Lockers, Store.Zones, Store.Events, Store.Occupancy, Store, Clock);

    public MarkLockerOutOfServiceHandler MarkOutOfService => new(Store.Lockers, Store.Zones, Store.Events, Store.Occupancy, Store, Clock);

    public RestoreLockerServiceHandler RestoreService => new(Store.Lockers, Store.Zones, Store.Events, Store.Occupancy, Store, Clock);

    public ChangeLockerNumberHandler ChangeNumber => new(Store.Lockers, Store.Zones, Store.Events, Store.Occupancy, Store, Clock);

    public ChangeLockerZoneHandler ChangeZone => new(Store.Lockers, Store.Zones, Store.Events, Store.Occupancy, Store, Clock);

    public RetireLockerHandler RetireLocker => new(Store.Lockers, Store.Zones, Store.Events, Store.Occupancy, Hooks, Store, Clock);

    public CreateLockerRangeHandler CreateRange => new(Store.Lockers, Store.Zones, Store.Events, Store, Clock);

    public GetLockerHistoryHandler History => new(Store.Lockers, Store.Zones, Store.Events, Localizer);

    public ListLockersHandler ListLockers => new(Store.Lockers, Store.Zones, Store.Occupancy);

    public async Task<Guid> ZoneAsync(string name) => (await CreateZone.HandleAsync(new CreateZoneRequest(name), default)).Value!.Id;

    public async Task<Guid> LockerAsync(int number, Guid zoneId, string? note = null)
    {
        Clock.Advance(TimeSpan.FromMinutes(1)); // so the events of one locker have distinct instants
        var added = await AddLocker.HandleAsync(new AddLockerRequest(number, zoneId, note), default);
        return added.Value!.Id;
    }

    public async Task<LockerRow> RowAsync(Guid lockerId)
    {
        var listing = await ListLockers.HandleAsync(new ListLockersRequest(new LockerFilter(IncludeRetired: true)), default);
        return listing.Value!.Rows.Single(r => r.Id == lockerId);
    }
}
