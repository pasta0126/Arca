// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Lockers.CreateLockerRange;

/// <summary>
/// The preview of a range (docs/convenciones.md, section 5): what would be created and what collides, computed without
/// saving anything. It is immutable; confirming it revalidates against the data as it is then.
/// </summary>
/// <param name="First">First number of the range.</param>
/// <param name="Last">Last number of the range.</param>
/// <param name="ZoneId">The zone where the lockers would go.</param>
/// <param name="ZoneName">Its name, for the confirmation message.</param>
/// <param name="ToCreate">The numbers that would be created, in order. Empty if there are conflicts.</param>
/// <param name="Conflicts">All the numbers of the range that an active locker already has.</param>
public sealed record CreateLockerRangePlan(
    int First, int Last, Guid ZoneId, string ZoneName, IReadOnlyList<int> ToCreate, IReadOnlyList<int> Conflicts)
{
    /// <summary>How many lockers the range has, whether or not they can be created.</summary>
    public int Count => Last - First + 1;

    public bool HasConflicts => Conflicts.Count > 0;

    public CreateLockerRangeRequest ToRequest() => new(First, Last, ZoneId);
}
