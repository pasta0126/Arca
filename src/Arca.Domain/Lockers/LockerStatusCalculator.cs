// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.Lockers;

/// <summary>
/// The only place that computes the status of a locker (taquilles-i-zones, D1), with this precedence: retired, out of
/// service, occupied, reserved, free. A pure function, so it is tested with the whole table of combinations.
/// </summary>
public static class LockerStatusCalculator
{
    public static LockerState Calculate(LockerFacts facts) => new(StatusOf(facts), facts.HasAssignment, facts.IsReserved);

    static LockerStatus StatusOf(LockerFacts facts) =>
        facts.IsRetired ? LockerStatus.Retired
        : facts.OutOfService == OutOfServiceKind.Broken ? LockerStatus.Broken
        : facts.OutOfService == OutOfServiceKind.Maintenance ? LockerStatus.Maintenance
        : facts.HasAssignment ? LockerStatus.Occupied
        : facts.IsReserved ? LockerStatus.Reserved
        : LockerStatus.Free;
}
