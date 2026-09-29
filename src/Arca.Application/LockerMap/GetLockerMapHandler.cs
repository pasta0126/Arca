// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Charges;
using Arca.Application.Lockers;
using Arca.Application.Search;
using Arca.Application.Students;
using Arca.Application.Zones;
using Arca.Domain.Common;
using Arca.Domain.Lockers;

namespace Arca.Application.LockerMap;

/// <summary>
/// The map of lockers in a single query (ui-shell, D5): every active locker of every active zone with its visible status,
/// the student who holds it and whether they owe anything. It loads zones, lockers, assignments, students and pending charges
/// in batches, never one locker at a time, and the status comes from the same function the rest of the application uses. It
/// saves nothing. A deactivated zone and a retired locker do not appear.
/// </summary>
public sealed class GetLockerMapHandler(
    IZoneRepository zones, ILockerRepository lockers, IAssignmentRepository assignments, IStudentRepository students, IChargeRepository charges)
{
    public async Task<Result<LockerMapData>> HandleAsync(CancellationToken ct)
    {
        var activeZones = (await zones.ListAsync(ct)).Where(z => z.IsActive).ToList();
        var zoneIds = activeZones.Select(z => z.Id).ToHashSet();
        var active = (await lockers.ListAsync(includeRetired: false, ct)).Where(l => zoneIds.Contains(l.ZoneId)).ToList();
        var holder = (await assignments.ListCurrentAsync(ct)).ToDictionary(a => a.LockerId, a => a.StudentId);
        var names = (await students.ListAsync(ct)).ToDictionary(s => s.Id, s => $"{s.FirstName} {s.LastName}");
        var owing = (await charges.ListPendingAsync(ct)).Select(c => c.StudentId).ToHashSet();

        var byZone = active.GroupBy(l => l.ZoneId).ToDictionary(g => g.Key, g => g.OrderBy(l => l.Number).Select(l => Map(l, holder, names, owing)).ToList());
        var zoneMaps = activeZones
            .OrderBy(z => z.Name, TextComparer.Comparer)
            .Select(z =>
            {
                var items = byZone.GetValueOrDefault(z.Id) ?? [];
                return new ZoneMap(z.Id, z.Name, items, Count(items));
            })
            .ToList();
        return Result<LockerMapData>.Success(new LockerMapData(zoneMaps, Count(zoneMaps.SelectMany(z => z.Lockers))));
    }

    internal static MapLocker Map(Locker locker, IReadOnlyDictionary<Guid, Guid> holder, IReadOnlyDictionary<Guid, string> names, IReadOnlySet<Guid> owing)
    {
        var student = holder.TryGetValue(locker.Id, out var id) ? id : (Guid?)null;
        var status = Enum.Parse<LockerStatusView>(locker.StateWith(student is not null).Status.ToString());
        return new MapLocker(
            locker.Id, locker.Number, status, student, student is { } s ? names.GetValueOrDefault(s) : null, student is { } d && owing.Contains(d));
    }

    /// <summary>How many lockers there are in each status.</summary>
    public static LockerCounters Count(IEnumerable<MapLocker> items)
    {
        var list = items.ToList();
        return new LockerCounters(
            list.Count,
            list.Count(l => l.Status == LockerStatusView.Free),
            list.Count(l => l.Status == LockerStatusView.Occupied),
            list.Count(l => l.Status == LockerStatusView.Broken),
            list.Count(l => l.Status == LockerStatusView.Maintenance),
            list.Count(l => l.Status == LockerStatusView.Reserved));
    }
}

/// <summary>
/// One locker of the map, read again (ui-shell, Actualización tras un cambio): after an assignment or a release only this
/// locker and the counters change, so the screen asks for it alone and does not reload the whole map.
/// </summary>
public sealed class GetMapLockerHandler(
    ILockerRepository lockers, IAssignmentRepository assignments, IStudentRepository students, IChargeRepository charges)
{
    /// <returns>The locker as the map shows it now, or null if it is no longer on the map (retired).</returns>
    public async Task<Result<MapLocker?>> HandleAsync(Guid lockerId, CancellationToken ct)
    {
        var locker = await lockers.GetAsync(lockerId, ct);
        if (locker is null || locker.IsRetired)
        {
            return Result<MapLocker?>.Success(null);
        }

        var current = await assignments.GetCurrentOfLockerAsync(lockerId, ct);
        var holder = current is null ? [] : new Dictionary<Guid, Guid> { [lockerId] = current.StudentId };
        var names = new Dictionary<Guid, string>();
        var owing = new HashSet<Guid>();
        if (current is not null && await students.GetAsync(current.StudentId, ct) is { } student)
        {
            names[student.Id] = $"{student.FirstName} {student.LastName}";
            if ((await charges.ListByStudentAsync(student.Id, ct)).Any(c => c.CountsAsDebt))
            {
                owing.Add(student.Id);
            }
        }

        return Result<MapLocker?>.Success(GetLockerMapHandler.Map(locker, holder, names, owing));
    }
}
