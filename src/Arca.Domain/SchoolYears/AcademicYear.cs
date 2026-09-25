// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.SchoolYears;

/// <summary>
/// A school year (alumnes-i-assignacions): dates, a name derived from the year it starts, and whether it is the active one.
/// At most one is active at a time. The rules that need to know about other years, or about the data they hold, are given
/// what they need, so the entity is tested without a database. Business rules return a failure instead of throwing.
/// </summary>
public sealed class AcademicYear
{
    AcademicYear(Guid id, DateOnly start, DateOnly end, bool isActive)
    {
        Id = id;
        StartDate = start;
        EndDate = end;
        IsActive = isActive;
    }

    /// <summary>Rebuilds a stored year. Used by persistence, which has already validated it.</summary>
    public static AcademicYear Restore(Guid id, DateOnly start, DateOnly end, bool isActive) => new(id, start, end, isActive);

    public Guid Id { get; }

    public DateOnly StartDate { get; }

    public DateOnly EndDate { get; }

    public bool IsActive { get; private set; }

    /// <summary>The year the course starts in.</summary>
    public int StartYear => StartDate.Year;

    /// <summary>The name in the form "2026-2027", derived from the year it starts.</summary>
    public string Name => NameOf(StartYear);

    public static string NameOf(int startYear) => $"{startYear}-{startYear + 1}";

    /// <summary>
    /// Creates a year. Its end must be after its start, no other year may start in the same year, and its dates must not
    /// overlap those of another. The first year of the system is created active; later ones are not.
    /// </summary>
    public static Result<AcademicYear> Create(Guid id, DateOnly start, DateOnly end, IEnumerable<AcademicYear> existing)
    {
        if (end <= start)
        {
            return Result<AcademicYear>.Failure(SchoolYearErrors.DatesInvalid);
        }

        var others = existing.ToList();
        if (others.Any(y => y.StartYear == start.Year))
        {
            return Result<AcademicYear>.Failure(SchoolYearErrors.AlreadyExists(NameOf(start.Year)));
        }

        var overlapped = others.FirstOrDefault(y => start <= y.EndDate && y.StartDate <= end);
        return overlapped is not null
            ? Result<AcademicYear>.Failure(SchoolYearErrors.Overlaps(overlapped.Name))
            : Result<AcademicYear>.Success(new AcademicYear(id, start, end, isActive: others.Count == 0));
    }

    /// <summary>
    /// Makes this year the active one, which is only possible when no other is active. Closing the active year, which
    /// frees the activation, belongs to cursos-i-historial. Activating the active year changes nothing.
    /// </summary>
    public Result<AcademicYear> Activate(IEnumerable<AcademicYear> all)
    {
        if (all.Any(y => y.IsActive && y.Id != Id))
        {
            return Result<AcademicYear>.Failure(SchoolYearErrors.AnotherActive);
        }

        IsActive = true;
        return Result<AcademicYear>.Success(this);
    }

    /// <summary>Whether the year may be deleted: only one with no enrolments and no assignments.</summary>
    /// <param name="hasData">True if any enrolment or assignment belongs to the year.</param>
    public Result<AcademicYear> CheckCanDelete(bool hasData) =>
        hasData ? Result<AcademicYear>.Failure(SchoolYearErrors.HasData) : Result<AcademicYear>.Success(this);
}
