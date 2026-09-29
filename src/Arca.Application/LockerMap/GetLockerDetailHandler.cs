// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Catalog;
using Arca.Application.Charges;
using Arca.Application.Enrollments;
using Arca.Application.Lockers;
using Arca.Application.SchoolYears;
using Arca.Application.Search;
using Arca.Application.Students;
using Arca.Application.Zones;
using Arca.Domain.Common;
using Arca.Domain.Lockers;

namespace Arca.Application.LockerMap;

/// <summary>
/// The detail of a locker for its panel (ui-shell, Detalle de la taquilla seleccionada): its status and notes, who holds it with
/// their level and group, and whether they owe anything. It carries no email and no identifier of the student.
/// </summary>
/// <param name="Note">Why the locker is out of service, if it is.</param>
/// <param name="ReservationNote">Why it is reserved, if it is.</param>
public sealed record LockerDetail(
    Guid LockerId, int Number, string ZoneName, LockerStatusView Status, string? Note, string? ReservationNote,
    Guid? StudentId, string? StudentName, string? LevelName, string? GroupName, bool HasDebt, decimal PendingTotal);

/// <summary>Reads what the panel of a selected locker shows. It saves nothing.</summary>
public sealed class GetLockerDetailHandler(
    ILockerRepository lockers, IZoneRepository zones, IAssignmentRepository assignments, IStudentRepository students,
    IEnrollmentRepository enrollments, ICatalogRepository catalog, IAcademicYearRepository years, IChargeRepository charges)
{
    /// <returns>The detail, or null if the locker does not exist or is retired.</returns>
    public async Task<Result<LockerDetail?>> HandleAsync(Guid lockerId, CancellationToken ct)
    {
        var locker = await lockers.GetAsync(lockerId, ct);
        if (locker is null || locker.IsRetired)
        {
            return Result<LockerDetail?>.Success(null);
        }

        var zone = await zones.GetAsync(locker.ZoneId, ct);
        var current = await assignments.GetCurrentOfLockerAsync(lockerId, ct);
        var student = current is null ? null : await students.GetAsync(current.StudentId, ct);
        string? level = null;
        string? group = null;
        var pending = 0m;
        if (student is not null)
        {
            if (await years.GetActiveAsync(ct) is { } year && await enrollments.GetAsync(student.Id, year.Id, ct) is { } enrolment)
            {
                level = (await catalog.ListLevelsAsync(ct)).FirstOrDefault(l => l.Id == enrolment.LevelId)?.Name;
                group = enrolment.GroupId is { } g ? (await catalog.ListGroupsAsync(ct)).FirstOrDefault(x => x.Id == g)?.Name : null;
            }

            pending = (await charges.ListByStudentAsync(student.Id, ct)).Where(c => c.CountsAsDebt).Aggregate(Money.Zero, (sum, c) => sum + c.Amount).Amount;
        }

        var status = Enum.Parse<LockerStatusView>(locker.StateWith(student is not null).Status.ToString());
        return Result<LockerDetail?>.Success(new LockerDetail(
            locker.Id, locker.Number, zone?.Name ?? string.Empty, status, locker.Note, locker.ReservationNote,
            student?.Id, student is null ? null : $"{student.FirstName} {student.LastName}", level, group, pending > 0, pending));
    }
}
