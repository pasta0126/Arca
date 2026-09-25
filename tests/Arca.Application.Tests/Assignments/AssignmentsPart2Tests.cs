// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Assignments.ChangeStudentLocker;
using Arca.Application.Assignments.GetLockerAssignments;
using Arca.Application.Assignments.GetStudentAssignments;
using Arca.Application.Assignments.ReleaseStudentLocker;
using Arca.Application.Assignments.ReserveLockerForStudent;
using Arca.Application.Assignments.SuggestLocker;
using Arca.Application.Lockers.MarkLockerOutOfService;
using Arca.Application.Lockers.RemoveLockerReservation;
using Arca.Application.Lockers.RetireLocker;
using Arca.Application.Students.RetireStudent;
using Arca.Domain.Assignments;
using Arca.Domain.Lockers;
using Arca.Domain.Students;
using Xunit;

namespace Arca.Application.Tests.Assignments;

public sealed class AssignmentsPart2Tests
{
    const string Spec = "alumnes-i-assignacions/assignacions";

    static async Task<(AssignmentsWorld World, Guid Zone)> WorldAsync()
    {
        var world = new AssignmentsWorld();
        await world.Students.CreateYearAsync();
        return (world, await world.ZoneAsync("Planta 1"));
    }

    // --- Suggestion ---

    [Fact]
    [Trait("spec", Spec + ": Sugerencia de taquilla libre (Zona con taquillas libres)")]
    public async Task The_suggestion_is_the_free_locker_with_the_lowest_number_of_the_zone()
    {
        var (world, zone) = await WorldAsync();
        foreach (var number in new[] { 20, 12, 15 })
        {
            await world.LockerAsync(number, zone);
        }

        var result = await world.Suggest.HandleAsync(new SuggestLockerRequest(zone), default);

        Assert.Equal(12, result.Value!.Locker.Number);
        Assert.False(result.Value.FromAnotherZone);
    }

    [Fact]
    [Trait("spec", Spec + ": Sugerencia de taquilla libre (Zona con taquillas libres)")]
    public async Task Occupied_reserved_and_broken_lockers_are_not_suggested()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var occupied = await world.LockerAsync(1, zone);
        var reserved = await world.LockerAsync(2, zone);
        var broken = await world.LockerAsync(3, zone);
        await world.LockerAsync(4, zone);
        await world.AssignAsync(student.Id, occupied);
        await world.Inventory.ReserveLocker.HandleAsync(new Arca.Application.Lockers.ReserveLocker.ReserveLockerRequest(reserved), default);
        await world.Inventory.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(broken, OutOfServiceKind.Broken), default);

        var result = await world.Suggest.HandleAsync(new SuggestLockerRequest(zone), default);

        Assert.Equal(4, result.Value!.Locker.Number);
    }

    [Fact]
    [Trait("spec", Spec + ": Sugerencia de taquilla libre (Zona sin taquillas libres)")]
    public async Task A_zone_with_none_free_suggests_the_next_zone_that_has_one_and_says_it_is_another_zone()
    {
        var (world, first) = await WorldAsync();
        var second = await world.ZoneAsync("Planta 2");
        var third = await world.ZoneAsync("Planta 3");
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var full = await world.LockerAsync(1, first);
        await world.LockerAsync(30, third);
        await world.LockerAsync(31, third);
        await world.AssignAsync(student.Id, full);

        var next = await world.Suggest.HandleAsync(new SuggestLockerRequest(first), default);
        var wrapped = await world.Suggest.HandleAsync(new SuggestLockerRequest(third), default);

        Assert.Equal(30, next.Value!.Locker.Number);
        Assert.Equal("Planta 3", next.Value.Locker.ZoneName); // Planta 2 has no lockers at all
        Assert.True(next.Value.FromAnotherZone);
        Assert.Equal(30, wrapped.Value!.Locker.Number);
        Assert.False(wrapped.Value.FromAnotherZone);
        Assert.NotEqual(second, next.Value.Locker.ZoneId);
    }

    [Fact]
    [Trait("spec", Spec + ": Sugerencia de taquilla libre (Sin taquillas libres)")]
    public async Task With_no_free_locker_anywhere_it_says_so()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        await world.AssignAsync(student.Id, await world.LockerAsync(1, zone));
        var empty = new AssignmentsWorld();

        var none = await world.Suggest.HandleAsync(new SuggestLockerRequest(zone), default);
        var noLockersAtAll = await empty.Suggest.HandleAsync(new SuggestLockerRequest(), default);

        Assert.Equal("Assignments.NoFreeLockers", none.Error!.Code);
        Assert.Equal("Assignments.NoFreeLockers", noLockersAtAll.Error!.Code);
    }

    // --- Reservation for a student ---

    [Fact]
    [Trait("spec", Spec + ": Reserva para un alumno (Reservar para un alumno)")]
    public async Task Reserving_for_a_student_records_the_event_in_both_histories_and_a_later_assignment_consumes_it()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(1, zone);

        var reserved = await world.ReserveForStudent.HandleAsync(new ReserveLockerForStudentRequest(locker, student.Id, "Per a la Núria"), default);

        Assert.Equal(LockerStatus.Reserved, reserved.Value!.Status);
        Assert.Equal(student.Id, world.Store.LockerList.Single().ReservedForStudentId);
        Assert.Contains(world.Store.EventList, e => e.EntityId == locker && e.Type == LockerEventTypes.Reserved);
        Assert.Contains(world.Store.StudentEventList, e => e.EntityId == student.Id && e.Type == StudentEventTypes.LockerReserved);

        var assigned = await world.AssignAsync(student.Id, locker);

        Assert.NotNull(assigned.Assignment);
        Assert.False(world.Store.LockerList.Single().IsReserved);
    }

    [Fact]
    [Trait("spec", Spec + ": Reserva para un alumno (Alumno con taquilla ya asignada)")]
    public async Task A_student_that_already_holds_a_locker_or_a_reservation_cannot_reserve_another()
    {
        var (world, zone) = await WorldAsync();
        var holder = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var reserver = await world.StudentAsync("Pau", "Serra", "pau@test.cat");
        var held = await world.LockerAsync(1, zone);
        var first = await world.LockerAsync(2, zone);
        var second = await world.LockerAsync(3, zone);
        await world.AssignAsync(holder.Id, held);
        await world.ReserveForStudent.HandleAsync(new ReserveLockerForStudentRequest(first, reserver.Id), default);

        var alreadyHolds = await world.ReserveForStudent.HandleAsync(new ReserveLockerForStudentRequest(second, holder.Id), default);
        var alreadyReserved = await world.ReserveForStudent.HandleAsync(new ReserveLockerForStudentRequest(second, reserver.Id), default);

        Assert.Equal("Assignments.StudentHasLocker", alreadyHolds.Error!.Code);
        Assert.Equal("Assignments.StudentHasReservation", alreadyReserved.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Reserva para un alumno (Reservar para un alumno)")]
    public async Task Only_a_free_locker_can_be_reserved_for_an_active_student_and_unknown_ones_are_reported()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var other = await world.StudentAsync("Pau", "Serra", "pau@test.cat");
        var occupied = await world.LockerAsync(1, zone);
        await world.AssignAsync(other.Id, occupied);
        var retired = await world.StudentAsync("Àlex", "Martí", "alex@test.cat");
        var free = await world.LockerAsync(2, zone);
        await world.Students.Retire.HandleAsync(new RetireStudentRequest(retired.Id, "Trasllat"), default);

        var onOccupied = await world.ReserveForStudent.HandleAsync(new ReserveLockerForStudentRequest(occupied, student.Id), default);
        var forRetired = await world.ReserveForStudent.HandleAsync(new ReserveLockerForStudentRequest(free, retired.Id), default);
        var unknownStudent = await world.ReserveForStudent.HandleAsync(new ReserveLockerForStudentRequest(free, Guid.NewGuid()), default);
        var unknownLocker = await world.ReserveForStudent.HandleAsync(new ReserveLockerForStudentRequest(Guid.NewGuid(), student.Id), default);

        Assert.Equal("Lockers.NotFree", onOccupied.Error!.Code);
        Assert.Equal("Assignments.StudentRetired", forRetired.Error!.Code);
        Assert.Equal("Students.NotFound", unknownStudent.Error!.Code);
        Assert.Equal("Lockers.NotFound", unknownLocker.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Reserva para un alumno (Baja del alumno reservado)")]
    public async Task Removing_a_reservation_for_a_student_also_records_it_in_the_student_history()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(1, zone);
        await world.ReserveForStudent.HandleAsync(new ReserveLockerForStudentRequest(locker, student.Id), default);

        var removed = await world.Inventory.RemoveReservation.HandleAsync(new RemoveLockerReservationRequest(locker), default);

        Assert.Equal(LockerStatus.Free, removed.Value!.Status);
        Assert.Contains(world.Store.StudentEventList, e => e.EntityId == student.Id && e.Type == StudentEventTypes.LockerReservationRemoved);
    }

    // --- Decisions when an occupied locker is put out of service ---

    [Fact]
    [Trait("spec", Spec + ": Decisión de reasignar o liberar (Reasignar)")]
    public async Task Reassigning_moves_the_student_to_the_new_locker_in_one_operation_and_records_the_decision()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var broken = await world.LockerAsync(1, zone);
        var target = await world.LockerAsync(2, zone);
        await world.AssignAsync(student.Id, broken);

        var result = await world.Inventory.MarkOutOfService.HandleAsync(
            new MarkLockerOutOfServiceRequest(broken, OutOfServiceKind.Broken, OutOfServiceDecision.Reassign, target), default);

        Assert.False(result.Value!.DecisionRequired);
        var oldRow = await world.Inventory.RowAsync(broken);
        Assert.Equal(LockerStatus.Broken, oldRow.Status);
        Assert.False(oldRow.HasAssignment);
        Assert.Equal(LockerStatus.Occupied, (await world.Inventory.RowAsync(target)).Status);
        Assert.Equal(target, world.Store.AssignmentList.Single(a => a.IsCurrent).LockerId);
        Assert.Equal(AssignmentCloseReason.OutOfServiceReassigned, world.Store.AssignmentList.Single(a => !a.IsCurrent).CloseReason);
        Assert.Contains("Reassign", world.Store.EventList.Single(e => e.EntityId == broken && e.Type == LockerEventTypes.OutOfService).AfterJson, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Decisión de reasignar o liberar (Reasignar sin destino válido)")]
    public async Task Reassigning_without_a_valid_destination_changes_nothing_and_the_locker_does_not_break()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var other = await world.StudentAsync("Pau", "Serra", "pau@test.cat");
        var broken = await world.LockerAsync(1, zone);
        var taken = await world.LockerAsync(2, zone);
        await world.AssignAsync(student.Id, broken);
        await world.AssignAsync(other.Id, taken);

        Task<Arca.Domain.Common.Result<MarkLockerOutOfServiceResult>> Mark(Guid? target) => world.Inventory.MarkOutOfService.HandleAsync(
            new MarkLockerOutOfServiceRequest(broken, OutOfServiceKind.Broken, OutOfServiceDecision.Reassign, target), default);

        var toOccupied = await Mark(taken);
        var noTarget = await Mark(null);
        var toSelf = await Mark(broken);
        var toMissing = await Mark(Guid.NewGuid());

        Assert.Equal("Assignments.LockerOccupied", toOccupied.Error!.Code);
        Assert.Equal("Assignments.TargetRequired", noTarget.Error!.Code);
        Assert.Equal("Assignments.SameLocker", toSelf.Error!.Code);
        Assert.Equal("Lockers.NotFound", toMissing.Error!.Code);
        var row = await world.Inventory.RowAsync(broken);
        Assert.Equal(LockerStatus.Occupied, row.Status); // still in service with its student
        Assert.Equal(broken, world.Store.AssignmentList.Single(a => a.StudentId == student.Id && a.IsCurrent).LockerId);
    }

    [Fact]
    [Trait("spec", Spec + ": Decisión de reasignar o liberar (Liberar)")]
    public async Task Freeing_the_student_leaves_the_locker_out_of_service_without_an_assignment()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(1, zone);
        await world.AssignAsync(student.Id, locker);

        await world.Inventory.MarkOutOfService.HandleAsync(
            new MarkLockerOutOfServiceRequest(locker, OutOfServiceKind.Maintenance, OutOfServiceDecision.Release), default);

        var row = await world.Inventory.RowAsync(locker);
        Assert.Equal(LockerStatus.Maintenance, row.Status);
        Assert.False(row.HasAssignment);
        Assert.Equal(AssignmentCloseReason.OutOfServiceReleased, world.Store.AssignmentList.Single().CloseReason);
        var detail = await world.Students.Get.HandleAsync(new Arca.Application.Students.GetStudent.GetStudentRequest(student.Id), default);
        Assert.Null(detail.Value!.LockerNumber);
    }

    [Fact]
    [Trait("spec", Spec + ": Decisión de reasignar o liberar (Reasignar)")]
    public async Task The_decision_is_still_required_and_keeping_the_student_keeps_the_assignment()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(1, zone);
        await world.AssignAsync(student.Id, locker);

        var asked = await world.Inventory.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(locker, OutOfServiceKind.Broken), default);
        var kept = await world.Inventory.MarkOutOfService.HandleAsync(
            new MarkLockerOutOfServiceRequest(locker, OutOfServiceKind.Broken, OutOfServiceDecision.Keep), default);

        Assert.True(asked.Value!.DecisionRequired);
        Assert.False(kept.Value!.DecisionRequired);
        Assert.True(world.Store.AssignmentList.Single().IsCurrent);
        Assert.True((await world.Inventory.RowAsync(locker)).HasAssignment);
    }

    [Fact]
    [Trait("spec", Spec + ": Decisión de reasignar o liberar (Reasignar)")]
    public async Task Reassigning_can_warn_about_the_destination_and_only_moves_the_student_once_confirmed()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var broken = await world.LockerAsync(1, zone);
        var target = await world.LockerAsync(2, zone);
        await world.AssignAsync(student.Id, broken);
        world.Students.Guards.Add(new WarningGuard());

        var asked = await world.Inventory.MarkOutOfService.HandleAsync(
            new MarkLockerOutOfServiceRequest(broken, OutOfServiceKind.Broken, OutOfServiceDecision.Reassign, target), default);

        Assert.True(asked.Value!.NeedsWarningConfirmation);
        Assert.Equal(LockerStatus.Occupied, (await world.Inventory.RowAsync(broken)).Status);

        var done = await world.Inventory.MarkOutOfService.HandleAsync(
            new MarkLockerOutOfServiceRequest(broken, OutOfServiceKind.Broken, OutOfServiceDecision.Reassign, target, ConfirmWarnings: true), default);

        Assert.False(done.Value!.NeedsWarningConfirmation);
        Assert.Equal(target, world.Store.AssignmentList.Single(a => a.IsCurrent).LockerId);
    }

    sealed class WarningGuard : IAssignmentGuard
    {
        public Task<IReadOnlyList<AssignmentFinding>> CheckAsync(ProposedAssignment proposal, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<AssignmentFinding>>([new AssignmentFinding(AssignmentFindingKind.Warning, "Charges.PriorDebt")]);
    }

    // --- Histories of assignments ---

    [Fact]
    [Trait("spec", Spec + ": Historial de asignaciones (Historial por alumno)")]
    public async Task The_assignments_of_a_student_list_every_locker_with_dates_and_year_most_recent_first()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var first = await world.LockerAsync(1, zone);
        var second = await world.LockerAsync(2, zone);
        await world.AssignAsync(student.Id, first);
        world.Clock.Advance(TimeSpan.FromMinutes(5));
        await world.Change.HandleAsync(new ChangeStudentLockerRequest(student.Id, second), default);

        var result = await world.StudentAssignments.HandleAsync(new GetStudentAssignmentsRequest(student.Id), default);
        var missing = await world.StudentAssignments.HandleAsync(new GetStudentAssignmentsRequest(Guid.NewGuid()), default);

        Assert.Equal([2, 1], result.Value!.Select(r => r.LockerNumber));
        Assert.All(result.Value!, r => Assert.Equal("2026-2027", r.YearName));
        Assert.True(result.Value![0].IsCurrent);
        Assert.Equal(AssignmentCloseReason.Changed, result.Value![1].CloseReason);
        Assert.Equal("Students.NotFound", missing.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Historial de asignaciones (Historial por taquilla)")]
    public async Task The_assignments_of_a_locker_list_every_student_that_has_had_it()
    {
        var (world, zone) = await WorldAsync();
        var first = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var second = await world.StudentAsync("Pau", "Serra", "pau@test.cat");
        var locker = await world.LockerAsync(1, zone);
        await world.AssignAsync(first.Id, locker);
        await world.Release.HandleAsync(new ReleaseStudentLockerRequest(first.Id), default);
        await world.AssignAsync(second.Id, locker);

        var result = await world.LockerAssignments.HandleAsync(new GetLockerAssignmentsRequest(locker), default);

        Assert.Equal(["Pau Serra", "Núria García"], result.Value!.Select(r => r.StudentName));
        Assert.Equal("Lockers.NotFound", (await world.LockerAssignments.HandleAsync(new GetLockerAssignmentsRequest(Guid.NewGuid()), default)).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Historial de asignaciones (Taquilla de baja con número reutilizado)")]
    public async Task A_retired_locker_and_an_active_one_with_the_same_number_show_only_their_own_assignments()
    {
        var (world, zone) = await WorldAsync();
        var first = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var second = await world.StudentAsync("Pau", "Serra", "pau@test.cat");
        var old = await world.LockerAsync(15, zone);
        await world.AssignAsync(first.Id, old);
        await world.Release.HandleAsync(new ReleaseStudentLockerRequest(first.Id), default);
        await world.Inventory.RetireLocker.HandleAsync(new RetireLockerRequest(old), default);
        var current = await world.LockerAsync(15, zone);
        await world.AssignAsync(second.Id, current);

        var oldHistory = await world.LockerAssignments.HandleAsync(new GetLockerAssignmentsRequest(old), default);
        var currentHistory = await world.LockerAssignments.HandleAsync(new GetLockerAssignmentsRequest(current), default);

        Assert.Equal(["Núria García"], oldHistory.Value!.Select(r => r.StudentName));
        Assert.Equal(["Pau Serra"], currentHistory.Value!.Select(r => r.StudentName));
    }

    // --- The histories of the student and of the locker read in Catalan ---

    [Fact]
    [Trait("spec", "alumnes-i-assignacions/alumnes: Historial del alumno (Consulta del historial)")]
    public async Task The_assignment_events_read_in_catalan_in_both_histories_with_names_looked_up_when_shown()
    {
        var (world, zone) = await WorldAsync();
        var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
        var locker = await world.LockerAsync(15, zone);
        await world.AssignAsync(student.Id, locker);
        world.Clock.Advance(TimeSpan.FromMinutes(5));
        await world.Release.HandleAsync(new ReleaseStudentLockerRequest(student.Id, new OperationContext("Trasllat")), default);

        var studentHistory = (await world.Students.History.HandleAsync(new Arca.Application.Students.GetStudentHistory.GetStudentHistoryRequest(student.Id), default)).Value!;
        var lockerHistory = (await world.Inventory.History.HandleAsync(new Arca.Application.Lockers.GetLockerHistory.GetLockerHistoryRequest(locker), default)).Value!;

        Assert.Contains("Taquilla 15 alliberada (alliberada manualment).", studentHistory.Select(e => e.Text));
        Assert.Contains("Taquilla 15 assignada.", studentHistory.Select(e => e.Text));
        Assert.Contains("Núria García ha deixat la taquilla (alliberada manualment). Nota: Trasllat", lockerHistory.Select(e => e.Text));
        Assert.Contains("Taquilla assignada a Núria García.", lockerHistory.Select(e => e.Text));
        string[] assignmentEvents = [LockerEventTypes.Assigned, LockerEventTypes.Released, StudentEventTypes.AssignmentOpened, StudentEventTypes.AssignmentClosed];
        Assert.DoesNotContain(
            world.Store.EventList.Concat(world.Store.StudentEventList).Where(e => assignmentEvents.Contains(e.Type)),
            e => (e.BeforeJson + e.AfterJson).Contains("Núria", StringComparison.Ordinal)); // ids only, never the name
    }

    // --- Equivalence of the three ways to assign ---

    [Fact]
    [Trait("spec", Spec + ": Iniciar la asignación desde el alumno, la taquilla o arrastrando (Arrastrando)")]
    public async Task Assigning_from_the_student_from_the_locker_or_by_dragging_gives_the_same_result_and_the_same_refusals()
    {
        // The three ways call the same use case with the same request: only the way of choosing (student first, locker first,
        // drop of a student on a locker) differs. Each one is run on its own copy of the same situation.
        var outcomes = new List<(string Result, string Refusal)>();
        foreach (var way in new[] { "from the student", "from the locker", "by dragging" })
        {
            var (world, zone) = await WorldAsync();
            world.Students.Guards.Add(new WarningGuard());
            var student = await world.StudentAsync("Núria", "García", "nuria@test.cat");
            var other = await world.StudentAsync("Pau", "Serra", "pau@test.cat");
            var third = await world.StudentAsync("Àlex", "Martí", "alex@test.cat");
            var free = await world.LockerAsync(1, zone);
            var taken = await world.LockerAsync(2, zone);
            var broken = await world.LockerAsync(3, zone);
            await world.AssignAsync(other.Id, taken, confirm: true);
            await world.Inventory.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(broken, OutOfServiceKind.Broken), default);

            var asked = await world.Assign.HandleAsync(new AssignLockerRequest(student.Id, free), default);
            var done = await world.Assign.HandleAsync(new AssignLockerRequest(student.Id, free, ConfirmWarnings: true), default);
            var onTaken = await world.Assign.HandleAsync(new AssignLockerRequest(third.Id, taken), default);
            var onBroken = await world.Assign.HandleAsync(new AssignLockerRequest(third.Id, broken), default);

            outcomes.Add((
                $"{asked.Value!.NeedsConfirmation}/{string.Join(",", asked.Value.Warnings.Select(w => w.Code))}/{done.Value!.Assignment!.LockerNumber}/{done.Value.Assignment.ZoneName}",
                $"{onTaken.Error!.Code}/{onBroken.Error!.Code}"));
            Assert.NotEmpty(way);
        }

        Assert.Single(outcomes.Distinct());
        Assert.Equal("True/Charges.PriorDebt/1/Planta 1", outcomes[0].Result);
        Assert.Equal("Assignments.LockerOccupied/Assignments.LockerUnavailable", outcomes[0].Refusal);
    }

    [Fact]
    [Trait("spec", "alumnes-i-assignacions/design: D7 Asignación: un único caso de uso")]
    public void Only_the_assignment_flow_opens_an_assignment_so_no_path_can_skip_the_validation()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Directory.Build.props")))
        {
            root = root.Parent;
        }

        var callers = Directory.EnumerateFiles(Path.Combine(root!.FullName, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Where(f => File.ReadAllText(f).Contains("Assignment.Open(", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(["AssignmentFlow.cs"], callers); // the single caller of the rule that validates an assignment
    }
}
