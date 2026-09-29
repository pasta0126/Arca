// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using Arca.Application.Assignments;
using Arca.Application.Catalog;
using Arca.Application.Charges;
using Arca.Application.Enrollments;
using Arca.Application.Lockers;
using Arca.Application.SchoolYears;
using Arca.Application.Students;
using Arca.Application.Zones;
using Arca.Domain.Common;

namespace Arca.Application.Search;

/// <summary>
/// The global search (ui-shell, D4): students, lockers and groups for one piece of text, without telling capitals or accents
/// apart, using the text comparison of the whole application. By default it looks among the students enrolled in the active
/// year (leaving out those who left, unless asked) and among the active lockers. With this volume it loads what it needs in
/// batches and filters in memory; it saves nothing and computes no rules: the status of a locker and the debt come from the
/// same functions the rest of the application uses.
/// </summary>
public sealed class GlobalSearchHandler(
    IStudentRepository students, IEnrollmentRepository enrollments, ICatalogRepository catalog, IAcademicYearRepository years,
    ILockerRepository lockers, IZoneRepository zones, IAssignmentRepository assignments, IChargeRepository charges)
{
    public async Task<Result<GlobalSearchResult>> HandleAsync(GlobalSearchRequest request, CancellationToken ct)
    {
        var key = TextComparer.Key(request.Text);
        var words = key.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return Result<GlobalSearchResult>.Success(GlobalSearchResult.Empty);
        }

        var max = Math.Max(1, request.MaxPerType);
        var year = await years.GetActiveAsync(ct);
        var current = await assignments.ListCurrentAsync(ct);
        var allLockers = await lockers.ListAsync(includeRetired: false, ct);
        var allZones = await zones.ListAsync(ct);
        var zoneNames = allZones.ToDictionary(z => z.Id, z => z.Name);
        var activeZones = allZones.Where(z => z.IsActive).Select(z => z.Id).ToHashSet();
        var levels = (await catalog.ListLevelsAsync(ct)).ToDictionary(l => l.Id, l => l.Name);
        var groups = (await catalog.ListGroupsAsync(ct)).ToDictionary(g => g.Id, g => g.Name);
        var inYear = year is null ? [] : (await enrollments.ListByYearAsync(year.Id, ct)).ToDictionary(e => e.StudentId);
        var everyone = (await students.ListAsync(ct)).ToDictionary(s => s.Id);
        var byLocker = current.ToDictionary(a => a.LockerId, a => a.StudentId);
        var numberOf = allLockers.ToDictionary(l => l.Id, l => l.Number);
        var lockerOf = current.ToDictionary(a => a.StudentId, a => numberOf.TryGetValue(a.LockerId, out var locker) ? locker : (int?)null);
        var debt = (await charges.ListPendingAsync(ct)).GroupBy(c => c.StudentId).ToDictionary(g => g.Key, g => g.Aggregate(Money.Zero, (sum, c) => sum + c.Amount).Amount);

        // Students: every word is in their name, or the text is the number of the locker they hold.
        var number = int.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : (int?)null;
        var matching = inYear.Keys
            .Select(id => everyone[id])
            .Where(s => words.All(w => s.NameKey.Contains(w, StringComparison.Ordinal)) || (number is not null && lockerOf.GetValueOrDefault(s.Id) == number))
            .ToList();
        var shown = matching.Where(s => request.IncludeRetired || !s.IsRetired).ToList();
        var studentHits = shown
            .OrderBy(s => s.LastName, TextComparer.Comparer).ThenBy(s => s.FirstName, TextComparer.Comparer).ThenBy(s => s.Id)
            .Take(max)
            .Select(s => new StudentHit(
                s.Id, s.FirstName, s.LastName,
                levels.GetValueOrDefault(inYear[s.Id].LevelId),
                inYear[s.Id].GroupId is { } g ? groups.GetValueOrDefault(g) : null,
                s.IsRetired ? null : lockerOf.GetValueOrDefault(s.Id),
                s.IsRetired, debt.ContainsKey(s.Id), debt.GetValueOrDefault(s.Id)))
            .ToList();

        // Lockers: the number typed, exactly.
        // The lockers of a deactivated zone are not on the map, so they are not offered here either.
        var lockerMatches = number is null ? [] : allLockers.Where(l => l.Number == number && activeZones.Contains(l.ZoneId)).ToList();
        var lockerHits = lockerMatches
            .OrderBy(l => l.Number).ThenBy(l => zoneNames.GetValueOrDefault(l.ZoneId), TextComparer.Comparer)
            .Take(max)
            .Select(l =>
            {
                var holder = byLocker.TryGetValue(l.Id, out var id) ? everyone.GetValueOrDefault(id) : null;
                var row = LockerRow.Of(l, zoneNames.GetValueOrDefault(l.ZoneId, string.Empty), hasAssignment: holder is not null);
                return new LockerHit(l.Id, l.Number, row.ZoneName, Enum.Parse<LockerStatusView>(row.Status.ToString()), holder is null ? null : $"{holder.FirstName} {holder.LastName}");
            })
            .ToList();

        // Groups: each word begins a word of the level or the group, so "1r a" finds "1r ESO A" but a lone "a" does not find everything.
        var groupCounts = inYear.Values
            .Where(e => !everyone[e.StudentId].IsRetired)
            .GroupBy(e => (e.LevelId, e.GroupId))
            .ToDictionary(g => g.Key, g => g.Count());
        var groupMatches = groupCounts
            .Where(kv => levels.ContainsKey(kv.Key.LevelId))
            .Select(kv => new GroupHit(kv.Key.GroupId, kv.Key.LevelId, levels[kv.Key.LevelId], kv.Key.GroupId is { } g ? groups.GetValueOrDefault(g) : null, kv.Value))
            .Where(g => BeginsEveryWord(TextComparer.Key(g.LevelName + " " + g.GroupName), words))
            .OrderBy(g => g.LevelName, TextComparer.Comparer).ThenBy(g => g.GroupName, TextComparer.Comparer)
            .ToList();

        return Result<GlobalSearchResult>.Success(new GlobalSearchResult(
            studentHits, shown.Count, lockerHits, lockerMatches.Count, [.. groupMatches.Take(max)], groupMatches.Count,
            request.IncludeRetired ? 0 : matching.Count(s => s.IsRetired)));
    }

    static bool BeginsEveryWord(string text, string[] words)
    {
        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.All(w => tokens.Any(t => t.StartsWith(w, StringComparison.Ordinal)));
    }
}
