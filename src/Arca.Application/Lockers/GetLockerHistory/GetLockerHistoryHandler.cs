// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Students;
using Arca.Application.Zones;
using Arca.Domain.Common;
using Arca.Domain.Lockers;

namespace Arca.Application.Lockers.GetLockerHistory;

/// <summary>
/// The history of one locker, most recent first, asked for when its detail is opened. Only the events of that locker
/// come back, even when another locker has the same number, because events follow the identity and not the number.
/// </summary>
public sealed class GetLockerHistoryHandler(
    ILockerRepository lockers, IZoneRepository zones, ILockerEventRepository events, IStudentRepository students, ILocalizer localizer)
{
    public async Task<Result<IReadOnlyList<LockerHistoryEntry>>> HandleAsync(GetLockerHistoryRequest request, CancellationToken ct)
    {
        if (await lockers.GetAsync(request.LockerId, ct) is null)
        {
            return Result<IReadOnlyList<LockerHistoryEntry>>.Failure(LockerErrors.NotFound);
        }

        var names = (await zones.ListAsync(ct)).ToDictionary(z => z.Id, z => z.Name);
        var studentNames = (await students.ListAsync(ct)).ToDictionary(s => s.Id, s => s.FirstName + " " + s.LastName);
        var composer = new LockerHistoryText(localizer);
        IReadOnlyList<LockerHistoryEntry> entries =
        [
            .. (await events.ListAsync(request.LockerId, ct))
                .OrderByDescending(e => e.OccurredAtUtc)
                .Select(e => new LockerHistoryEntry(e.OccurredAtUtc, e.Type, composer.Compose(e, names, studentNames)))
        ];
        return Result<IReadOnlyList<LockerHistoryEntry>>.Success(entries);
    }
}
