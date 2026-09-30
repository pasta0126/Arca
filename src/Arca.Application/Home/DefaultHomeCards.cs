// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Home;

namespace Arca.Application.Home;

/// <summary>One of the cards every centre starts with: a stable key, the key of its title and the filter it opens.</summary>
public sealed record DefaultHomeCard(string SeedKey, string TitleKey, HomeCardTarget Target, IReadOnlyDictionary<string, string> Criteria);

/// <summary>
/// The cards every centre starts with (targetes-d-inici, Tarjetas de serie), in their order: the lockers by status, which open the map,
/// and the students without a locker and with pending payments. The stable key is what lets restoring them add only the missing ones.
/// </summary>
public static class DefaultHomeCards
{
    public static IReadOnlyList<DefaultHomeCard> All { get; } =
    [
        Locker("lockers-free", "LockersFree", "Free"),
        Locker("lockers-occupied", "LockersOccupied", "Occupied"),
        Locker("lockers-reserved", "LockersReserved", "Reserved"),
        Locker("lockers-broken", "LockersBroken", "Broken"),
        Locker("lockers-maintenance", "LockersMaintenance", "Maintenance"),
        new("students-without-locker", "HomeCards.Default.StudentsWithoutLocker", HomeCardTarget.Students, new Dictionary<string, string> { [HomeCardCriteria.Locker] = "without" }),
        new("students-pending-payment", "HomeCards.Default.StudentsPending", HomeCardTarget.Students, new Dictionary<string, string> { [HomeCardCriteria.Payment] = "pending" }),
    ];

    static DefaultHomeCard Locker(string key, string title, string status) =>
        new(key, "HomeCards.Default." + title, HomeCardTarget.LockerMap, new Dictionary<string, string> { [HomeCardCriteria.Status] = status });
}
