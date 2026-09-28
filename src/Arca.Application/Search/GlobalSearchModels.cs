// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Search;

/// <param name="Text">What the person typed. Empty finds nothing.</param>
/// <param name="IncludeRetired">Also search the students who have left; off by default.</param>
/// <param name="MaxPerType">The most results shown of each kind; the total of each is still reported.</param>
public sealed record GlobalSearchRequest(string? Text, bool IncludeRetired = false, int MaxPerType = GlobalSearchRequest.DefaultMaxPerType)
{
    public const int DefaultMaxPerType = 8;
}

/// <summary>
/// A student found. It carries no email and no identifier of the student, only what a person needs to recognise them:
/// name, level, group, locker and whether they owe anything (ui-shell, Resultados con estado visible).
/// </summary>
public sealed record StudentHit(
    Guid StudentId, string FirstName, string LastName, string? LevelName, string? GroupName, int? LockerNumber, bool IsRetired,
    bool HasDebt, decimal PendingTotal);

/// <summary>The status of a locker as a result shows it, so the interface does not depend on the domain model.</summary>
public enum LockerStatusView
{
    Free,
    Occupied,
    Reserved,
    Broken,
    Maintenance,
    Retired,
}

/// <summary>A locker found: where it is, its status, and who holds it.</summary>
public sealed record LockerHit(Guid LockerId, int Number, string ZoneName, LockerStatusView Status, string? StudentName);

/// <summary>A group found: its level and how many active students are in it.</summary>
public sealed record GroupHit(Guid? GroupId, Guid LevelId, string LevelName, string? GroupName, int StudentCount);

/// <summary>
/// The results of a global search, by kind, each limited to the most that are shown and with its total so the person can be
/// told "12 in all". <see cref="RetiredMatches"/> says how many students who left also match, when they were not included,
/// so the search can offer to include them.
/// </summary>
public sealed record GlobalSearchResult(
    IReadOnlyList<StudentHit> Students, int StudentTotal,
    IReadOnlyList<LockerHit> Lockers, int LockerTotal,
    IReadOnlyList<GroupHit> Groups, int GroupTotal,
    int RetiredMatches)
{
    public static GlobalSearchResult Empty { get; } = new([], 0, [], 0, [], 0, 0);

    public bool IsEmpty => StudentTotal + LockerTotal + GroupTotal == 0;
}
