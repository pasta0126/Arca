// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Lockers;

/// <summary>Error codes of the lockers. Resource keys: Lockers.Error.&lt;Name&gt;.</summary>
public static class LockerErrors
{
    /// <summary>Not a whole number between the limits. Args: {0} lowest, {1} highest.</summary>
    public static Error NumberInvalid(int lowest, int highest) => new("Lockers.NumberInvalid", Args: [lowest, highest]);

    /// <summary>Another locker that is not retired already has the number. Args: {0} the number.</summary>
    public static Error NumberInUse(int number) => new("Lockers.NumberInUse", Args: [number]);

    /// <summary>The note is longer than allowed. Args: {0} maximum length.</summary>
    public static Error NoteTooLong(int maximum) => new("Lockers.NoteTooLong", Args: [maximum]);

    /// <summary>The zone is deactivated, so it takes no lockers.</summary>
    public static readonly Error ZoneUnavailable = new("Lockers.ZoneUnavailable");

    /// <summary>The locker is retired, and a retired locker cannot be changed.</summary>
    public static readonly Error Retired = new("Lockers.Retired");

    /// <summary>The locker is not free (it is occupied, reserved or out of service), so it cannot be reserved.</summary>
    public static readonly Error NotFree = new("Lockers.NotFree");

    /// <summary>The locker is not reserved.</summary>
    public static readonly Error NotReserved = new("Lockers.NotReserved");

    /// <summary>The locker is already out of service with that kind.</summary>
    public static readonly Error AlreadyOutOfService = new("Lockers.AlreadyOutOfService");

    /// <summary>The locker is in service.</summary>
    public static readonly Error NotOutOfService = new("Lockers.NotOutOfService");

    /// <summary>It has an assignment; it must be released before retiring the locker.</summary>
    public static readonly Error HasAssignment = new("Lockers.HasAssignment");

    /// <summary>It is reserved; the reservation must be removed before retiring the locker.</summary>
    public static readonly Error HasReservation = new("Lockers.HasReservation");

    /// <summary>The option needs the assignments, which do not exist yet. Args: {0} the option.</summary>
    public static Error DecisionNotAvailable(OutOfServiceDecision decision) =>
        new("Lockers.DecisionNotAvailable", Args: [decision.ToString()]);

    /// <summary>The new value is the one it already has.</summary>
    public static readonly Error Unchanged = new("Lockers.Unchanged");

    /// <summary>There is no locker with that identity.</summary>
    public static readonly Error NotFound = new("Lockers.NotFound");

    /// <summary>The first number of a range is greater than the last.</summary>
    public static readonly Error RangeInvalid = new("Lockers.RangeInvalid");

    /// <summary>A range has more lockers than one operation may create. Args: {0} the maximum.</summary>
    public static Error RangeTooLarge(int maximum) => new("Lockers.RangeTooLarge", Args: [maximum]);
}
