// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Assignments;
using Arca.Application.Catalog;
using Arca.Application.Charges;
using Arca.Application.ConceptAmounts;
using Arca.Application.Enrollments;
using Arca.Application.Lockers;
using Arca.Application.Students;
using Arca.Application.SchoolYears;
using Arca.Application.Zones;
using Arca.Domain.Common;
using Arca.Domain.Assignments;
using Arca.Domain.Catalog;
using Arca.Domain.Charges;
using Arca.Domain.ConceptAmounts;
using Arca.Domain.Enrollments;
using Arca.Domain.Lockers;
using Arca.Domain.Students;
using Arca.Domain.SchoolYears;
using Arca.Domain.Zones;

namespace Arca.Testing.Inventory;

/// <summary>
/// The zones, lockers and events of a test, kept in memory, with the ports the use cases need. As a unit of work it
/// behaves like a transaction: what a failed or throwing operation changed is undone.
/// </summary>
public sealed class InMemoryInventory : IUnitOfWork
{
    public InMemoryInventory()
    {
        Zones = new ZoneRepository(this);
        Lockers = new LockerRepository(this);
        Events = new EventRepository(this);
        Years = new YearRepository(this);
        Students = new StudentRepository(this);
        Enrollments = new EnrollmentRepository(this);
        Catalog = new CatalogRepository(this);
        StudentEvents = new StudentEventRepository(this);
        Assignments = new AssignmentRepository(this);
        ConceptAmounts = new ConceptAmountRepository(this);
        ConceptAmountEvents = new ConceptAmountEventRepository(this);
        Charges = new ChargeRepository(this);
        ChargeEvents = new ChargeEventRepository(this);
    }

    public List<Zone> ZoneList { get; private set; } = [];

    public List<Locker> LockerList { get; private set; } = [];

    public List<HistoryEvent> EventList { get; private set; } = [];

    public List<AcademicYear> YearList { get; private set; } = [];

    public List<Student> StudentList { get; private set; } = [];

    public List<Enrollment> EnrollmentList { get; private set; } = [];

    public List<Level> LevelList { get; private set; } = [];

    public List<Group> GroupList { get; private set; } = [];

    public List<HistoryEvent> StudentEventList { get; private set; } = [];

    public List<Assignment> AssignmentList { get; private set; } = [];

    public List<ConceptAmount> ConceptAmountList { get; private set; } = [];

    public List<HistoryEvent> ConceptAmountEventList { get; private set; } = [];

    public List<Charge> ChargeList { get; private set; } = [];

    public List<HistoryEvent> ChargeEventList { get; private set; } = [];

    /// <summary>The locker each student holds, until the assignments exist. A test sets it.</summary>
    public ConfigurableStudentLockers StudentLockers { get; } = new();

    /// <summary>The years a test marks as holding enrolments or assignments, until those exist.</summary>
    public HashSet<Guid> YearsWithData { get; } = [];

    public IZoneRepository Zones { get; }

    public ILockerRepository Lockers { get; }

    public ILockerEventRepository Events { get; }

    public IAcademicYearRepository Years { get; }

    public IStudentRepository Students { get; }

    public IEnrollmentRepository Enrollments { get; }

    public ICatalogRepository Catalog { get; }

    public IStudentEventRepository StudentEvents { get; }

    public IAssignmentRepository Assignments { get; }

    public IConceptAmountRepository ConceptAmounts { get; }

    public IConceptAmountEventRepository ConceptAmountEvents { get; }

    public IChargeRepository Charges { get; }

    public IChargeEventRepository ChargeEvents { get; }

    /// <summary>Which lockers a student holds. The real one comes with the assignments.</summary>
    public ConfigurableOccupancy Occupancy { get; } = new();

    /// <summary>Makes the next saved change fail, so a test can prove that nothing stays half done.</summary>
    public bool FailOnNextEvent { get; set; }

    /// <summary>Lets this many events be saved and makes the next one fail: a failure in the middle of a bulk save.</summary>
    public int? EventsBeforeFailure { get; set; }

    void FailIfScheduled()
    {
        if (FailOnNextEvent || EventsBeforeFailure == 0)
        {
            FailOnNextEvent = false;
            EventsBeforeFailure = null;
            throw new IOException("the disk failed while saving");
        }

        if (EventsBeforeFailure is > 0)
        {
            EventsBeforeFailure--;
        }
    }

    public async Task<Result<T>> RunAsync<T>(Func<CancellationToken, Task<Result<T>>> work, CancellationToken ct)
    {
        var zones = ZoneList.Select(z => new Zone(z.Id, z.Name, z.NameKey, z.IsActive)).ToList();
        var lockers = LockerList.Select(Copy).ToList();
        var events = EventList.ToList();
        var students = StudentList.Select(x => new Student(x.Id, x.FirstName, x.LastName, x.Email, x.NameKey, x.RetiredAtUtc, x.RetirementReason)).ToList();
        var enrollments = EnrollmentList.Select(x => new Enrollment(x.Id, x.StudentId, x.YearId, x.LevelId, x.GroupId)).ToList();
        var levels = LevelList.ToList();
        var groups = GroupList.ToList();
        var studentEvents = StudentEventList.ToList();
        var assignments = AssignmentList.Select(a => new Assignment(a.Id, a.StudentId, a.LockerId, a.YearId, a.StartedAtUtc, a.EndedAtUtc, a.CloseReason, a.CloseNote)).ToList();
        var years = YearList.Select(y => AcademicYear.Restore(y.Id, y.StartDate, y.EndDate, y.IsActive)).ToList();
        var conceptAmounts = ConceptAmountList.Select(a => new ConceptAmount(a.Id, a.YearId, a.Concept, a.Amount)).ToList();
        var conceptAmountEvents = ConceptAmountEventList.ToList();
        var charges = ChargeList.Select(c => new Charge(c.Id, c.StudentId, c.Concept, c.YearId, c.Amount, c.Status, c.PaidOn, c.Reason, c.Return, c.ReturnedOn, c.ReturnNote)).ToList();
        var chargeEvents = ChargeEventList.ToList();
        Result<T> result;
        try
        {
            result = await work(ct);
        }
        catch
        {
            (ZoneList, LockerList, EventList, YearList) = (zones, lockers, events, years);
            (StudentList, EnrollmentList, LevelList, GroupList, StudentEventList, AssignmentList) = (students, enrollments, levels, groups, studentEvents, assignments);
            (ConceptAmountList, ConceptAmountEventList) = (conceptAmounts, conceptAmountEvents);
            (ChargeList, ChargeEventList) = (charges, chargeEvents);
            throw;
        }

        if (!result.IsSuccess)
        {
            (ZoneList, LockerList, EventList, YearList) = (zones, lockers, events, years);
            (StudentList, EnrollmentList, LevelList, GroupList, StudentEventList, AssignmentList) = (students, enrollments, levels, groups, studentEvents, assignments);
            (ConceptAmountList, ConceptAmountEventList) = (conceptAmounts, conceptAmountEvents);
            (ChargeList, ChargeEventList) = (charges, chargeEvents);
        }

        return result;
    }

    sealed class YearRepository(InMemoryInventory owner) : IAcademicYearRepository
    {
        public Task<IReadOnlyList<AcademicYear>> ListAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<AcademicYear>>([.. owner.YearList]);

        public Task<AcademicYear?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(owner.YearList.FirstOrDefault(y => y.Id == id));

        public Task<AcademicYear?> GetActiveAsync(CancellationToken ct) => Task.FromResult(owner.YearList.FirstOrDefault(y => y.IsActive));

        public Task AddAsync(AcademicYear year, CancellationToken ct)
        {
            owner.YearList.Add(year);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(AcademicYear year, CancellationToken ct) => Task.CompletedTask;

        public Task RemoveAsync(AcademicYear year, CancellationToken ct)
        {
            owner.YearList.Remove(year);
            return Task.CompletedTask;
        }

        public Task<bool> HasDataAsync(Guid yearId, CancellationToken ct) => Task.FromResult(owner.YearsWithData.Contains(yearId));
    }

    sealed class StudentRepository(InMemoryInventory owner) : IStudentRepository
    {
        public Task<IReadOnlyList<Student>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Student>>([.. owner.StudentList]);

        public Task<Student?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(owner.StudentList.FirstOrDefault(s => s.Id == id));

        public Task AddAsync(Student student, CancellationToken ct)
        {
            owner.StudentList.Add(student);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Student student, CancellationToken ct) => Task.CompletedTask;
    }

    sealed class EnrollmentRepository(InMemoryInventory owner) : IEnrollmentRepository
    {
        public Task<IReadOnlyList<Enrollment>> ListByYearAsync(Guid yearId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Enrollment>>([.. owner.EnrollmentList.Where(e => e.YearId == yearId)]);

        public Task<Enrollment?> GetAsync(Guid studentId, Guid yearId, CancellationToken ct) =>
            Task.FromResult(owner.EnrollmentList.FirstOrDefault(e => e.StudentId == studentId && e.YearId == yearId));

        public Task AddAsync(Enrollment enrollment, CancellationToken ct)
        {
            owner.EnrollmentList.Add(enrollment);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Enrollment enrollment, CancellationToken ct) => Task.CompletedTask;
    }

    sealed class CatalogRepository(InMemoryInventory owner) : ICatalogRepository
    {
        public Task<IReadOnlyList<Level>> ListLevelsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Level>>([.. owner.LevelList]);

        public Task<IReadOnlyList<Group>> ListGroupsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Group>>([.. owner.GroupList]);

        public Task AddLevelAsync(Level level, CancellationToken ct)
        {
            owner.LevelList.Add(level);
            return Task.CompletedTask;
        }

        public Task AddGroupAsync(Group group, CancellationToken ct)
        {
            owner.GroupList.Add(group);
            return Task.CompletedTask;
        }
    }

    sealed class StudentEventRepository(InMemoryInventory owner) : IStudentEventRepository
    {
        public Task AddAsync(HistoryEvent change, CancellationToken ct)
        {
            owner.StudentEventList.Add(change);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<HistoryEvent>> ListAsync(Guid studentId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<HistoryEvent>>([.. Enumerable.Reverse(owner.StudentEventList).Where(e => e.EntityId == studentId).OrderByDescending(e => e.OccurredAtUtc)]);
    }

    sealed class AssignmentRepository(InMemoryInventory owner) : IAssignmentRepository
    {
        public Task<IReadOnlyList<Assignment>> ListCurrentAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Assignment>>([.. owner.AssignmentList.Where(a => a.IsCurrent)]);

        public Task<Assignment?> GetCurrentOfStudentAsync(Guid studentId, CancellationToken ct) =>
            Task.FromResult(owner.AssignmentList.FirstOrDefault(a => a.IsCurrent && a.StudentId == studentId));

        public Task<Assignment?> GetCurrentOfLockerAsync(Guid lockerId, CancellationToken ct) =>
            Task.FromResult(owner.AssignmentList.FirstOrDefault(a => a.IsCurrent && a.LockerId == lockerId));

        public Task<IReadOnlyList<Assignment>> ListByStudentAsync(Guid studentId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Assignment>>([.. owner.AssignmentList.Where(a => a.StudentId == studentId).OrderByDescending(a => a.StartedAtUtc)]);

        public Task<IReadOnlyList<Assignment>> ListByLockerAsync(Guid lockerId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Assignment>>([.. owner.AssignmentList.Where(a => a.LockerId == lockerId).OrderByDescending(a => a.StartedAtUtc)]);

        public Task AddAsync(Assignment assignment, CancellationToken ct)
        {
            owner.AssignmentList.Add(assignment);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Assignment assignment, CancellationToken ct) => Task.CompletedTask;
    }

    sealed class ConceptAmountRepository(InMemoryInventory owner) : IConceptAmountRepository
    {
        public Task<IReadOnlyList<ConceptAmount>> ListByYearAsync(Guid yearId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ConceptAmount>>([.. owner.ConceptAmountList.Where(a => a.YearId == yearId)]);

        public Task AddAsync(ConceptAmount amount, CancellationToken ct)
        {
            owner.ConceptAmountList.Add(amount);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ConceptAmount amount, CancellationToken ct) => Task.CompletedTask;
    }

    sealed class ConceptAmountEventRepository(InMemoryInventory owner) : IConceptAmountEventRepository
    {
        public Task AddAsync(HistoryEvent change, CancellationToken ct)
        {
            owner.ConceptAmountEventList.Add(change);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<HistoryEvent>> ListAsync(Guid conceptAmountId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<HistoryEvent>>(
                [.. Enumerable.Reverse(owner.ConceptAmountEventList).Where(e => e.EntityId == conceptAmountId).OrderByDescending(e => e.OccurredAtUtc)]);
    }

    sealed class ChargeRepository(InMemoryInventory owner) : IChargeRepository
    {
        public Task<Charge?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(owner.ChargeList.FirstOrDefault(c => c.Id == id));

        public Task<IReadOnlyList<Charge>> ListByStudentAsync(Guid studentId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Charge>>([.. owner.ChargeList.Where(c => c.StudentId == studentId)]);

        public Task<IReadOnlyList<Charge>> ListPendingAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Charge>>([.. owner.ChargeList.Where(c => c.CountsAsDebt)]);

        public Task<IReadOnlyList<Charge>> ListDepositsDueBackAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Charge>>([.. owner.ChargeList.Where(c => c.IsDueBack)]);

        public Task AddAsync(Charge charge, CancellationToken ct)
        {
            owner.ChargeList.Add(charge);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Charge charge, CancellationToken ct) => Task.CompletedTask;
    }

    sealed class ChargeEventRepository(InMemoryInventory owner) : IChargeEventRepository
    {
        public Task AddAsync(HistoryEvent change, CancellationToken ct)
        {
            owner.FailIfScheduled();
            owner.ChargeEventList.Add(change);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<HistoryEvent>> ListAsync(Guid chargeId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<HistoryEvent>>(
                [.. Enumerable.Reverse(owner.ChargeEventList).Where(e => e.EntityId == chargeId).OrderByDescending(e => e.OccurredAtUtc)]);
    }

    static Locker Copy(Locker l) =>
        new(l.Id, l.Number, l.ZoneId, l.Note, l.OutOfService, l.IsReserved, l.ReservationNote, l.RetiredAtUtc, l.ReservedForStudentId);

    sealed class ZoneRepository(InMemoryInventory owner) : IZoneRepository
    {
        public Task<IReadOnlyList<Zone>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Zone>>([.. owner.ZoneList]);

        public Task<Zone?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(owner.ZoneList.FirstOrDefault(z => z.Id == id));

        public Task AddAsync(Zone zone, CancellationToken ct)
        {
            owner.ZoneList.Add(zone);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Zone zone, CancellationToken ct) => Task.CompletedTask;

        public Task RemoveAsync(Zone zone, CancellationToken ct)
        {
            owner.ZoneList.Remove(zone);
            return Task.CompletedTask;
        }
    }

    sealed class LockerRepository(InMemoryInventory owner) : ILockerRepository
    {
        public Task<IReadOnlyList<Locker>> ListAsync(bool includeRetired, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Locker>>([.. owner.LockerList.Where(l => includeRetired || !l.IsRetired)]);

        public Task<Locker?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(owner.LockerList.FirstOrDefault(l => l.Id == id));

        public Task AddAsync(Locker locker, CancellationToken ct)
        {
            owner.LockerList.Add(locker);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Locker locker, CancellationToken ct) => Task.CompletedTask;

        public Task<int> CountActiveInZoneAsync(Guid zoneId, CancellationToken ct) =>
            Task.FromResult(owner.LockerList.Count(l => l.ZoneId == zoneId && !l.IsRetired));

        public Task<bool> HasEverHadLockersAsync(Guid zoneId, CancellationToken ct) =>
            Task.FromResult(owner.LockerList.Any(l => l.ZoneId == zoneId));
    }

    sealed class EventRepository(InMemoryInventory owner) : ILockerEventRepository
    {
        public Task AddAsync(HistoryEvent change, CancellationToken ct)
        {
            owner.FailIfScheduled();
            owner.EventList.Add(change);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<HistoryEvent>> ListAsync(Guid lockerId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<HistoryEvent>>([.. Enumerable.Reverse(owner.EventList).Where(e => e.EntityId == lockerId).OrderByDescending(e => e.OccurredAtUtc)]);
    }
}
