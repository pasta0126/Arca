// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Catalog;
using Arca.Application.Localization;
using Arca.Application.SchoolYears;
using Arca.Domain.Common;
using Arca.Domain.Students;

namespace Arca.Application.Students.GetStudentHistory;

/// <summary>The history of one student, most recent first, asked for when their record is opened.</summary>
public sealed class GetStudentHistoryHandler(
    IStudentRepository students, IStudentEventRepository events, ICatalogRepository catalog, IAcademicYearRepository years, ILocalizer localizer)
{
    public async Task<Result<IReadOnlyList<StudentHistoryEntry>>> HandleAsync(GetStudentHistoryRequest request, CancellationToken ct)
    {
        if (await students.GetAsync(request.StudentId, ct) is null)
        {
            return Result<IReadOnlyList<StudentHistoryEntry>>.Failure(StudentErrors.NotFound);
        }

        var composer = new StudentHistoryText(
            localizer,
            (await catalog.ListLevelsAsync(ct)).ToDictionary(l => l.Id, l => l.Name),
            (await catalog.ListGroupsAsync(ct)).ToDictionary(g => g.Id, g => g.Name),
            (await years.ListAsync(ct)).ToDictionary(y => y.Id, y => y.Name));
        IReadOnlyList<StudentHistoryEntry> entries =
        [
            .. (await events.ListAsync(request.StudentId, ct))
                .OrderByDescending(e => e.OccurredAtUtc)
                .Select(e => new StudentHistoryEntry(e.OccurredAtUtc, e.Type, composer.Compose(e)))
        ];
        return Result<IReadOnlyList<StudentHistoryEntry>>.Success(entries);
    }
}
