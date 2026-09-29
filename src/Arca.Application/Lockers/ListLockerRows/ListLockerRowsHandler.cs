// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Search;
using Arca.Application.Students;
using Arca.Application.Zones;
using Arca.Application.Charges;
using Arca.Domain.Common;
using Arca.Domain.Lockers;

namespace Arca.Application.Lockers.ListLockerRows;

/// <summary>
/// A locker as the list of the Lockers section shows it: its number and zone, its visible status (a retired locker is told apart
/// as such), the name of the student who holds it and whether they owe anything, and its notes. The status is a view type so a
/// screen never needs the domain model. It carries the student's name and nothing else about them: no email, no identifier.
/// </summary>
public sealed record LockerListRow(
    Guid Id, int Number, Guid ZoneId, string ZoneName, LockerStatusView Status, string? StudentName, bool HasDebt,
    string? Note, string? ReservationNote);

/// <param name="Rows">Every locker of every zone, retired ones included, by number: the screen narrows them with its filters, so a change of filter costs no new query.</param>
/// <param name="Counters">How many active lockers there are in each status, whatever the filters hide.</param>
public sealed record LockerRowsListing(IReadOnlyList<LockerListRow> Rows, LockerCounters Counters, LockerEmptyState EmptyState);

/// <summary>
/// The list of lockers for its screen (pantalles-de-domini, D6) in one query: zones, lockers, assignments, students and pending
/// charges are loaded in batches, never one locker at a time. It applies no rule of its own: the status is the one every other
/// view derives, and nothing is saved.
/// </summary>
public sealed class ListLockerRowsHandler(
    ILockerRepository lockers, IZoneRepository zones, IAssignmentRepository assignments, IStudentRepository students, IChargeRepository charges)
{
    public async Task<Result<LockerRowsListing>> HandleAsync(CancellationToken ct)
    {
        var allZones = await zones.ListAsync(ct);
        var names = allZones.ToDictionary(z => z.Id, z => z.Name);
        var all = await lockers.ListAsync(includeRetired: true, ct);
        var holder = (await assignments.ListCurrentAsync(ct)).ToDictionary(a => a.LockerId, a => a.StudentId);
        var studentNames = (await students.ListAsync(ct)).ToDictionary(s => s.Id, s => $"{s.FirstName} {s.LastName}");
        var owing = (await charges.ListPendingAsync(ct)).Where(c => c.CountsAsDebt).Select(c => c.StudentId).ToHashSet();

        var rows = all
            .OrderBy(l => l.Number)
            .ThenBy(l => l.IsRetired) // an active locker comes before a retired one with the same number
            .ThenBy(l => names.GetValueOrDefault(l.ZoneId, string.Empty), TextComparer.Comparer)
            .Select(l =>
            {
                var student = holder.TryGetValue(l.Id, out var id) && !l.IsRetired ? id : (Guid?)null;
                var status = l.IsRetired ? LockerStatusView.Retired : Enum.Parse<LockerStatusView>(l.StateWith(student is not null).Status.ToString());
                return new LockerListRow(
                    l.Id, l.Number, l.ZoneId, names.GetValueOrDefault(l.ZoneId, string.Empty), status,
                    student is { } s ? studentNames.GetValueOrDefault(s) : null, student is { } d && owing.Contains(d), l.Note, l.ReservationNote);
            })
            .ToList();

        var active = rows.Where(r => r.Status != LockerStatusView.Retired).ToList();
        var counters = new LockerCounters(
            active.Count,
            active.Count(r => r.Status == LockerStatusView.Free),
            active.Count(r => r.Status == LockerStatusView.Occupied),
            active.Count(r => r.Status == LockerStatusView.Broken),
            active.Count(r => r.Status == LockerStatusView.Maintenance),
            active.Count(r => r.Status == LockerStatusView.Reserved));
        var empty = active.Count > 0 ? LockerEmptyState.None
            : !allZones.Any(z => z.IsActive) ? LockerEmptyState.NoZones
            : LockerEmptyState.NoLockers;
        return Result<LockerRowsListing>.Success(new LockerRowsListing(rows, counters, empty));
    }
}
