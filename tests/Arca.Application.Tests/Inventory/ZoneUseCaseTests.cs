// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Zones.CreateZone;
using Arca.Application.Zones.DeactivateZone;
using Arca.Application.Zones.DeleteZone;
using Arca.Application.Zones.ListZones;
using Arca.Application.Zones.ReactivateZone;
using Arca.Application.Zones.RenameZone;
using Arca.Application.Lockers.RetireLocker;
using Xunit;

namespace Arca.Application.Tests.Inventory;

public sealed class ZoneUseCaseTests
{
    const string Spec = "taquilles-i-zones/zones";

    [Fact]
    [Trait("spec", Spec + ": Crear zonas (Zona nueva)")]
    public async Task Creating_a_zone_saves_it_active_with_no_lockers()
    {
        var world = new InventoryWorld();

        var result = await world.CreateZone.HandleAsync(new CreateZoneRequest("  Planta 1  "), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Planta 1", result.Value!.Name);
        Assert.True(result.Value.IsActive);
        Assert.Equal(0, result.Value.ActiveLockers);
        Assert.Single(world.Store.ZoneList);
    }

    [Fact]
    [Trait("spec", Spec + ": Nombre de zona único (Nombre duplicado)")]
    public async Task A_duplicate_name_is_refused_and_saves_nothing()
    {
        var world = new InventoryWorld();
        await world.ZoneAsync("Gimnàs");

        var result = await world.CreateZone.HandleAsync(new CreateZoneRequest("gimnas"), default);

        Assert.Equal("Zones.NameDuplicate", result.Error!.Code);
        Assert.Single(world.Store.ZoneList);
    }

    [Fact]
    [Trait("spec", Spec + ": Renombrar zonas (Renombrado correcto)")]
    public async Task Renaming_shows_the_new_name_on_every_locker_of_the_zone()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);

        var result = await world.RenameZone.HandleAsync(new RenameZoneRequest(zone, "Primera planta"), default);

        Assert.Equal("Primera planta", result.Value!.Name);
        Assert.Equal(1, result.Value.ActiveLockers);
        Assert.Equal("Primera planta", (await world.RowAsync(locker)).ZoneName);
    }

    [Fact]
    [Trait("spec", Spec + ": Renombrar zonas (Renombrar con el mismo nombre)")]
    public async Task Renaming_a_zone_changing_only_the_case_is_accepted()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");

        Assert.True((await world.RenameZone.HandleAsync(new RenameZoneRequest(zone, "PLANTA 1"), default)).IsSuccess);
    }

    [Fact]
    [Trait("spec", Spec + ": Renombrar zonas (Renombrar a un nombre existente)")]
    public async Task Renaming_to_the_name_of_another_zone_is_refused()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        await world.ZoneAsync("Gimnàs");

        var result = await world.RenameZone.HandleAsync(new RenameZoneRequest(zone, "gimnas"), default);

        Assert.Equal("Zones.NameDuplicate", result.Error!.Code);
        Assert.Equal("Planta 1", world.Store.ZoneList.Single(z => z.Id == zone).Name);
    }

    [Fact]
    [Trait("spec", Spec + ": Renombrar zonas (Renombrado correcto)")]
    public async Task An_unknown_zone_is_reported_for_every_change()
    {
        var world = new InventoryWorld();
        var missing = Guid.NewGuid();

        Assert.Equal("Zones.NotFound", (await world.RenameZone.HandleAsync(new RenameZoneRequest(missing, "x"), default)).Error!.Code);
        Assert.Equal("Zones.NotFound", (await world.DeactivateZone.HandleAsync(new DeactivateZoneRequest(missing), default)).Error!.Code);
        Assert.Equal("Zones.NotFound", (await world.ReactivateZone.HandleAsync(new ReactivateZoneRequest(missing), default)).Error!.Code);
        Assert.Equal("Zones.NotFound", (await world.DeleteZone.HandleAsync(new DeleteZoneRequest(missing), default)).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Desactivar y reactivar zonas (Desactivar zona sin taquillas activas)")]
    public async Task A_zone_whose_lockers_are_all_retired_can_be_deactivated_and_reactivated()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        await world.RetireLocker.HandleAsync(new RetireLockerRequest(locker), default);

        var deactivated = await world.DeactivateZone.HandleAsync(new DeactivateZoneRequest(zone), default);
        var reactivated = await world.ReactivateZone.HandleAsync(new ReactivateZoneRequest(zone), default);

        Assert.False(deactivated.Value!.IsActive);
        Assert.True(reactivated.Value!.IsActive);
    }

    [Fact]
    [Trait("spec", Spec + ": Desactivar y reactivar zonas (Desactivar zona con taquillas activas)")]
    public async Task A_zone_with_an_active_locker_cannot_be_deactivated()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        await world.LockerAsync(1, zone);

        var result = await world.DeactivateZone.HandleAsync(new DeactivateZoneRequest(zone), default);

        Assert.Equal("Zones.HasActiveLockers", result.Error!.Code);
        Assert.True(world.Store.ZoneList.Single().IsActive);
    }

    [Fact]
    [Trait("spec", Spec + ": Conservación de zonas con historial (Eliminar zona sin uso)")]
    public async Task A_zone_that_never_had_lockers_can_be_deleted()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Nova");

        var result = await world.DeleteZone.HandleAsync(new DeleteZoneRequest(zone), default);

        Assert.True(result.IsSuccess);
        Assert.Empty(world.Store.ZoneList);
    }

    [Fact]
    [Trait("spec", Spec + ": Conservación de zonas con historial (Eliminar zona con historial)")]
    public async Task A_zone_that_had_a_locker_even_retired_cannot_be_deleted()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        await world.RetireLocker.HandleAsync(new RetireLockerRequest(locker), default);

        var result = await world.DeleteZone.HandleAsync(new DeleteZoneRequest(zone), default);

        Assert.Equal("Zones.HasHistory", result.Error!.Code);
        Assert.Single(world.Store.ZoneList);
    }

    [Fact]
    [Trait("spec", Spec + ": Listado de zonas (Listado por defecto)")]
    public async Task The_list_is_in_catalan_order_and_hides_deactivated_zones_unless_asked()
    {
        var world = new InventoryWorld();
        await world.ZoneAsync("Planta 2");
        await world.ZoneAsync("Cambra");
        await world.ZoneAsync("Çafareig"); // ç sorts with c in Catalan, not after z
        var old = await world.ZoneAsync("Antiga");
        await world.DeactivateZone.HandleAsync(new DeactivateZoneRequest(old), default);

        var active = await world.ListZones.HandleAsync(new ListZonesRequest(), default);
        var all = await world.ListZones.HandleAsync(new ListZonesRequest(IncludeInactive: true), default);

        Assert.Equal(["Çafareig", "Cambra", "Planta 2"], active.Value!.Select(z => z.Name));
        Assert.Equal(["Antiga", "Çafareig", "Cambra", "Planta 2"], all.Value!.Select(z => z.Name));
    }

    [Fact]
    [Trait("spec", Spec + ": Listado de zonas (Recuento de taquillas)")]
    public async Task The_count_is_of_active_lockers_and_ignores_retired_ones()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var lockers = new List<Guid>();
        for (var number = 1; number <= 45; number++)
        {
            lockers.Add(await world.LockerAsync(number, zone));
        }

        foreach (var locker in lockers.Take(5))
        {
            await world.RetireLocker.HandleAsync(new RetireLockerRequest(locker), default);
        }

        var listed = await world.ListZones.HandleAsync(new ListZonesRequest(), default);

        Assert.Equal(40, Assert.Single(listed.Value!).ActiveLockers);
    }
}
