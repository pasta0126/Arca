// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Lockers;
using Arca.Application.Lockers.AddLocker;
using Arca.Application.Lockers.CreateLockerRange;
using Arca.Application.Lockers.GetLockerHistory;
using Arca.Application.Lockers.ListLockers;
using Arca.Application.Lockers.MarkLockerOutOfService;
using Arca.Application.Lockers.ReserveLocker;
using Arca.Application.Lockers.RetireLocker;
using Arca.Application.Localization;
using Arca.Application.Storage;
using Arca.Application.Zones.CreateZone;
using Arca.Application.Zones.DeleteZone;
using Arca.Application.Zones.ListZones;
using Arca.Domain.Common;
using Arca.Domain.Lockers;
using Arca.Domain.Zones;
using Arca.Infrastructure.Inventory;
using Arca.Infrastructure.Storage;
using Arca.Infrastructure.Tests.Storage;
using Arca.Testing;
using Arca.Testing.Inventory;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Arca.Infrastructure.Tests.Inventory;

/// <summary>The zones, lockers and history over a real encrypted database: indexes, transactions and the use cases end to end.</summary>
public sealed class EfInventoryTests : IDisposable
{
    const string Spec = "taquilles-i-zones";

    readonly TempDirectory _dir = new();
    readonly DatabaseKey _key = TestKeys.FromSeed("inventory");
    readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 25, 9, 0, 0, TimeSpan.Zero));
    readonly ConfigurableOccupancy _occupancy = new();
    string? _path;

    public void Dispose() => _dir.Dispose();

    string Path => _path ??= _dir.File("arca.db");

    async Task<EfInventory> NewDatabaseAsync()
    {
        var created = await ArcaDatabase.CreateAsync(Path, _key);
        Assert.True(created.IsSuccess);
        await created.Value!.DisposeAsync();
        return Open();
    }

    EfInventory Open() => new(() => new ArcaDbContext(Path, _key));

    ArcaDbContext Raw() => new(Path, _key);

    /// <summary>How many events are stored, optionally of one type, read straight from the table.</summary>
    static async Task<int> EventsAsync(ArcaDbContext context, string? type = null) =>
        (await context.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS Value FROM LockerEvents WHERE {0} IS NULL OR Type = {0}", type ?? (object)DBNull.Value).ToListAsync()).Single();

    async Task<Guid> ZoneAsync(EfInventory store, string name) =>
        (await new CreateZoneHandler(store.Zones, store).HandleAsync(new CreateZoneRequest(name), default)).Value!.Id;

    async Task<Guid> LockerAsync(EfInventory store, int number, Guid zone)
    {
        _clock.Advance(TimeSpan.FromMinutes(1));
        var added = await new AddLockerHandler(store.Lockers, store.Zones, store.Events, store, _clock)
            .HandleAsync(new AddLockerRequest(number, zone), default);
        return added.Value!.Id;
    }

    RetireLockerHandler Retire(EfInventory store, params ILockerRetiredHandler[] hooks) =>
        new(store.Lockers, store.Zones, store.Events, _occupancy, hooks, store, _clock);

    // --- Schema ---

    [Fact]
    [Trait("spec", Spec + "/taquilles: Identidad de la taquilla y número visible (Historial ligado a la identidad)")]
    public async Task The_migration_creates_the_tables_and_the_model_has_no_changes_left_to_migrate()
    {
        await NewDatabaseAsync();
        await using var context = Raw();

        var tables = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'").ToListAsync();

        Assert.Contains("Zones", tables);
        Assert.Contains("Lockers", tables);
        Assert.Contains("LockerEvents", tables);
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    [Trait("spec", Spec + "/taquilles: Estado visible derivado (Taquilla sin hechos)")]
    public async Task No_status_column_is_stored_only_the_facts_it_is_derived_from()
    {
        await NewDatabaseAsync();
        await using var context = Raw();

        var columns = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('Lockers')").ToListAsync();

        Assert.DoesNotContain(columns, c => c.Contains("Status", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(
            ["Id", "IsReserved", "Note", "Number", "OutOfService", "ReservationNote", "RetiredAtUtc", "ZoneId"], columns.Order());
    }

    // --- Unique indexes ---

    [Fact]
    [Trait("spec", Spec + "/taquilles: Número único entre taquillas activas (Número duplicado entre activas)")]
    public async Task The_index_rejects_two_active_lockers_with_the_same_number_even_without_the_domain_rule()
    {
        await NewDatabaseAsync();
        await using var context = Raw();
        var zone = new Zone(Guid.NewGuid(), "Planta 1", TextComparer.Key("Planta 1"), true);
        context.Add(zone);
        context.Add(new Locker(Guid.NewGuid(), 15, zone.Id, null, null, false, null, null));
        await context.SaveChangesAsync();
        context.Add(new Locker(Guid.NewGuid(), 15, zone.Id, null, null, false, null, null));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    [Trait("spec", Spec + "/taquilles: Número único entre taquillas activas (Varias bajas con el mismo número)")]
    public async Task A_retired_locker_can_share_its_number_with_an_active_one_and_with_other_retired_ones()
    {
        await NewDatabaseAsync();
        await using var context = Raw();
        var zone = new Zone(Guid.NewGuid(), "Planta 1", TextComparer.Key("Planta 1"), true);
        context.Add(zone);
        var retired = _clock.UtcNow;
        context.Add(new Locker(Guid.NewGuid(), 15, zone.Id, null, null, false, null, retired));
        context.Add(new Locker(Guid.NewGuid(), 15, zone.Id, null, null, false, null, retired.AddDays(1)));
        context.Add(new Locker(Guid.NewGuid(), 15, zone.Id, null, null, false, null, null));

        await context.SaveChangesAsync();

        Assert.Equal(3, await context.Set<Locker>().CountAsync(l => l.Number == 15));
    }

    [Fact]
    [Trait("spec", Spec + "/zones: Nombre de zona único (Nombre duplicado)")]
    public async Task The_index_rejects_two_zones_with_the_same_normalised_name()
    {
        await NewDatabaseAsync();
        await using var context = Raw();
        context.Add(new Zone(Guid.NewGuid(), "Gimnàs", TextComparer.Key("Gimnàs"), true));
        context.Add(new Zone(Guid.NewGuid(), "gimnas", TextComparer.Key("gimnas"), false));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    [Trait("spec", Spec + "/zones: Conservación de zonas con historial (Eliminar zona con historial)")]
    public async Task A_zone_with_lockers_cannot_be_deleted_even_by_hand_and_a_locker_needs_a_zone()
    {
        var store = await NewDatabaseAsync();
        var zone = await ZoneAsync(store, "Planta 1");
        await LockerAsync(store, 1, zone);
        await using var context = Raw();

        context.Set<Zone>().Remove(await context.Set<Zone>().SingleAsync());
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        await using var other = Raw();
        other.Add(new Locker(Guid.NewGuid(), 2, Guid.NewGuid(), null, null, false, null, null));
        await Assert.ThrowsAsync<DbUpdateException>(() => other.SaveChangesAsync());
    }

    // --- Use cases end to end ---

    [Fact]
    [Trait("spec", Spec + "/taquilles: Alta individual de una taquilla (Alta correcta)")]
    public async Task What_the_use_cases_save_is_still_there_after_closing_and_opening_the_database_again()
    {
        var store = await NewDatabaseAsync();
        var zone = await ZoneAsync(store, "Planta 1");
        var locker = await LockerAsync(store, 101, zone);
        await new ReserveLockerHandler(store.Lockers, store.Zones, store.Events, _occupancy, store, _clock)
            .HandleAsync(new ReserveLockerRequest(locker, "Professorat"), default);
        SqliteConnection.ClearAllPools();

        var reopened = Open();
        var listing = (await new ListLockersHandler(reopened.Lockers, reopened.Zones, _occupancy).HandleAsync(new ListLockersRequest(), default)).Value!;

        var row = Assert.Single(listing.Rows);
        Assert.Equal(101, row.Number);
        Assert.Equal("Planta 1", row.ZoneName);
        Assert.Equal(LockerStatus.Reserved, row.Status);
        Assert.Equal("Professorat", row.ReservationNote);
    }

    [Fact]
    [Trait("spec", Spec + "/taquilles: Historial de eventos de la taquilla (Consulta del historial)")]
    public async Task The_history_is_stored_with_its_instants_and_comes_back_most_recent_first_in_catalan()
    {
        var store = await NewDatabaseAsync();
        var zone = await ZoneAsync(store, "Planta 1");
        var locker = await LockerAsync(store, 15, zone);
        _clock.Advance(TimeSpan.FromMinutes(5));
        await new MarkLockerOutOfServiceHandler(store.Lockers, store.Zones, store.Events, _occupancy, store, _clock)
            .HandleAsync(new MarkLockerOutOfServiceRequest(locker, OutOfServiceKind.Broken), default);

        var history = (await new GetLockerHistoryHandler(store.Lockers, store.Zones, store.Events, new ResxLocalizer())
            .HandleAsync(new GetLockerHistoryRequest(locker), default)).Value!;

        Assert.Equal([LockerEventTypes.OutOfService, LockerEventTypes.Created], history.Select(e => e.Type));
        Assert.Equal("Taquilla fora de servei: avariada.", history[0].Text);
        Assert.Equal(_clock.UtcNow, history[0].At);
        Assert.Equal(TimeSpan.Zero, history[0].At.Offset);
    }

    [Fact]
    [Trait("spec", Spec + "/taquilles: Estado visible derivado (Taquilla averiada con alumno)")]
    public async Task The_status_is_derived_with_the_occupancy_and_the_counters_come_from_the_database()
    {
        var store = await NewDatabaseAsync();
        var zone = await ZoneAsync(store, "Planta 1");
        var free = await LockerAsync(store, 1, zone);
        var occupied = await LockerAsync(store, 2, zone);
        _occupancy.Occupy(occupied);
        await new MarkLockerOutOfServiceHandler(store.Lockers, store.Zones, store.Events, _occupancy, store, _clock)
            .HandleAsync(new MarkLockerOutOfServiceRequest(occupied, OutOfServiceKind.Broken, OutOfServiceDecision.Keep), default);

        var listing = (await new ListLockersHandler(store.Lockers, store.Zones, _occupancy).HandleAsync(new ListLockersRequest(), default)).Value!;

        Assert.Equal(LockerStatus.Free, listing.Rows.Single(r => r.Id == free).Status);
        var broken = listing.Rows.Single(r => r.Id == occupied);
        Assert.Equal(LockerStatus.Broken, broken.Status);
        Assert.True(broken.HasAssignment);
        Assert.Equal(new LockerCounters(2, 1, 0, 1, 0, 0), listing.Total);
    }

    [Fact]
    [Trait("spec", Spec + "/taquilles: Número único entre taquillas activas (Reutilizar el número de una baja)")]
    public async Task The_number_of_a_retired_locker_is_reused_and_each_keeps_its_own_history()
    {
        var store = await NewDatabaseAsync();
        var zone = await ZoneAsync(store, "Planta 1");
        var old = await LockerAsync(store, 15, zone);
        await Retire(store).HandleAsync(new RetireLockerRequest(old), default);
        var current = await LockerAsync(store, 15, zone);
        var duplicate = await new AddLockerHandler(store.Lockers, store.Zones, store.Events, store, _clock).HandleAsync(new AddLockerRequest(15, zone), default);

        var history = new GetLockerHistoryHandler(store.Lockers, store.Zones, store.Events, new ResxLocalizer());
        var oldHistory = (await history.HandleAsync(new GetLockerHistoryRequest(old), default)).Value!;
        var currentHistory = (await history.HandleAsync(new GetLockerHistoryRequest(current), default)).Value!;

        Assert.Equal("Lockers.NumberInUse", duplicate.Error!.Code);
        Assert.Equal([LockerEventTypes.Retired, LockerEventTypes.Created], oldHistory.Select(e => e.Type));
        Assert.Equal([LockerEventTypes.Created], currentHistory.Select(e => e.Type));
        await using var context = Raw();
        Assert.Equal(2, await context.Set<Locker>().CountAsync(l => l.Number == 15));
    }

    // --- Atomicity ---

    /// <summary>An event repository that fails after letting some events through, like a disk error in the middle of a save.</summary>
    sealed class FailingEvents(ILockerEventRepository inner, int allowed) : ILockerEventRepository
    {
        int _saved;

        public Task AddAsync(HistoryEvent change, CancellationToken ct)
        {
            if (_saved++ >= allowed)
            {
                throw new IOException("the disk failed while saving");
            }

            return inner.AddAsync(change, ct);
        }

        public Task<IReadOnlyList<HistoryEvent>> ListAsync(Guid lockerId, CancellationToken ct) => inner.ListAsync(lockerId, ct);
    }

    [Fact]
    [Trait("spec", Spec + "/taquilles: Alta por rangos (Fallo durante la creación)")]
    public async Task A_failure_in_the_middle_of_a_range_leaves_no_locker_and_no_event_in_the_database()
    {
        var store = await NewDatabaseAsync();
        var zone = await ZoneAsync(store, "Planta 1");
        var handler = new CreateLockerRangeHandler(store.Lockers, store.Zones, new FailingEvents(store.Events, allowed: 20), store, _clock);
        var plan = (await handler.AnalyzeAsync(new CreateLockerRangeRequest(1, 40, zone), null, default)).Value!;

        await Assert.ThrowsAsync<IOException>(() => handler.ApplyAsync(plan, null, default));

        await using var context = Raw();
        Assert.Equal(0, await context.Set<Locker>().CountAsync());
        Assert.Equal(0, await EventsAsync(context));
    }

    [Fact]
    [Trait("spec", Spec + "/taquilles: Alta por rangos (Rango correcto)")]
    public async Task A_range_creates_all_its_lockers_with_their_events_in_one_transaction()
    {
        var store = await NewDatabaseAsync();
        var zone = await ZoneAsync(store, "Planta 1");
        var handler = new CreateLockerRangeHandler(store.Lockers, store.Zones, store.Events, store, _clock);
        var plan = (await handler.AnalyzeAsync(new CreateLockerRangeRequest(1, 40, zone), null, default)).Value!;

        var result = await handler.ApplyAsync(plan, null, default);

        Assert.Equal(40, result.Value!.Created);
        await using var context = Raw();
        Assert.Equal(40, await context.Set<Locker>().CountAsync());
        Assert.Equal(40, await EventsAsync(context, LockerEventTypes.Created));
    }

    sealed class FailingHook : ILockerRetiredHandler
    {
        public Task HandleAsync(Guid lockerId, DateTimeOffset retiredAtUtc, CancellationToken ct) => throw new InvalidOperationException("hook failed");
    }

    [Fact]
    [Trait("spec", Spec + "/taquilles: Baja definitiva (Baja correcta)")]
    public async Task If_a_retirement_hook_fails_the_retirement_and_its_event_are_undone_in_the_database()
    {
        var store = await NewDatabaseAsync();
        var zone = await ZoneAsync(store, "Planta 1");
        var locker = await LockerAsync(store, 1, zone);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Retire(store, new FailingHook()).HandleAsync(new RetireLockerRequest(locker), default));

        await using var context = Raw();
        Assert.Null((await context.Set<Locker>().SingleAsync()).RetiredAtUtc);
        Assert.Equal(1, await EventsAsync(context));
    }

    [Fact]
    [Trait("spec", Spec + "/taquilles: Baja definitiva (Baja correcta)")]
    public async Task A_retirement_is_saved_with_its_instant_in_utc()
    {
        var store = await NewDatabaseAsync();
        var zone = await ZoneAsync(store, "Planta 1");
        var locker = await LockerAsync(store, 1, zone);
        _clock.Set(new DateTimeOffset(2026, 10, 1, 12, 30, 0, TimeSpan.FromHours(2)));

        var result = await Retire(store).HandleAsync(new RetireLockerRequest(locker), default);

        Assert.Equal(LockerStatus.Retired, result.Value!.Status);
        await using var context = Raw();
        var stored = await context.Set<Locker>().SingleAsync();
        Assert.Equal(_clock.UtcNow, stored.RetiredAtUtc);
        Assert.Equal(TimeSpan.Zero, stored.RetiredAtUtc!.Value.Offset);
    }

    // --- Zones ---

    [Fact]
    [Trait("spec", Spec + "/zones: Listado de zonas (Recuento de taquillas)")]
    public async Task Zones_are_listed_in_catalan_order_with_their_count_of_active_lockers()
    {
        var store = await NewDatabaseAsync();
        var first = await ZoneAsync(store, "Planta 2");
        var second = await ZoneAsync(store, "Çafareig");
        await LockerAsync(store, 1, first);
        var gone = await LockerAsync(store, 2, first);
        await Retire(store).HandleAsync(new RetireLockerRequest(gone), default);
        await LockerAsync(store, 3, second);

        var zones = (await new ListZonesHandler(store.Zones, store.Lockers).HandleAsync(new ListZonesRequest(), default)).Value!;

        Assert.Equal(["Çafareig", "Planta 2"], zones.Select(z => z.Name));
        Assert.Equal([1, 1], zones.Select(z => z.ActiveLockers));
    }

    [Fact]
    [Trait("spec", Spec + "/zones: Conservación de zonas con historial (Eliminar zona sin uso)")]
    public async Task A_zone_that_never_had_lockers_is_deleted_and_one_that_had_is_kept()
    {
        var store = await NewDatabaseAsync();
        var unused = await ZoneAsync(store, "Nova");
        var used = await ZoneAsync(store, "Planta 1");
        await LockerAsync(store, 1, used);
        var delete = new DeleteZoneHandler(store.Zones, store.Lockers, store);

        var deleted = await delete.HandleAsync(new DeleteZoneRequest(unused), default);
        var refused = await delete.HandleAsync(new DeleteZoneRequest(used), default);

        Assert.True(deleted.IsSuccess);
        Assert.Equal("Zones.HasHistory", refused.Error!.Code);
        await using var context = Raw();
        Assert.Equal(["Planta 1"], await context.Set<Zone>().Select(z => z.Name).ToListAsync());
    }

    [Fact]
    [Trait("spec", Spec + "/zones: Nombre de zona único (Nombre duplicado)")]
    public async Task A_duplicate_zone_name_is_refused_by_the_use_case_before_it_reaches_the_index()
    {
        var store = await NewDatabaseAsync();
        await ZoneAsync(store, "Gimnàs");

        var result = await new CreateZoneHandler(store.Zones, store).HandleAsync(new CreateZoneRequest("GIMNAS"), default);

        Assert.Equal("Zones.NameDuplicate", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + "/taquilles: Historial de eventos de la taquilla (Historial inmutable)")]
    public async Task A_write_outside_a_unit_of_work_is_refused()
    {
        var store = await NewDatabaseAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.Zones.AddAsync(new Zone(Guid.NewGuid(), "x", "x", true), default));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.Events.AddAsync(new HistoryEvent(Guid.NewGuid(), LockerEventTypes.Created, _clock.UtcNow, null, null), default));
    }
}
