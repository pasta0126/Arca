// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Assignments.ChangeStudentLocker;
using Arca.Application.Assignments.ReleaseStudentLocker;
using Arca.Application.Students;
using Arca.Application.Tests.Inventory;
using Arca.Application.Tests.Students;
using Arca.Testing;
using Arca.Testing.Inventory;

namespace Arca.Application.Tests.Assignments;

/// <summary>Zones, lockers, students and assignments over one in-memory inventory with the real occupancy derived from the assignments.</summary>
public sealed class AssignmentsWorld
{
    public AssignmentsWorld()
    {
        Store = new InMemoryInventory();
        Clock = new FakeClock(new DateTimeOffset(2026, 9, 25, 9, 0, 0, TimeSpan.Zero));
        var occupancy = new AssignmentOccupancy(Store.Assignments, Store.Lockers);
        Inventory = new InventoryWorld(Store, Clock, occupancy);
        Students = new StudentsWorld(Store, Clock, occupancy);
    }

    public InMemoryInventory Store { get; }

    public FakeClock Clock { get; }

    public InventoryWorld Inventory { get; }

    public StudentsWorld Students { get; }

    public AssignLockerHandler Assign => new(Students.Services, Store, Clock);

    public ChangeStudentLockerHandler Change => new(Students.Services, Store, Clock);

    public ReleaseStudentLockerHandler Release => new(Students.Services, Store, Clock);

    public Task<Guid> ZoneAsync(string name) => Inventory.ZoneAsync(name);

    public Task<Guid> LockerAsync(int number, Guid zoneId) => Inventory.LockerAsync(number, zoneId);

    public Task<StudentDetail> StudentAsync(string first, string last, string email, string level = "1r ESO", string? group = "A") =>
        Students.StudentAsync(first, last, email, level, group);

    public async Task<AssignLockerResult> AssignAsync(Guid studentId, Guid lockerId, bool confirm = false)
    {
        Clock.Advance(TimeSpan.FromMinutes(1));
        return (await Assign.HandleAsync(new AssignLockerRequest(studentId, lockerId, confirm), default)).Value!;
    }
}
