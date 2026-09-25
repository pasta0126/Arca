// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Catalog;
using Arca.Application.Enrollments;
using Arca.Application.Lockers;
using Arca.Application.SchoolYears;
using Arca.Application.Students;
using Arca.Application.Zones;

namespace Arca.Application.Assignments;

/// <summary>
/// Everything the use cases of assignments share: the repositories they read and write and the hooks of other capabilities
/// (guards, opened and closed handlers). It builds the flow that gives them all the same validation.
/// </summary>
public sealed class AssignmentServices(
    IAssignmentRepository assignments, IStudentRepository students, ILockerRepository lockers, IZoneRepository zones,
    IEnrollmentRepository enrollments, IAcademicYearRepository years, IStudentEventRepository studentEvents,
    ILockerEventRepository lockerEvents, IEnumerable<IAssignmentGuard> guards, IEnumerable<IAssignmentOpenedHandler> opened,
    IEnumerable<IAssignmentClosedHandler> closed)
{
    public IAssignmentRepository Assignments { get; } = assignments;

    public IStudentRepository Students { get; } = students;

    public ILockerRepository Lockers { get; } = lockers;

    public IAcademicYearRepository Years { get; } = years;

    public ILockerEventRepository LockerEvents { get; } = lockerEvents;

    public IStudentEventRepository StudentEvents { get; } = studentEvents;

    internal AssignmentFlow Flow { get; } = new(
        assignments, students, lockers, zones, enrollments, years, studentEvents, lockerEvents, guards, opened, closed);
}
