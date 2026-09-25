// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Assignments;

/// <summary>Error codes of the assignments. Resource keys: Assignments.Error.&lt;Name&gt;.</summary>
public static class AssignmentErrors
{
    /// <summary>The student is retired, so they cannot be given a locker.</summary>
    public static readonly Error StudentRetired = new("Assignments.StudentRetired");

    /// <summary>The student has no enrolment in the active year.</summary>
    public static readonly Error StudentNotEnrolled = new("Assignments.StudentNotEnrolled");

    /// <summary>The student already holds a locker; the way to give them another is to change it.</summary>
    public static readonly Error StudentHasLocker = new("Assignments.StudentHasLocker");

    /// <summary>The student already has a locker reserved.</summary>
    public static readonly Error StudentHasReservation = new("Assignments.StudentHasReservation");

    /// <summary>The locker is retired or out of service.</summary>
    public static readonly Error LockerUnavailable = new("Assignments.LockerUnavailable");

    /// <summary>Another student already holds the locker.</summary>
    public static readonly Error LockerOccupied = new("Assignments.LockerOccupied");

    /// <summary>The locker is reserved with no student, so the reservation has to be removed before assigning it.</summary>
    public static readonly Error LockerReserved = new("Assignments.LockerReserved");

    /// <summary>The locker is reserved for a different student.</summary>
    public static readonly Error LockerReservedForOther = new("Assignments.LockerReservedForOther");

    /// <summary>The student holds no locker, so there is nothing to change or release.</summary>
    public static readonly Error NoAssignment = new("Assignments.NoAssignment");

    /// <summary>The chosen locker is the one the student already holds.</summary>
    public static readonly Error SameLocker = new("Assignments.SameLocker");

    /// <summary>The assignment is already closed.</summary>
    public static readonly Error AlreadyClosed = new("Assignments.AlreadyClosed");

    /// <summary>There is no free locker in the whole centre.</summary>
    public static readonly Error NoFreeLockers = new("Assignments.NoFreeLockers");

    /// <summary>The note is longer than allowed. Args: {0} maximum length.</summary>
    public static Error NoteTooLong(int maximum) => new("Assignments.NoteTooLong", Args: [maximum]);

    public static readonly Error NotFound = new("Assignments.NotFound");
}
