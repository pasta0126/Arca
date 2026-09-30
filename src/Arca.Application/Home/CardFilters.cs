// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Search;
using Arca.Application.Students.ListStudentRows;
using Arca.Domain.Home;

namespace Arca.Application.Home;

/// <summary>
/// The one rule of which lockers a filter keeps (targetes-d-inici, D2): the screens draw the rows it keeps and the cards count them, so the
/// count of a card and what its screen shows are the same thing and cannot drift apart.
/// </summary>
/// <param name="Status">Only lockers in this status, or null for any.</param>
/// <param name="ZoneId">Only lockers of this zone, or null for any.</param>
public sealed record LockerCardFilter(LockerStatusView? Status = null, Guid? ZoneId = null)
{
    /// <summary>Reads the filter a card or a request holds; what is not there or is not understood does not filter.</summary>
    public static LockerCardFilter From(IReadOnlyDictionary<string, string> criteria) => new(
        criteria.TryGetValue(HomeCardCriteria.Status, out var status) && Enum.TryParse<LockerStatusView>(status, out var parsed) ? parsed : null,
        criteria.TryGetValue(HomeCardCriteria.Zone, out var zone) && Guid.TryParse(zone, out var id) ? id : null);

    /// <summary>Whether a row passes the status and the zone. What the list does, on top of which it hides the retired ones or shows them.</summary>
    public bool MatchesCriteria(LockerListRow row) =>
        (Status is null || row.Status == Status) && (ZoneId is null || row.ZoneId == ZoneId);

    /// <summary>Whether a row is one of the active lockers the filter keeps: what a card counts.</summary>
    public bool Matches(LockerListRow row) => row.Status != LockerStatusView.Retired && MatchesCriteria(row);

    /// <summary>The filter written as criteria, leaving out what does not filter.</summary>
    public IReadOnlyDictionary<string, string> ToCriteria()
    {
        var criteria = new Dictionary<string, string>();
        if (Status is { } status)
        {
            criteria[HomeCardCriteria.Status] = status.ToString();
        }

        if (ZoneId is { } zone)
        {
            criteria[HomeCardCriteria.Zone] = zone.ToString();
        }

        return criteria;
    }
}

/// <summary>
/// The one rule of which students a filter keeps, shared by the list of students and the cards that count them. A student who left and
/// still owes is kept when the filter asks for pending payments, whether or not the students who left are listed.
/// </summary>
/// <param name="Locker">"with" or "without" a locker, or null for either.</param>
/// <param name="Payment">"pending" or "upToDate", or null for either.</param>
/// <param name="Level">Only students of this level, by name, or null for any.</param>
/// <param name="Group">Only students of this group, by name, or null for any.</param>
/// <param name="IncludeRetired">Also list the students who left.</param>
public sealed record StudentCardFilter(string? Locker = null, string? Payment = null, string? Level = null, string? Group = null, bool IncludeRetired = false)
{
    /// <summary>Reads the filter a card or a request holds; what is not there does not filter.</summary>
    public static StudentCardFilter From(IReadOnlyDictionary<string, string> criteria) => new(
        Value(criteria, HomeCardCriteria.Locker), Value(criteria, HomeCardCriteria.Payment), Value(criteria, HomeCardCriteria.Level),
        Value(criteria, HomeCardCriteria.Group), criteria.TryGetValue(HomeCardCriteria.IncludeRetired, out var retired) && retired == "true");

    /// <summary>Whether a student passes the filter.</summary>
    public bool Matches(StudentListRow student) =>
        (IncludeRetired || !student.IsRetired || (Payment == "pending" && student.HasDebt))
        && (Level is null || student.LevelName == Level)
        && (Group is null || student.GroupName == Group)
        && (Locker switch { "with" => student.LockerNumber is not null, "without" => student.LockerNumber is null && !student.IsRetired, _ => true })
        && (Payment switch { "pending" => student.HasDebt, "upToDate" => !student.HasDebt, _ => true });

    /// <summary>The filter written as criteria, leaving out what does not filter.</summary>
    public IReadOnlyDictionary<string, string> ToCriteria()
    {
        var criteria = new Dictionary<string, string>();
        Add(criteria, HomeCardCriteria.Locker, Locker);
        Add(criteria, HomeCardCriteria.Payment, Payment);
        Add(criteria, HomeCardCriteria.Level, Level);
        Add(criteria, HomeCardCriteria.Group, Group);
        if (IncludeRetired)
        {
            criteria[HomeCardCriteria.IncludeRetired] = "true";
        }

        return criteria;
    }

    static string? Value(IReadOnlyDictionary<string, string> criteria, string name) =>
        criteria.TryGetValue(name, out var value) && value.Length > 0 ? value : null;

    static void Add(Dictionary<string, string> criteria, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            criteria[name] = value;
        }
    }
}
