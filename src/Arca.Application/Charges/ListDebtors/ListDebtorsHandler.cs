// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Catalog;
using Arca.Application.Enrollments;
using Arca.Application.Lockers;
using Arca.Application.SchoolYears;
using Arca.Application.Students;
using Arca.Domain.Charges;
using Arca.Domain.Common;

namespace Arca.Application.Charges.ListDebtors;

/// <summary>
/// The students with pending charges, with their breakdown and the totals, under the filters (pagaments, D8). With this
/// volume it loads the pending charges and what it needs to describe their students explicitly and filters in memory;
/// it does not lazy load. Retired students with debt are included and marked.
/// </summary>
public sealed class ListDebtorsHandler(
    IChargeRepository charges, IStudentRepository students, IEnrollmentRepository enrollments, ICatalogRepository catalog,
    IAcademicYearRepository years, IStudentLockers lockers, ILockerRepository lockerRepository)
{
    public async Task<Result<DebtorsListing>> HandleAsync(ListDebtorsRequest request, CancellationToken ct)
    {
        var filter = request.Filter ?? new DebtorFilter();
        var pending = await charges.ListPendingAsync(ct);
        var active = await years.GetActiveAsync(ct);
        var byStudent = pending.GroupBy(c => c.StudentId).ToDictionary(g => g.Key, g => g.ToList());
        var known = (await students.ListAsync(ct)).Where(s => byStudent.ContainsKey(s.Id)).ToList();

        var inYear = active is null ? [] : (await enrollments.ListByYearAsync(active.Id, ct)).ToDictionary(e => e.StudentId);
        var levels = (await catalog.ListLevelsAsync(ct)).ToDictionary(l => l.Id, l => l.Name);
        var groups = (await catalog.ListGroupsAsync(ct)).ToDictionary(g => g.Id, g => g.Name);
        var held = await lockers.CurrentAsync([.. known.Where(s => !s.IsRetired).Select(s => s.Id)], ct);
        var zoneOf = filter.ZoneId is null ? [] : (await lockerRepository.ListAsync(includeRetired: true, ct)).ToDictionary(l => l.Id, l => l.ZoneId);

        var all = known.Select(s => (Student: s, Standing: PaymentStanding.Of(byStudent[s.Id], active?.Id))).ToList();
        var narrowed = new List<DebtorRow>();
        foreach (var (student, _) in all)
        {
            var counted = byStudent[student.Id]
                .Where(c => filter.YearId is null || c.YearId == filter.YearId)
                .Where(c => filter.Concept is null || c.Concept == filter.Concept)
                .ToList();
            var enrollment = inYear.GetValueOrDefault(student.Id);
            var locker = held.GetValueOrDefault(student.Id);
            var matches = counted.Count > 0
                && (filter.LevelId is null || enrollment?.LevelId == filter.LevelId)
                && (filter.GroupId is null || enrollment?.GroupId == filter.GroupId)
                && (filter.ZoneId is null || (locker is not null && zoneOf.GetValueOrDefault(locker.LockerId) == filter.ZoneId));
            if (!matches)
            {
                continue;
            }

            var standing = PaymentStanding.Of(counted, active?.Id);
            narrowed.Add(new DebtorRow(
                student.Id, student.FirstName, student.LastName,
                enrollment is null ? null : levels.GetValueOrDefault(enrollment.LevelId),
                enrollment?.GroupId is { } g ? groups.GetValueOrDefault(g) : null,
                student.IsRetired, locker?.Number, standing.PendingTotal, standing.Breakdown));
        }

        IReadOnlyList<DebtorRow> rows = [.. narrowed.OrderBy(r => r.LastName, TextComparer.Comparer).ThenBy(r => r.FirstName, TextComparer.Comparer).ThenBy(r => r.StudentId)];
        var empty = rows.Count > 0 ? DebtorsEmptyState.None : pending.Count == 0 ? DebtorsEmptyState.NoDebt : DebtorsEmptyState.NoResults;
        return Result<DebtorsListing>.Success(new DebtorsListing(
            rows, rows.Count, rows.Sum(r => r.PendingTotal), all.Count, all.Sum(a => a.Standing.PendingTotal), empty));
    }
}
