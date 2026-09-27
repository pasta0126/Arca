// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Assignments.ChangeStudentLocker;
using Arca.Application.Assignments.ReleaseStudentLocker;
using Arca.Application.Lockers.MarkLockerOutOfService;
using Arca.Application.Lockers.ReserveLocker;
using Arca.Application.Students.RetireStudent;
using Arca.Domain.Assignments;
using Arca.Domain.Common;
using Arca.Domain.Lockers;
using Arca.Domain.Students;
using Xunit;

namespace Arca.Application.Tests.Assignments;

public sealed class AssignLockerTests
{
    const string Spec = "alumnes-i-assignacions/assignacions";

    static async Task<(AssignmentsWorld World, Guid Zone)> WorldAsync()
    {
        var world = new AssignmentsWorld();
        await world.Students.CreateYearAsync();
        return (world, await world.ZoneAsync("Planta 1"));
    }

    // --- Assign ---

    [Fact]
    [Trait("spec", Spec + ": Asignación uno a uno en el curso activo (Asignación correcta)")]
    public async Task Assigning_a_free_locker_makes_it_occupied_and_records_both_histories()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(15, zone);

        var result = await world.AssignAsync(student.Id, locker);

        var row = result.Assignment!;
        Assert.Equal("Núria García", row.StudentName);
        Assert.Equal(15, row.LockerNumber);
        Assert.Equal("Planta 1", row.ZoneName);
        Assert.Equal("2026-2027", row.YearName);
        Assert.True(row.IsCurrent);
        Assert.Equal(LockerStatus.Occupied, (await world.Inventory.RowAsync(locker)).Status);
        Assert.Contains(world.Store.StudentEventList, e => e.EntityId == student.Id && e.Type == StudentEventTypes.AssignmentOpened);
        Assert.Contains(world.Store.EventList, e => e.EntityId == locker && e.Type == LockerEventTypes.Assigned);
    }

    [Fact]
    [Trait("spec", Spec + ": Asignación uno a uno en el curso activo (Alumno que ya tiene taquilla)")]
    public async Task A_student_with_a_locker_and_an_occupied_locker_are_both_refused()
    {
        var (world, zone) = await WorldAsync();
        var first = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var second = await world.StudentAsync("Pau", "Serra", "pau@test.cat");
        var lockerA = await world.LockerAsync(1, zone);
        var lockerB = await world.LockerAsync(2, zone);
        await world.AssignAsync(first.Id, lockerA);

        var hasLocker = await world.Assign.HandleAsync(new AssignLockerRequest(first.Id, lockerB), default);
        var occupied = await world.Assign.HandleAsync(new AssignLockerRequest(second.Id, lockerA), default);

        Assert.Equal("Assignments.StudentHasLocker", hasLocker.Error!.Code);
        Assert.Equal("Assignments.LockerOccupied", occupied.Error!.Code);
        Assert.Single(world.Store.AssignmentList);
    }

    [Fact]
    [Trait("spec", Spec + ": Asignación uno a uno en el curso activo (Alumno de baja)")]
    public async Task A_retired_student_or_one_without_an_enrolment_or_an_unknown_one_is_refused()
    {
        var (world, zone) = await WorldAsync();
        var retired = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        await world.Students.Retire.HandleAsync(new RetireStudentRequest(retired.Id, "Trasllat"), default);
        var locker = await world.LockerAsync(1, zone);
        var orphan = Student.Create(Guid.NewGuid(), "Sense", "Matrícula", "sense@test.cat", [], world.Clock.UtcNow).Value!.Student;
        await world.Store.Students.AddAsync(orphan, default);

        var byRetired = await world.Assign.HandleAsync(new AssignLockerRequest(retired.Id, locker), default);
        var byOrphan = await world.Assign.HandleAsync(new AssignLockerRequest(orphan.Id, locker), default);
        var unknownStudent = await world.Assign.HandleAsync(new AssignLockerRequest(Guid.NewGuid(), locker), default);
        var unknownLocker = await world.Assign.HandleAsync(new AssignLockerRequest(orphan.Id, Guid.NewGuid()), default);

        Assert.Equal("Assignments.StudentRetired", byRetired.Error!.Code);
        Assert.Equal("Assignments.StudentNotEnrolled", byOrphan.Error!.Code);
        Assert.Equal("Students.NotFound", unknownStudent.Error!.Code);
        Assert.Equal("Lockers.NotFound", unknownLocker.Error!.Code);
    }

    [Fact]
    [Trait("spec", "alumnes-i-assignacions/curs-escolar: Un solo curso activo (Sin curso activo)")]
    public async Task Without_an_active_year_nothing_is_assigned()
    {
        var world = new AssignmentsWorld();

        var result = await world.Assign.HandleAsync(new AssignLockerRequest(Guid.NewGuid(), Guid.NewGuid()), default);

        Assert.Equal("SchoolYears.NoActiveYear", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Taquillas asignables (Taquilla averiada)")]
    public async Task A_broken_locker_and_a_reserved_one_are_refused_with_their_own_reasons()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var broken = await world.LockerAsync(1, zone);
        var reserved = await world.LockerAsync(2, zone);
        await world.Inventory.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(broken, OutOfServiceKind.Broken), default);
        await world.Inventory.ReserveLocker.HandleAsync(new ReserveLockerRequest(reserved, "Professorat"), default);

        var onBroken = await world.Assign.HandleAsync(new AssignLockerRequest(student.Id, broken), default);
        var onReserved = await world.Assign.HandleAsync(new AssignLockerRequest(student.Id, reserved), default);

        Assert.Equal("Assignments.LockerUnavailable", onBroken.Error!.Code);
        Assert.Equal("Assignments.LockerReserved", onReserved.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Taquillas asignables (Taquilla reservada para el mismo alumno)")]
    public async Task A_locker_reserved_for_the_student_is_assigned_and_the_reservation_consumed_with_its_event()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var other = await world.StudentAsync("Pau", "Serra", "pau@test.cat");
        var locker = await world.LockerAsync(1, zone);
        var stored = world.Store.LockerList.Single();
        stored.ReserveForStudent(student.Id, "Per a la Núria", hasAssignment: false, world.Clock.UtcNow);

        var forOther = await world.Assign.HandleAsync(new AssignLockerRequest(other.Id, locker), default);
        var result = await world.AssignAsync(student.Id, locker);

        Assert.Equal("Assignments.LockerReservedForOther", forOther.Error!.Code);
        Assert.NotNull(result.Assignment);
        stored = world.Store.LockerList.Single(); // the refused attempt rolled back and reloaded the store
        Assert.False(stored.IsReserved);
        Assert.Contains(world.Store.EventList, e => e.EntityId == locker && e.Type == LockerEventTypes.ReservationConsumed);
    }

    // --- Warnings and blockers of other capabilities ---

    sealed class FixedGuard(params AssignmentFinding[] findings) : IAssignmentGuard
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyList<AssignmentFinding>> CheckAsync(ProposedAssignment proposal, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<AssignmentFinding>>(findings);
        }
    }

    [Fact]
    [Trait("spec", Spec + ": Avisos que exigen confirmación (Asignación con aviso)")]
    public async Task A_warning_asks_for_confirmation_and_assigns_nothing_until_it_is_given()
    {
        var (world, zone) = await WorldAsync();
        var guard = new FixedGuard(new AssignmentFinding(AssignmentFindingKind.Warning, "Charges.PriorDebt", [120]));
        world.Students.Guards.Add(guard);
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(1, zone);

        var asked = await world.AssignAsync(student.Id, locker);

        Assert.True(asked.NeedsConfirmation);
        var warning = Assert.Single(asked.Warnings);
        Assert.Equal("Charges.PriorDebt", warning.Code);
        Assert.Equal(120, Assert.Single(warning.Args));
        Assert.Empty(world.Store.AssignmentList);

        var confirmed = await world.AssignAsync(student.Id, locker, confirm: true);

        Assert.False(confirmed.NeedsConfirmation);
        Assert.Single(world.Store.AssignmentList);
    }

    [Fact]
    [Trait("spec", Spec + ": Avisos que exigen confirmación (Sin avisos)")]
    public async Task With_no_findings_the_assignment_is_made_without_asking_and_the_guards_were_consulted()
    {
        var (world, zone) = await WorldAsync();
        var guard = new FixedGuard();
        world.Students.Guards.Add(guard);
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(1, zone);

        var result = await world.AssignAsync(student.Id, locker);

        Assert.False(result.NeedsConfirmation);
        Assert.Equal(1, guard.Calls);
    }

    [Fact]
    [Trait("spec", Spec + ": Impedimentos de otras capacidades (Impedimento y aviso a la vez)")]
    public async Task A_blocker_refuses_the_assignment_without_offering_to_confirm_even_with_a_warning_and_with_confirmation()
    {
        var (world, zone) = await WorldAsync();
        world.Students.Guards.Add(new FixedGuard(
            new AssignmentFinding(AssignmentFindingKind.Warning, "Charges.PriorDebt"),
            new AssignmentFinding(AssignmentFindingKind.Blocker, "Keys.NoKeyAvailable", [15])));
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(1, zone);

        var without = await world.Assign.HandleAsync(new AssignLockerRequest(student.Id, locker), default);
        var confirmed = await world.Assign.HandleAsync(new AssignLockerRequest(student.Id, locker, ConfirmWarnings: true), default);

        Assert.Equal("Keys.NoKeyAvailable", without.Error!.Code);
        Assert.Equal(15, Assert.Single(without.Error.Args));
        Assert.Equal("Keys.NoKeyAvailable", confirmed.Error!.Code);
        Assert.Empty(world.Store.AssignmentList);
    }

    [Fact]
    [Trait("spec", Spec + ": Avisos que exigen confirmación (Aviso rechazado)")]
    public async Task A_warning_that_is_rejected_leaves_no_assignment()
    {
        var (world, zone) = await WorldAsync();
        world.Students.Guards.Add(new FixedGuard(new AssignmentFinding(AssignmentFindingKind.Warning, "Charges.PriorDebt")));
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(1, zone);
        var confirmations = new Arca.Testing.RecordingConfirmations(answer: false);

        var asked = await world.AssignAsync(student.Id, locker);
        var accepted = await confirmations.ConfirmAsync(new Arca.Application.Feedback.ConfirmationRequest("t", "c", "ok"));
        if (accepted)
        {
            await world.AssignAsync(student.Id, locker, confirm: true);
        }

        Assert.True(asked.NeedsConfirmation);
        Assert.Empty(world.Store.AssignmentList);
    }

    // --- Hooks inside the transaction ---

    sealed class RecordingHooks(AssignmentsWorld world) : IAssignmentOpenedHandler, IAssignmentClosedHandler
    {
        public List<(string Kind, Guid AssignmentId, string? Reason, bool Saved)> Calls { get; } = [];

        public Task HandleAsync(AssignmentHookContext context, CancellationToken ct)
        {
            var kind = context.Assignment.IsCurrent ? "opened" : "closed";
            Calls.Add((kind, context.Assignment.Id, context.Operation.Reason, world.Store.AssignmentList.Any(a => a.Id == context.Assignment.Id)));
            return Task.CompletedTask;
        }
    }

    [Fact]
    [Trait("spec", "alumnes-i-assignacions/design: D9 Ganchos de ciclo de vida")]
    public async Task The_opened_and_closed_hooks_run_inside_the_transaction_with_the_context_of_the_operation()
    {
        var (world, zone) = await WorldAsync();
        var hooks = new RecordingHooks(world);
        world.Students.OpenedHooks.Add(hooks);
        world.Students.ClosedHooks.Add(hooks);
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(1, zone);

        await world.Assign.HandleAsync(new AssignLockerRequest(student.Id, locker, Operation: new OperationContext("alta")), default);
        await world.Release.HandleAsync(new ReleaseStudentLockerRequest(student.Id, new OperationContext("Ha marxat", new Dictionary<string, object?> { ["key"] = "returned" })), default);

        Assert.Equal(["opened", "closed"], hooks.Calls.Select(c => c.Kind));
        Assert.Equal("alta", hooks.Calls[0].Reason);
        Assert.Equal("Ha marxat", hooks.Calls[1].Reason);
        Assert.All(hooks.Calls, c => Assert.True(c.Saved)); // the assignment was already saved when the hook ran
    }

    sealed class FailingOpenedHook : IAssignmentOpenedHandler
    {
        public Task HandleAsync(AssignmentHookContext context, CancellationToken ct) => throw new InvalidOperationException("charges failed");
    }

    [Fact]
    [Trait("spec", "alumnes-i-assignacions/design: D9 Ganchos de ciclo de vida")]
    public async Task If_a_hook_fails_the_whole_assignment_is_undone()
    {
        var (world, zone) = await WorldAsync();
        world.Students.OpenedHooks.Add(new FailingOpenedHook());
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(1, zone);
        var events = world.Store.StudentEventList.Count;

        await Assert.ThrowsAsync<InvalidOperationException>(() => world.Assign.HandleAsync(new AssignLockerRequest(student.Id, locker), default));

        Assert.Empty(world.Store.AssignmentList);
        Assert.Equal(events, world.Store.StudentEventList.Count);
        Assert.Equal(LockerStatus.Free, (await world.Inventory.RowAsync(locker)).Status);
    }

    // --- Change ---

    [Fact]
    [Trait("spec", Spec + ": Cambio de taquilla (Cambio correcto)")]
    public async Task Changing_locker_frees_the_old_one_occupies_the_new_one_and_records_all_the_events()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var old = await world.LockerAsync(1, zone);
        var next = await world.LockerAsync(2, zone);
        await world.AssignAsync(student.Id, old);

        var result = await world.Change.HandleAsync(new ChangeStudentLockerRequest(student.Id, next), default);

        Assert.Equal(2, result.Value!.Assignment!.LockerNumber);
        Assert.Equal(LockerStatus.Free, (await world.Inventory.RowAsync(old)).Status);
        Assert.Equal(LockerStatus.Occupied, (await world.Inventory.RowAsync(next)).Status);
        Assert.Equal(2, world.Store.AssignmentList.Count);
        Assert.Single(world.Store.AssignmentList, a => a.IsCurrent);
        Assert.Equal(AssignmentCloseReason.Changed, world.Store.AssignmentList.Single(a => !a.IsCurrent).CloseReason);
        Assert.Contains(world.Store.EventList, e => e.EntityId == old && e.Type == LockerEventTypes.Released);
        Assert.Contains(world.Store.EventList, e => e.EntityId == next && e.Type == LockerEventTypes.Assigned);
        Assert.Equal(
            [StudentEventTypes.AssignmentOpened, StudentEventTypes.AssignmentClosed, StudentEventTypes.AssignmentOpened],
            world.Store.StudentEventList.Where(e => e.EntityId == student.Id && e.Type.StartsWith("Student.Assignment", StringComparison.Ordinal)).Select(e => e.Type));
    }

    [Fact]
    [Trait("spec", Spec + ": Cambio de taquilla (Destino no asignable)")]
    public async Task A_destination_that_cannot_be_assigned_changes_nothing_and_the_original_stays()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var other = await world.StudentAsync("Pau", "Serra", "pau@test.cat");
        var old = await world.LockerAsync(1, zone);
        var taken = await world.LockerAsync(2, zone);
        var broken = await world.LockerAsync(3, zone);
        await world.AssignAsync(student.Id, old);
        await world.AssignAsync(other.Id, taken);
        await world.Inventory.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(broken, OutOfServiceKind.Broken), default);

        var toTaken = await world.Change.HandleAsync(new ChangeStudentLockerRequest(student.Id, taken), default);
        var toBroken = await world.Change.HandleAsync(new ChangeStudentLockerRequest(student.Id, broken), default);

        Assert.Equal("Assignments.LockerOccupied", toTaken.Error!.Code);
        Assert.Equal("Assignments.LockerUnavailable", toBroken.Error!.Code);
        Assert.Equal(old, world.Store.AssignmentList.Single(a => a.StudentId == student.Id && a.IsCurrent).LockerId);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambio de taquilla (Mismo destino)")]
    public async Task Changing_to_the_locker_the_student_already_has_or_without_one_is_refused()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(1, zone);
        var free = await world.LockerAsync(2, zone);

        var withoutLocker = await world.Change.HandleAsync(new ChangeStudentLockerRequest(student.Id, free), default);
        await world.AssignAsync(student.Id, locker);
        var same = await world.Change.HandleAsync(new ChangeStudentLockerRequest(student.Id, locker), default);
        var unknown = await world.Change.HandleAsync(new ChangeStudentLockerRequest(Guid.NewGuid(), free), default);
        var missingLocker = await world.Change.HandleAsync(new ChangeStudentLockerRequest(student.Id, Guid.NewGuid()), default);

        Assert.Equal("Assignments.NoAssignment", withoutLocker.Error!.Code);
        Assert.Equal("Assignments.SameLocker", same.Error!.Code);
        Assert.Equal("Students.NotFound", unknown.Error!.Code);
        Assert.Equal("Lockers.NotFound", missingLocker.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambio de taquilla (Cambio de curso activo)")]
    public async Task An_assignment_of_a_year_that_is_no_longer_active_cannot_be_changed_or_released()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(1, zone);
        var free = await world.LockerAsync(2, zone);
        await world.AssignAsync(student.Id, locker);
        var year = world.Store.YearList.Single();
        world.Store.YearList[0] = Arca.Domain.SchoolYears.AcademicYear.Restore(year.Id, year.StartDate, year.EndDate, isActive: false);

        var change = await world.Change.HandleAsync(new ChangeStudentLockerRequest(student.Id, free), default);
        var release = await world.Release.HandleAsync(new ReleaseStudentLockerRequest(student.Id), default);

        Assert.Equal("SchoolYears.NotActive", change.Error!.Code);
        Assert.Equal("SchoolYears.NotActive", release.Error!.Code);
        Assert.Single(world.Store.AssignmentList, a => a.IsCurrent);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambio de taquilla (Cambio correcto)")]
    public async Task Changing_can_warn_about_the_new_locker_and_only_moves_the_student_once_confirmed()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var old = await world.LockerAsync(1, zone);
        var next = await world.LockerAsync(2, zone);
        await world.AssignAsync(student.Id, old);
        world.Students.Guards.Add(new FixedGuard(new AssignmentFinding(AssignmentFindingKind.Warning, "Charges.PriorDebt")));

        var asked = await world.Change.HandleAsync(new ChangeStudentLockerRequest(student.Id, next), default);
        Assert.True(asked.Value!.NeedsConfirmation);
        Assert.Equal(old, world.Store.AssignmentList.Single(a => a.IsCurrent).LockerId);

        var done = await world.Change.HandleAsync(new ChangeStudentLockerRequest(student.Id, next, ConfirmWarnings: true), default);

        Assert.False(done.Value!.NeedsConfirmation);
        Assert.Equal(next, world.Store.AssignmentList.Single(a => a.IsCurrent).LockerId);
    }

    // --- Release ---

    [Fact]
    [Trait("spec", Spec + ": Liberación de una taquilla (Liberación manual)")]
    public async Task Releasing_frees_the_locker_and_keeps_the_closed_assignment_with_its_reason()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(1, zone);
        await world.AssignAsync(student.Id, locker);

        var result = await world.Release.HandleAsync(new ReleaseStudentLockerRequest(student.Id, new OperationContext("Canvi de centre")), default);

        Assert.False(result.Value!.IsCurrent);
        Assert.Equal(AssignmentCloseReason.Released, result.Value.CloseReason);
        Assert.Equal("Canvi de centre", result.Value.CloseNote);
        Assert.Equal(LockerStatus.Free, (await world.Inventory.RowAsync(locker)).Status);
        var again = await world.Release.HandleAsync(new ReleaseStudentLockerRequest(student.Id), default);
        Assert.Equal("Assignments.NoAssignment", again.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Liberación de una taquilla (Liberación de una taquilla averiada con alumno)")]
    public async Task Releasing_a_broken_locker_that_kept_its_student_leaves_it_broken_and_the_student_without_one()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(1, zone);
        await world.AssignAsync(student.Id, locker);
        await world.Inventory.MarkOutOfService.HandleAsync(
            new MarkLockerOutOfServiceRequest(locker, OutOfServiceKind.Broken, OutOfServiceDecision.Keep), default);

        await world.Release.HandleAsync(new ReleaseStudentLockerRequest(student.Id), default);

        var row = await world.Inventory.RowAsync(locker);
        Assert.Equal(LockerStatus.Broken, row.Status);
        Assert.False(row.HasAssignment);
        Assert.DoesNotContain(world.Store.AssignmentList, a => a.IsCurrent);
    }

    // --- Occupancy ---

    [Fact]
    [Trait("spec", Spec + ": Ocupación real de las taquillas (Estado tras liberar)")]
    public async Task The_real_occupancy_answers_for_a_whole_set_and_drives_the_visible_status()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var taken = await world.LockerAsync(1, zone);
        var free = await world.LockerAsync(2, zone);
        await world.AssignAsync(student.Id, taken);
        var occupancy = new AssignmentOccupancy(world.Store.Assignments, world.Store.Lockers);

        var occupied = await occupancy.OccupiedAmongAsync([taken, free], default);
        var held = await occupancy.CurrentAsync([student.Id], default);

        Assert.Equal([taken], occupied);
        Assert.Equal(1, held[student.Id].Number);
        await world.Release.HandleAsync(new ReleaseStudentLockerRequest(student.Id), default);
        Assert.Empty(await occupancy.OccupiedAmongAsync([taken, free], default));
        Assert.Equal(LockerStatus.Free, (await world.Inventory.RowAsync(taken)).Status);
    }

    // --- Retiring the student ---

    sealed class RecordingLifecycle : IStudentLifecycleHandler
    {
        public List<string> Calls { get; } = [];

        public Task OnRetiredAsync(Student student, OperationContext operation, CancellationToken ct)
        {
            Calls.Add("retired:" + operation.Reason);
            return Task.CompletedTask;
        }

        public Task OnReactivatedAsync(Student student, OperationContext operation, CancellationToken ct)
        {
            Calls.Add("reactivated");
            return Task.CompletedTask;
        }
    }

    [Fact]
    [Trait("spec", "alumnes-i-assignacions/alumnes: Baja de un alumno (Baja con taquilla)")]
    public async Task Retiring_a_student_frees_the_locker_records_both_histories_and_calls_the_lifecycle_hooks()
    {
        var (world, zone) = await WorldAsync();
        var lifecycle = new RecordingLifecycle();
        world.Students.LifecycleHooks.Add(lifecycle);
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(1, zone);
        await world.AssignAsync(student.Id, locker);

        var result = await world.Students.Retire.HandleAsync(new RetireStudentRequest(student.Id, "Trasllat"), default);

        Assert.True(result.Value!.IsRetired);
        Assert.Null(result.Value.LockerNumber);
        Assert.Equal(LockerStatus.Free, (await world.Inventory.RowAsync(locker)).Status);
        Assert.Equal(AssignmentCloseReason.StudentRetired, world.Store.AssignmentList.Single().CloseReason);
        Assert.Contains(world.Store.EventList, e => e.EntityId == locker && e.Type == LockerEventTypes.Released);
        Assert.Contains(world.Store.StudentEventList, e => e.EntityId == student.Id && e.Type == StudentEventTypes.AssignmentClosed);
        Assert.Equal(["retired:Trasllat"], lifecycle.Calls);
    }

    [Fact]
    [Trait("spec", "alumnes-i-assignacions/alumnes: Baja de un alumno (Baja sin taquilla)")]
    public async Task Retiring_a_student_without_a_locker_only_retires_and_reactivating_calls_the_hook()
    {
        var (world, _) = await WorldAsync();
        var lifecycle = new RecordingLifecycle();
        world.Students.LifecycleHooks.Add(lifecycle);
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");

        await world.Students.Retire.HandleAsync(new RetireStudentRequest(student.Id, "Trasllat"), default);
        await world.Students.Reactivate.HandleAsync(new Arca.Application.Students.ReactivateStudent.ReactivateStudentRequest(student.Id, "1r ESO", "A"), default);

        Assert.Empty(world.Store.AssignmentList);
        Assert.Equal(["retired:Trasllat", "reactivated"], lifecycle.Calls);
    }

    [Fact]
    [Trait("spec", Spec + ": Reserva para un alumno (Baja del alumno reservado)")]
    public async Task Retiring_a_student_removes_the_locker_reserved_in_their_name()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(1, zone);
        world.Store.LockerList.Single().ReserveForStudent(student.Id, "Per a la Núria", false, world.Clock.UtcNow);

        await world.Students.Retire.HandleAsync(new RetireStudentRequest(student.Id, "Trasllat"), default);

        var stored = world.Store.LockerList.Single();
        Assert.False(stored.IsReserved);
        Assert.Null(stored.ReservedForStudentId);
        Assert.Equal(LockerStatus.Free, (await world.Inventory.RowAsync(locker)).Status);
        Assert.Contains(world.Store.EventList, e => e.EntityId == locker && e.Type == LockerEventTypes.ReservationRemoved);
    }
}
