// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Diagnostics;
using Arca.Application.Lockers;
using Arca.Application.Lockers.CreateLockerRange;
using Arca.Application.Lockers.ListLockers;
using Arca.Application.Storage;
using Arca.Application.Zones.CreateZone;
using Arca.Domain.Lockers;
using Arca.Infrastructure.Inventory;
using Arca.Infrastructure.Storage;
using Arca.Infrastructure.Tests.Storage;
using Arca.Testing;
using Arca.Testing.Inventory;
using Xunit;

namespace Arca.Infrastructure.Tests.Inventory;

public sealed class InventoryVolumeTests
{
    [Fact]
    [Trait("spec", "taquilles-i-zones/taquilles: Consulta y filtros (Listado por defecto)")]
    public async Task With_1000_lockers_the_filtered_list_answers_in_well_under_a_second_and_gives_the_right_counts()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        var key = TestKeys.FromSeed("volume");
        var created = await ArcaDatabase.CreateAsync(path, key);
        await created.Value!.DisposeAsync();
        var store = new EfInventory(() => new ArcaDbContext(path, key));
        var clock = new FakeClock(new DateTimeOffset(2026, 9, 25, 9, 0, 0, TimeSpan.Zero));
        var occupancy = new ConfigurableOccupancy();

        var first = (await new CreateZoneHandler(store.Zones, store).HandleAsync(new CreateZoneRequest("Planta 1"), default)).Value!.Id;
        var second = (await new CreateZoneHandler(store.Zones, store).HandleAsync(new CreateZoneRequest("Planta 2"), default)).Value!.Id;
        var range = new CreateLockerRangeHandler(store.Lockers, store.Zones, store.Events, store, clock);
        foreach (var (from, to, zone) in new[] { (1, 500, first), (501, 1000, second) })
        {
            var plan = (await range.AnalyzeAsync(new CreateLockerRangeRequest(from, to, zone), null, default)).Value!;
            Assert.True((await range.ApplyAsync(plan, null, default)).Value!.Applied);
        }

        var all = await store.Lockers.ListAsync(includeRetired: false, default);
        foreach (var locker in all.Where(l => l.Number % 3 == 0))
        {
            occupancy.Occupy(locker.Id);
        }

        var handler = new ListLockersHandler(store.Lockers, store.Zones, occupancy);
        await handler.HandleAsync(new ListLockersRequest(), default); // warm up the connection and the query plan

        var clockTime = Stopwatch.StartNew();
        var everything = (await handler.HandleAsync(new ListLockersRequest(), default)).Value!;
        var filtered = (await handler.HandleAsync(new ListLockersRequest(new LockerFilter(ZoneId: second, Status: LockerStatus.Occupied)), default)).Value!;
        var searched = (await handler.HandleAsync(new ListLockersRequest(new LockerFilter(Number: 777)), default)).Value!;
        clockTime.Stop();

        Assert.Equal(1000, everything.Rows.Count);
        Assert.Equal(new LockerCounters(1000, 667, 333, 0, 0, 0), everything.Total);
        Assert.Equal(Enumerable.Range(1, 1000), everything.Rows.Select(r => r.Number));
        Assert.Equal(Enumerable.Range(501, 500).Where(n => n % 3 == 0), filtered.Rows.Select(r => r.Number));
        Assert.Equal(777, Assert.Single(searched.Rows).Number);
        Assert.True(clockTime.Elapsed < TimeSpan.FromSeconds(1.5), $"three queries over 1000 lockers took {clockTime.Elapsed.TotalSeconds:0.00} s");
    }
}
