// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Lockers;
using Arca.Application.Lockers.CreateLockerRange;
using Arca.Application.Lockers.GetLocker;
using Arca.Application.Lockers.ListLockers;
using Arca.Application.Lockers.RetireLocker;
using Arca.Application.Zones.DeactivateZone;
using Arca.Testing;
using Xunit;

namespace Arca.Application.Tests.Inventory;

public sealed class InventoryFeedbackTests
{
    const string Spec = "taquilles-i-zones/taquilles";

    // --- Results with counts and texts ---

    [Fact]
    [Trait("spec", Spec + ": Alta por rangos (Rango correcto)")]
    public async Task Confirming_a_range_says_how_many_lockers_were_created_and_where()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var plan = (await world.CreateRange.AnalyzeAsync(new CreateLockerRangeRequest(1, 40, zone), null, default)).Value!;

        var result = (await world.CreateRange.ApplyAsync(plan, null, default)).Value!;

        Assert.Equal(40, result.Created);
        Assert.Equal("S'han creat 40 taquilles a la zona Planta 1.", new InventoryResultTexts(world.Localizer).RangeCreated(result));
    }

    [Fact]
    [Trait("spec", Spec + ": Alta por rangos (Conflicto con números existentes)")]
    public async Task A_range_that_created_nothing_says_so()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        await world.LockerAsync(3, zone);
        var plan = (await world.CreateRange.AnalyzeAsync(new CreateLockerRangeRequest(1, 5, zone), null, default)).Value!;

        var result = (await world.CreateRange.ApplyAsync(plan, null, default)).Value!;

        Assert.Equal("No s'ha creat cap taquilla.", new InventoryResultTexts(world.Localizer).RangeCreated(result));
    }

    [Fact]
    [Trait("spec", Spec + ": Alta individual de una taquilla (Alta correcta)")]
    public async Task Every_success_has_a_visible_message_in_catalan()
    {
        var world = new InventoryWorld();
        var texts = new InventoryResultTexts(world.Localizer);
        var zone = (await world.CreateZone.HandleAsync(new("Planta 1"), default)).Value!;
        var row = await world.RowAsync(await world.LockerAsync(7, zone.Id));

        Assert.Equal("S'ha creat la zona «Planta 1».", texts.ZoneCreated(zone));
        Assert.Equal("S'ha creat la taquilla 7 a la zona Planta 1.", texts.LockerAdded(row));
        Assert.Equal("S'ha reservat la taquilla 7.", texts.LockerReserved(row));
        Assert.Equal("S'ha tret la reserva de la taquilla 7.", texts.ReservationRemoved(row));
        Assert.Equal("La taquilla 7 ha quedat fora de servei.", texts.OutOfService(row));
        Assert.Equal("La taquilla 7 torna a estar en servei.", texts.ServiceRestored(row));
        Assert.Equal("La taquilla ara té el número 7.", texts.NumberChanged(row));
        Assert.Equal("La taquilla 7 és ara a la zona Planta 1.", texts.ZoneChanged(row));
        Assert.Equal("S'ha donat de baixa la taquilla 7.", texts.Retired(row));
        Assert.Equal("La zona ara es diu «Planta 1».", texts.ZoneRenamed(zone));
        Assert.Equal("S'ha desactivat la zona «Planta 1».", texts.ZoneDeactivated(zone));
        Assert.Equal("S'ha reactivat la zona «Planta 1».", texts.ZoneReactivated(zone));
        Assert.Equal("S'ha eliminat la zona.", texts.ZoneDeleted());
    }

    // --- Confirmations ---

    [Fact]
    [Trait("spec", Spec + ": Confirmación de baja y de alta por rangos (Confirmar la baja)")]
    public async Task Retiring_asks_for_confirmation_saying_it_cannot_be_undone()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var row = await world.RowAsync(await world.LockerAsync(15, zone));

        var request = new LockerConfirmations(world.Localizer).ForRetire(row);

        Assert.Equal("Donar de baixa la taquilla 15?", request.Title);
        Assert.Contains("no es pot desfer", request.Consequence, StringComparison.Ordinal);
        Assert.Contains("Planta 1", request.Consequence, StringComparison.Ordinal);
        Assert.True(request.Destructive);
    }

    [Fact]
    [Trait("spec", Spec + ": Confirmación de baja y de alta por rangos (Confirmar un rango)")]
    public async Task Confirming_a_range_of_40_says_that_40_lockers_will_be_created()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var plan = (await world.CreateRange.AnalyzeAsync(new CreateLockerRangeRequest(1, 40, zone), null, default)).Value!;

        var request = new LockerConfirmations(world.Localizer).ForRange(plan);

        Assert.Equal("Es crearan 40 taquilles noves lliures a la zona Planta 1.", request.Consequence);
        Assert.Equal("Crea 40 taquilles", request.ConfirmLabel);
        Assert.Equal("Del número 1 al 40", Assert.Single(request.Details!));
        Assert.False(request.Destructive);
    }

    [Fact]
    [Trait("spec", Spec + ": Confirmación de baja y de alta por rangos (Confirmar la baja)")]
    public async Task A_rejected_confirmation_leaves_the_data_untouched_and_a_given_one_applies()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(15, zone);
        var plan = (await world.CreateRange.AnalyzeAsync(new CreateLockerRangeRequest(100, 139, zone), null, default)).Value!;
        var confirmations = new LockerConfirmations(world.Localizer);
        var no = new RecordingConfirmations(answer: false);
        var yes = new RecordingConfirmations(answer: true);

        // What a screen does: ask first, call the use case only if the person confirms.
        if (await no.ConfirmAsync(confirmations.ForRetire(await world.RowAsync(locker))))
        {
            await world.RetireLocker.HandleAsync(new RetireLockerRequest(locker), default);
        }

        if (await no.ConfirmAsync(confirmations.ForRange(plan)))
        {
            await world.CreateRange.ApplyAsync(plan, null, default);
        }

        Assert.Equal(2, no.Asked.Count);
        Assert.Single(world.Store.LockerList);
        Assert.False(world.Store.LockerList.Single().IsRetired);

        if (await yes.ConfirmAsync(confirmations.ForRange(plan)))
        {
            await world.CreateRange.ApplyAsync(plan, null, default);
        }

        Assert.Equal(41, world.Store.LockerList.Count);
    }

    // --- Empty states ---

    [Fact]
    [Trait("spec", Spec + ": Estados vacíos del inventario (Sin zonas)")]
    public async Task With_no_zones_the_inventory_says_to_create_one_first()
    {
        var world = new InventoryWorld();

        var listing = (await world.ListLockers.HandleAsync(new ListLockersRequest(), default)).Value!;
        var guide = LockerEmptyStates.Describe(listing.EmptyState, world.Localizer)!;

        Assert.Equal(LockerEmptyState.NoZones, listing.EmptyState);
        Assert.Contains("Crea una zona primer", guide.Message, StringComparison.Ordinal);
        Assert.Equal([SuggestedAction.CreateZone], guide.Actions);
        Assert.Equal("Crea una zona", LockerEmptyStates.Label(SuggestedAction.CreateZone, world.Localizer));
    }

    [Fact]
    [Trait("spec", Spec + ": Estados vacíos del inventario (Sin zonas)")]
    public async Task With_only_deactivated_zones_it_is_still_the_no_zones_state()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Antiga");
        await world.DeactivateZone.HandleAsync(new DeactivateZoneRequest(zone), default);

        var listing = (await world.ListLockers.HandleAsync(new ListLockersRequest(), default)).Value!;

        Assert.Equal(LockerEmptyState.NoZones, listing.EmptyState);
    }

    [Fact]
    [Trait("spec", Spec + ": Estados vacíos del inventario (Sin taquillas)")]
    public async Task With_zones_but_no_active_lockers_it_offers_ranges_and_individual_creation_and_no_import()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var old = await world.LockerAsync(1, zone);
        await world.RetireLocker.HandleAsync(new RetireLockerRequest(old), default); // only a retired one is left

        var listing = (await world.ListLockers.HandleAsync(new ListLockersRequest(), default)).Value!;
        var guide = LockerEmptyStates.Describe(listing.EmptyState, world.Localizer)!;

        Assert.Equal(LockerEmptyState.NoLockers, listing.EmptyState);
        Assert.Equal([SuggestedAction.CreateRange, SuggestedAction.AddLocker], guide.Actions);
        Assert.DoesNotContain("import", guide.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("spec", Spec + ": Estados vacíos del inventario (Filtros sin resultados)")]
    public async Task Filters_with_no_result_offer_clearing_them()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        await world.LockerAsync(1, zone);

        var listing = (await world.ListLockers.HandleAsync(new ListLockersRequest(new LockerFilter(Number: 999)), default)).Value!;
        var guide = LockerEmptyStates.Describe(listing.EmptyState, world.Localizer)!;

        Assert.Equal(LockerEmptyState.NoResults, listing.EmptyState);
        Assert.Equal([SuggestedAction.ClearFilters], guide.Actions);
        Assert.Equal("Neteja els filtres", LockerEmptyStates.Label(SuggestedAction.ClearFilters, world.Localizer));
    }

    [Fact]
    [Trait("spec", Spec + ": Estados vacíos del inventario (Filtros sin resultados)")]
    public async Task A_list_with_lockers_has_no_empty_state_and_no_guide()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        await world.LockerAsync(1, zone);

        var listing = (await world.ListLockers.HandleAsync(new ListLockersRequest(), default)).Value!;

        Assert.Equal(LockerEmptyState.None, listing.EmptyState);
        Assert.Null(LockerEmptyStates.Describe(listing.EmptyState, world.Localizer));
    }

    // --- Detail on demand ---

    [Fact]
    [Trait("spec", Spec + ": Historial de eventos de la taquilla (Consulta del historial)")]
    public async Task The_detail_of_a_locker_is_loaded_when_asked_and_an_unknown_one_is_reported()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(15, zone, "prop de la porta");
        var detail = new GetLockerHandler(world.Store.Lockers, world.Store.Zones, world.Store.Occupancy);

        var found = await detail.HandleAsync(new GetLockerRequest(locker), default);
        var missing = await detail.HandleAsync(new GetLockerRequest(Guid.NewGuid()), default);

        Assert.Equal(15, found.Value!.Number);
        Assert.Equal("prop de la porta", found.Value.Note);
        Assert.Equal("Planta 1", found.Value.ZoneName);
        Assert.Equal("Lockers.NotFound", missing.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Consulta y filtros (Sin resultados)")]
    public void The_lists_and_details_return_plain_data_without_lazy_references()
    {
        // A row and a listing hold only values and other plain rows: nothing that loads more when touched.
        var types = new[] { typeof(LockerRow), typeof(LockerListing), typeof(LockerCounters), typeof(ZoneCounters) };

        Assert.All(types.SelectMany(t => t.GetProperties()), p =>
            Assert.False(typeof(Delegate).IsAssignableFrom(p.PropertyType) || typeof(Task).IsAssignableFrom(p.PropertyType), p.Name));
    }
}
