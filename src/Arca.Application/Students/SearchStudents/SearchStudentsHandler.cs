// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Catalog;
using Arca.Application.Enrollments;
using Arca.Application.SchoolYears;
using Arca.Domain.Common;
using Arca.Domain.SchoolYears;

namespace Arca.Application.Students.SearchStudents;

/// <summary>
/// Lists and searches the students of the active year (alumnes-i-assignacions, D13). By default only the active ones are
/// shown. With this volume it loads the data explicitly and filters in memory, asking the assignments once for the lockers
/// of every student. Names are compared without case or accents, and the rows never carry the email.
/// </summary>
public sealed class SearchStudentsHandler(
    IStudentRepository students, IEnrollmentRepository enrollments, ICatalogRepository catalog, IAcademicYearRepository years, IStudentLockers lockers)
{
    public async Task<Result<StudentListing>> HandleAsync(SearchStudentsRequest request, CancellationToken ct)
    {
        var filter = request.Filter ?? new StudentFilter();
        var year = await years.GetActiveAsync(ct);
        if (year is null)
        {
            return Result<StudentListing>.Failure(SchoolYearErrors.NoActiveYear);
        }

        var inYear = (await enrollments.ListByYearAsync(year.Id, ct)).ToDictionary(e => e.StudentId);
        var levels = (await catalog.ListLevelsAsync(ct)).ToDictionary(l => l.Id, l => l.Name);
        var groups = (await catalog.ListGroupsAsync(ct)).ToDictionary(g => g.Id, g => g.Name);
        var enrolled = (await students.ListAsync(ct)).Where(s => inYear.ContainsKey(s.Id)).ToList();
        var held = await lockers.CurrentAsync([.. enrolled.Where(s => !s.IsRetired).Select(s => s.Id)], ct);
        var words = TextComparer.Key(filter.Text).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        IReadOnlyList<StudentRow> rows =
        [
            .. enrolled
                .Where(s => filter.IncludeRetired || !s.IsRetired)
                .Where(s => words.All(w => s.NameKey.Contains(w, StringComparison.Ordinal)))
                .Where(s => filter.LevelId is null || inYear[s.Id].LevelId == filter.LevelId)
                .Where(s => filter.GroupId is null || inYear[s.Id].GroupId == filter.GroupId)
                .Where(s => filter.LockerNumber is null || held.GetValueOrDefault(s.Id)?.Number == filter.LockerNumber)
                .Where(s => filter.LockerState switch
                {
                    StudentLockerState.WithLocker => held.ContainsKey(s.Id),
                    StudentLockerState.WithoutLocker => !s.IsRetired && !held.ContainsKey(s.Id),
                    _ => true,
                })
                .OrderBy(s => s.LastName, TextComparer.Comparer)
                .ThenBy(s => s.FirstName, TextComparer.Comparer)
                .ThenBy(s => s.Id)
                .Select(s => new StudentRow(
                    s.Id, s.FirstName, s.LastName,
                    levels.GetValueOrDefault(inYear[s.Id].LevelId),
                    inYear[s.Id].GroupId is { } g ? groups.GetValueOrDefault(g) : null,
                    s.IsRetired,
                    held.GetValueOrDefault(s.Id)?.Number))
        ];

        var active = enrolled.Count(s => !s.IsRetired);
        var withLocker = enrolled.Count(s => !s.IsRetired && held.ContainsKey(s.Id));
        return Result<StudentListing>.Success(new StudentListing(rows, new StudentCounters(active, withLocker, active - withLocker)));
    }
}
