// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Lockers;
using Arca.Application.Lockers.ChangeLockerNumber;
using Arca.Application.Lockers.ChangeLockerZone;
using Arca.Application.Lockers.GetLockerHistory;
using Arca.Application.Lockers.ListLockers;
using Arca.Application.Lockers.MarkLockerOutOfService;
using Arca.Application.Lockers.RemoveLockerReservation;
using Arca.Application.Lockers.ReserveLocker;
using Arca.Application.Lockers.RestoreLockerService;
using Arca.Application.Lockers.RetireLocker;
using Arca.Domain.Lockers;
using Xunit;

namespace Arca.Application.Tests.Inventory;

public sealed class LockerQueryTests
{
    const string Spec = "taquilles-i-zones/taquilles";

    static ListLockersRequest Filter(Guid? zone = null, LockerStatus? status = null, int? number = null, bool retired = false) =>
        new(new LockerFilter(zone, status, number, retired));

    // --- History ---

    [Fact]
    [Trait("spec", Spec + ": Historial de eventos de la taquilla (Consulta del historial)")]
    public async Task The_history_is_most_recent_first_and_written_in_catalan()
    {
        var world = new InventoryWorld();
        var first = await world.ZoneAsync("Planta 1");
        var second = await world.ZoneAsync("Gimnàs");
        var locker = await world.LockerAsync(15, first);
        world.Clock.Advance(TimeSpan.FromMinutes(1));
        await world.ChangeZone.HandleAsync(new ChangeLockerZoneRequest(locker, second), default);
        world.Clock.Advance(TimeSpan.FromMinutes(1));
        await world.ChangeNumber.HandleAsync(new ChangeLockerNumberRequest(locker, 16), default);

        var history = await world.History.HandleAsync(new GetLockerHistoryRequest(locker), default);

        var entries = history.Value!;
        Assert.Equal([LockerEventTypes.NumberChanged, LockerEventTypes.ZoneChanged, LockerEventTypes.Created], entries.Select(e => e.Type));
        Assert.Equal("Número canviat de 15 a 16.", entries[0].Text);
        Assert.Equal("Taquilla moguda de la zona Planta 1 a la zona Gimnàs.", entries[1].Text);
        Assert.Equal("Taquilla creada amb el número 15 a la zona Planta 1.", entries[2].Text);
    }

    [Fact]
    [Trait("spec", Spec + ": Historial de eventos de la taquilla (Consulta del historial)")]
    public async Task The_history_covers_reservations_breakdowns_and_retirement()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        await world.ReserveLocker.HandleAsync(new ReserveLockerRequest(locker, "Professorat"), default);
        await world.RemoveReservation.HandleAsync(new RemoveLockerReservationRequest(locker), default);
        await world.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(locker, OutOfServiceKind.Broken), default);
        await world.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(locker, OutOfServiceKind.Maintenance), default);
        await world.RestoreService.HandleAsync(new RestoreLockerServiceRequest(locker), default);
        await world.RetireLocker.HandleAsync(new RetireLockerRequest(locker), default);

        var texts = (await world.History.HandleAsync(new GetLockerHistoryRequest(locker), default)).Value!.Select(e => e.Text).ToList();

        Assert.Contains("Taquilla reservada. Nota: Professorat", texts);
        Assert.Contains("S'ha tret la reserva de la taquilla.", texts);
        Assert.Contains("Taquilla fora de servei: avariada.", texts);
        Assert.Contains("Canvi de tipus de fora de servei: de avariada a en manteniment.", texts);
        Assert.Contains("Taquilla de nou en servei (s'ha resolt: en manteniment).", texts);
        Assert.Contains("Taquilla donada de baixa.", texts);
    }

    [Fact]
    [Trait("spec", Spec + ": Historial de eventos de la taquilla (Historial de una baja con número reutilizado)")]
    public async Task A_retired_and_an_active_locker_with_the_same_number_show_only_their_own_events()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var old = await world.LockerAsync(15, zone);
        await world.RetireLocker.HandleAsync(new RetireLockerRequest(old), default);
        var current = await world.LockerAsync(15, zone);
        await world.ReserveLocker.HandleAsync(new ReserveLockerRequest(current), default);

        var oldHistory = (await world.History.HandleAsync(new GetLockerHistoryRequest(old), default)).Value!;
        var currentHistory = (await world.History.HandleAsync(new GetLockerHistoryRequest(current), default)).Value!;

        Assert.Equal(2, oldHistory.Count);
        Assert.Equal(2, currentHistory.Count);
        Assert.DoesNotContain(oldHistory, e => e.Type == LockerEventTypes.Reserved);
        Assert.DoesNotContain(currentHistory, e => e.Type == LockerEventTypes.Retired);
    }

    [Fact]
    [Trait("spec", Spec + ": Historial de eventos de la taquilla (Historial inmutable)")]
    public void The_event_repository_offers_no_way_to_change_or_delete_an_event()
    {
        var operations = typeof(ILockerEventRepository).GetMethods().Select(m => m.Name).ToList();

        Assert.Equal(["AddAsync", "ListAsync"], operations.Order());
    }

    [Fact]
    [Trait("spec", Spec + ": Historial de eventos de la taquilla (Consulta del historial)")]
    public void Every_locker_event_type_and_kind_has_a_catalan_text()
    {
        var localizer = new ResxLocalizer();

        Assert.All(LockerEventTypes.All, type => Assert.NotEqual("History." + type, localizer.Get("History." + type)));
        Assert.NotEqual("History.Locker.ReservedWithNote", localizer.Get("History.Locker.ReservedWithNote"));
        Assert.NotEqual("History.Locker.OutOfServiceKeeping", localizer.Get("History.Locker.OutOfServiceKeeping"));
        Assert.NotEqual("History.Kind.Broken", localizer.Get("History.Kind.Broken"));
        Assert.NotEqual("History.Kind.Maintenance", localizer.Get("History.Kind.Maintenance"));
        Assert.NotEqual("History.Unknown", localizer.Get("History.Unknown"));
    }

    [Fact]
    [Trait("spec", Spec + ": Historial de eventos de la taquilla (Consulta del historial)")]
    public async Task An_event_of_an_unknown_type_still_shows_something_readable_and_an_unknown_locker_is_reported()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        await world.Store.Events.AddAsync(new Arca.Domain.Common.HistoryEvent(locker, "Assignment.Opened", world.Clock.UtcNow.AddDays(1), null, null), default);

        var history = await world.History.HandleAsync(new GetLockerHistoryRequest(locker), default);
        var missing = await world.History.HandleAsync(new GetLockerHistoryRequest(Guid.NewGuid()), default);

        Assert.Equal("Esdeveniment no reconegut (Assignment.Opened).", history.Value![0].Text);
        Assert.Equal("Lockers.NotFound", missing.Error!.Code);
    }

    // --- Listing and filters ---

    [Fact]
    [Trait("spec", Spec + ": Consulta y filtros (Listado por defecto)")]
    public async Task By_default_the_list_has_the_active_lockers_in_numeric_order_and_no_retired_ones()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        await world.LockerAsync(10, zone);
        await world.LockerAsync(2, zone);
        var retired = await world.LockerAsync(5, zone);
        await world.RetireLocker.HandleAsync(new RetireLockerRequest(retired), default);

        var listing = await world.ListLockers.HandleAsync(new ListLockersRequest(), default);

        Assert.Equal([2, 10], listing.Value!.Rows.Select(r => r.Number));
    }

    [Fact]
    [Trait("spec", Spec + ": Consulta y filtros (Incluir bajas)")]
    public async Task Including_retired_lockers_puts_the_active_one_before_a_retired_one_with_the_same_number()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var old = await world.LockerAsync(15, zone);
        await world.RetireLocker.HandleAsync(new RetireLockerRequest(old), default);
        var current = await world.LockerAsync(15, zone);

        var listing = await world.ListLockers.HandleAsync(Filter(retired: true), default);

        Assert.Equal([current, old], listing.Value!.Rows.Select(r => r.Id));
        Assert.Equal([false, true], listing.Value.Rows.Select(r => r.IsRetired));
    }

    [Fact]
    [Trait("spec", Spec + ": Consulta y filtros (Filtro combinado)")]
    public async Task Filters_combine_by_zone_and_status()
    {
        var world = new InventoryWorld();
        var first = await world.ZoneAsync("Planta 1");
        var second = await world.ZoneAsync("Gimnàs");
        var brokenHere = await world.LockerAsync(1, first);
        await world.LockerAsync(2, first);
        var brokenThere = await world.LockerAsync(3, second);
        await world.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(brokenHere, OutOfServiceKind.Broken), default);
        await world.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(brokenThere, OutOfServiceKind.Broken), default);

        var listing = await world.ListLockers.HandleAsync(Filter(zone: first, status: LockerStatus.Broken), default);

        Assert.Equal([brokenHere], listing.Value!.Rows.Select(r => r.Id));
    }

    [Fact]
    [Trait("spec", Spec + ": Consulta y filtros (Búsqueda por número)")]
    public async Task Searching_a_number_finds_the_active_locker_and_the_retired_ones_only_when_asked()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var old = await world.LockerAsync(15, zone);
        await world.RetireLocker.HandleAsync(new RetireLockerRequest(old), default);
        var current = await world.LockerAsync(15, zone);
        await world.LockerAsync(16, zone);

        var active = await world.ListLockers.HandleAsync(Filter(number: 15), default);
        var withRetired = await world.ListLockers.HandleAsync(Filter(number: 15, retired: true), default);

        Assert.Equal([current], active.Value!.Rows.Select(r => r.Id));
        Assert.Equal([current, old], withRetired.Value!.Rows.Select(r => r.Id));
    }

    [Fact]
    [Trait("spec", Spec + ": Consulta y filtros (Contadores)")]
    public async Task The_counters_give_the_active_total_and_each_status_in_total_and_by_zone()
    {
        var world = new InventoryWorld();
        var first = await world.ZoneAsync("Planta 1");
        var second = await world.ZoneAsync("Gimnàs");
        await world.LockerAsync(1, first);
        var occupied = await world.LockerAsync(2, first);
        var reserved = await world.LockerAsync(3, first);
        var broken = await world.LockerAsync(4, second);
        var maintenance = await world.LockerAsync(5, second);
        var retired = await world.LockerAsync(6, second);
        world.Store.Occupancy.Occupy(occupied);
        await world.ReserveLocker.HandleAsync(new ReserveLockerRequest(reserved), default);
        await world.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(broken, OutOfServiceKind.Broken), default);
        await world.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(maintenance, OutOfServiceKind.Maintenance), default);
        await world.RetireLocker.HandleAsync(new RetireLockerRequest(retired), default);

        var listing = (await world.ListLockers.HandleAsync(Filter(zone: first), default)).Value!;

        Assert.Equal(new LockerCounters(Active: 5, Free: 1, Occupied: 1, Broken: 1, Maintenance: 1, Reserved: 1), listing.Total); // whatever the filter
        Assert.Equal(["Gimnàs", "Planta 1"], listing.ByZone.Select(z => z.ZoneName));
        Assert.Equal(new LockerCounters(2, 0, 0, 1, 1, 0), listing.ByZone[0].Counters);
        Assert.Equal(new LockerCounters(3, 1, 1, 0, 0, 1), listing.ByZone[1].Counters);
    }

    [Fact]
    [Trait("spec", Spec + ": Consulta y filtros (Sin resultados)")]
    public async Task A_filter_with_no_match_gives_an_empty_list_and_no_error()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        await world.LockerAsync(1, zone);

        var listing = await world.ListLockers.HandleAsync(Filter(number: 999), default);
        var empty = await new InventoryWorld().ListLockers.HandleAsync(new ListLockersRequest(), default);

        Assert.True(listing.IsSuccess);
        Assert.Empty(listing.Value!.Rows);
        Assert.Empty(empty.Value!.Rows);
        Assert.Equal(0, empty.Value.Total.Active);
    }

    [Fact]
    [Trait("spec", Spec + ": Estado visible derivado (Taquilla averiada con alumno)")]
    public async Task The_list_shows_the_status_derived_from_the_occupancy_and_the_other_facts()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        world.Store.Occupancy.Occupy(locker);
        await world.MarkOutOfService.HandleAsync(
            new MarkLockerOutOfServiceRequest(locker, OutOfServiceKind.Broken, OutOfServiceDecision.Keep), default);

        var row = (await world.ListLockers.HandleAsync(Filter(), default)).Value!.Rows.Single();

        Assert.Equal(LockerStatus.Broken, row.Status);
        Assert.True(row.HasAssignment);
    }
}
