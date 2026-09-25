// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Lockers;
using Arca.Application.SchoolYears;
using Arca.Application.Zones;
using Arca.Domain.Common;
using Arca.Domain.Lockers;
using Arca.Domain.SchoolYears;
using Arca.Domain.Zones;

namespace Arca.Testing.Inventory;

/// <summary>
/// The zones, lockers and events of a test, kept in memory, with the ports the use cases need. As a unit of work it
/// behaves like a transaction: what a failed or throwing operation changed is undone.
/// </summary>
public sealed class InMemoryInventory : IUnitOfWork
{
    public InMemoryInventory()
    {
        Zones = new ZoneRepository(this);
        Lockers = new LockerRepository(this);
        Events = new EventRepository(this);
        Years = new YearRepository(this);
    }

    public List<Zone> ZoneList { get; private set; } = [];

    public List<Locker> LockerList { get; private set; } = [];

    public List<HistoryEvent> EventList { get; private set; } = [];

    public List<AcademicYear> YearList { get; private set; } = [];

    /// <summary>The years a test marks as holding enrolments or assignments, until those exist.</summary>
    public HashSet<Guid> YearsWithData { get; } = [];

    public IZoneRepository Zones { get; }

    public ILockerRepository Lockers { get; }

    public ILockerEventRepository Events { get; }

    public IAcademicYearRepository Years { get; }

    /// <summary>Which lockers a student holds. The real one comes with the assignments.</summary>
    public ConfigurableOccupancy Occupancy { get; } = new();

    /// <summary>Makes the next saved change fail, so a test can prove that nothing stays half done.</summary>
    public bool FailOnNextEvent { get; set; }

    /// <summary>Lets this many events be saved and makes the next one fail: a failure in the middle of a bulk save.</summary>
    public int? EventsBeforeFailure { get; set; }

    public async Task<Result<T>> RunAsync<T>(Func<CancellationToken, Task<Result<T>>> work, CancellationToken ct)
    {
        var zones = ZoneList.Select(z => new Zone(z.Id, z.Name, z.NameKey, z.IsActive)).ToList();
        var lockers = LockerList.Select(Copy).ToList();
        var events = EventList.ToList();
        var years = YearList.Select(y => AcademicYear.Restore(y.Id, y.StartDate, y.EndDate, y.IsActive)).ToList();
        Result<T> result;
        try
        {
            result = await work(ct);
        }
        catch
        {
            (ZoneList, LockerList, EventList, YearList) = (zones, lockers, events, years);
            throw;
        }

        if (!result.IsSuccess)
        {
            (ZoneList, LockerList, EventList, YearList) = (zones, lockers, events, years);
        }

        return result;
    }

    sealed class YearRepository(InMemoryInventory owner) : IAcademicYearRepository
    {
        public Task<IReadOnlyList<AcademicYear>> ListAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<AcademicYear>>([.. owner.YearList]);

        public Task<AcademicYear?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(owner.YearList.FirstOrDefault(y => y.Id == id));

        public Task<AcademicYear?> GetActiveAsync(CancellationToken ct) => Task.FromResult(owner.YearList.FirstOrDefault(y => y.IsActive));

        public Task AddAsync(AcademicYear year, CancellationToken ct)
        {
            owner.YearList.Add(year);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(AcademicYear year, CancellationToken ct) => Task.CompletedTask;

        public Task RemoveAsync(AcademicYear year, CancellationToken ct)
        {
            owner.YearList.Remove(year);
            return Task.CompletedTask;
        }

        public Task<bool> HasDataAsync(Guid yearId, CancellationToken ct) => Task.FromResult(owner.YearsWithData.Contains(yearId));
    }

    static Locker Copy(Locker l) =>
        new(l.Id, l.Number, l.ZoneId, l.Note, l.OutOfService, l.IsReserved, l.ReservationNote, l.RetiredAtUtc);

    sealed class ZoneRepository(InMemoryInventory owner) : IZoneRepository
    {
        public Task<IReadOnlyList<Zone>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Zone>>([.. owner.ZoneList]);

        public Task<Zone?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(owner.ZoneList.FirstOrDefault(z => z.Id == id));

        public Task AddAsync(Zone zone, CancellationToken ct)
        {
            owner.ZoneList.Add(zone);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Zone zone, CancellationToken ct) => Task.CompletedTask;

        public Task RemoveAsync(Zone zone, CancellationToken ct)
        {
            owner.ZoneList.Remove(zone);
            return Task.CompletedTask;
        }
    }

    sealed class LockerRepository(InMemoryInventory owner) : ILockerRepository
    {
        public Task<IReadOnlyList<Locker>> ListAsync(bool includeRetired, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Locker>>([.. owner.LockerList.Where(l => includeRetired || !l.IsRetired)]);

        public Task<Locker?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(owner.LockerList.FirstOrDefault(l => l.Id == id));

        public Task AddAsync(Locker locker, CancellationToken ct)
        {
            owner.LockerList.Add(locker);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Locker locker, CancellationToken ct) => Task.CompletedTask;

        public Task<int> CountActiveInZoneAsync(Guid zoneId, CancellationToken ct) =>
            Task.FromResult(owner.LockerList.Count(l => l.ZoneId == zoneId && !l.IsRetired));

        public Task<bool> HasEverHadLockersAsync(Guid zoneId, CancellationToken ct) =>
            Task.FromResult(owner.LockerList.Any(l => l.ZoneId == zoneId));
    }

    sealed class EventRepository(InMemoryInventory owner) : ILockerEventRepository
    {
        public Task AddAsync(HistoryEvent change, CancellationToken ct)
        {
            if (owner.FailOnNextEvent || owner.EventsBeforeFailure == 0)
            {
                owner.FailOnNextEvent = false;
                owner.EventsBeforeFailure = null;
                throw new IOException("the disk failed while saving");
            }

            if (owner.EventsBeforeFailure is > 0)
            {
                owner.EventsBeforeFailure--;
            }

            owner.EventList.Add(change);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<HistoryEvent>> ListAsync(Guid lockerId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<HistoryEvent>>([.. owner.EventList.Where(e => e.EntityId == lockerId).OrderByDescending(e => e.OccurredAtUtc)]);
    }
}
