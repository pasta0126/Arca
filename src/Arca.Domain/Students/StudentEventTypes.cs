// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.Students;

/// <summary>
/// The types of history event of a student (alumnes-i-assignacions, D1). They are stable codes, never translated text.
/// The assignments add their own when they open, change or close.
/// </summary>
public static class StudentEventTypes
{
    public const string Created = "Student.Created";
    public const string DataChanged = "Student.DataChanged";
    public const string Enrolled = "Student.Enrolled";
    public const string EnrollmentChanged = "Student.EnrollmentChanged";
    public const string Retired = "Student.Retired";
    public const string Reactivated = "Student.Reactivated";
    public const string AssignmentOpened = "Student.AssignmentOpened";
    public const string AssignmentClosed = "Student.AssignmentClosed";
    public const string LockerReserved = "Student.LockerReserved";
    public const string LockerReservationRemoved = "Student.LockerReservationRemoved";

    public static IReadOnlyList<string> All { get; } = [Created, DataChanged, Enrolled, EnrollmentChanged, Retired, Reactivated, AssignmentOpened, AssignmentClosed, LockerReserved, LockerReservationRemoved];
}
