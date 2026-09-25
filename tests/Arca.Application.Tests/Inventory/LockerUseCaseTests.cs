// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Lockers;
using Arca.Application.Lockers.AddLocker;
using Arca.Application.Lockers.ChangeLockerNumber;
using Arca.Application.Lockers.ChangeLockerZone;
using Arca.Application.Lockers.MarkLockerOutOfService;
using Arca.Application.Lockers.RemoveLockerReservation;
using Arca.Application.Lockers.ReserveLocker;
using Arca.Application.Lockers.RestoreLockerService;
using Arca.Application.Lockers.RetireLocker;
using Arca.Domain.Lockers;
using Xunit;

namespace Arca.Application.Tests.Inventory;

public sealed class LockerUseCaseTests
{
    const string Spec = "taquilles-i-zones/taquilles";

    static List<string> EventTypes(InventoryWorld world, Guid lockerId) =>
        [.. world.Store.EventList.Where(e => e.EntityId == lockerId).OrderBy(e => e.OccurredAtUtc).Select(e => e.Type)];

    [Fact]
    [Trait("spec", Spec + ": Alta individual de una taquilla (Alta correcta)")]
    public async Task Adding_a_locker_creates_it_free_in_its_zone_and_records_the_event_in_the_same_operation()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");

        var result = await world.AddLocker.HandleAsync(new AddLockerRequest(101, zone), default);

        var row = result.Value!;
        Assert.Equal(101, row.Number);
        Assert.Equal("Planta 1", row.ZoneName);
        Assert.Equal(LockerStatus.Free, row.Status);
        Assert.Equal([LockerEventTypes.Created], EventTypes(world, row.Id));
    }

    [Fact]
    [Trait("spec", Spec + ": Alta individual de una taquilla (Zona desactivada)")]
    public async Task A_locker_cannot_be_added_to_a_deactivated_or_unknown_zone()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Antiga");
        await world.DeactivateZone.HandleAsync(new(zone), default);

        var inactive = await world.AddLocker.HandleAsync(new AddLockerRequest(1, zone), default);
        var unknown = await world.AddLocker.HandleAsync(new AddLockerRequest(1, Guid.NewGuid()), default);

        Assert.Equal("Lockers.ZoneUnavailable", inactive.Error!.Code);
        Assert.Equal("Zones.NotFound", unknown.Error!.Code);
        Assert.Empty(world.Store.LockerList);
        Assert.Empty(world.Store.EventList);
    }

    [Fact]
    [Trait("spec", Spec + ": Alta individual de una taquilla (Nota demasiado larga)")]
    public async Task A_note_that_is_too_long_saves_nothing()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");

        var result = await world.AddLocker.HandleAsync(new AddLockerRequest(1, zone, new string('n', 501)), default);

        Assert.Equal("Lockers.NoteTooLong", result.Error!.Code);
        Assert.Empty(world.Store.LockerList);
    }

    [Fact]
    [Trait("spec", Spec + ": Número único entre taquillas activas (Número duplicado entre activas)")]
    public async Task A_number_in_use_is_refused_and_a_retired_number_can_be_reused()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var first = await world.LockerAsync(15, zone);

        var duplicate = await world.AddLocker.HandleAsync(new AddLockerRequest(15, zone), default);
        await world.RetireLocker.HandleAsync(new RetireLockerRequest(first), default);
        var reused = await world.AddLocker.HandleAsync(new AddLockerRequest(15, zone), default);

        Assert.Equal("Lockers.NumberInUse", duplicate.Error!.Code);
        Assert.True(reused.IsSuccess);
        Assert.Equal(2, world.Store.LockerList.Count(l => l.Number == 15));
    }

    [Fact]
    [Trait("spec", Spec + ": Reserva con nota opcional (Reservar con nota)")]
    public async Task Reserving_and_removing_the_reservation_record_both_events()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);

        var reserved = await world.ReserveLocker.HandleAsync(new ReserveLockerRequest(locker, "Professorat"), default);
        var removed = await world.RemoveReservation.HandleAsync(new RemoveLockerReservationRequest(locker), default);

        Assert.Equal(LockerStatus.Reserved, reserved.Value!.Status);
        Assert.Equal("Professorat", reserved.Value.ReservationNote);
        Assert.Equal(LockerStatus.Free, removed.Value!.Status);
        Assert.Equal([LockerEventTypes.Created, LockerEventTypes.Reserved, LockerEventTypes.ReservationRemoved], EventTypes(world, locker));
    }

    [Fact]
    [Trait("spec", Spec + ": Reserva con nota opcional (Reservar una taquilla ocupada)")]
    public async Task An_occupied_locker_cannot_be_reserved_and_records_no_event()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        world.Store.Occupancy.Occupy(locker);

        var result = await world.ReserveLocker.HandleAsync(new ReserveLockerRequest(locker), default);

        Assert.Equal("Lockers.NotFree", result.Error!.Code);
        Assert.Equal([LockerEventTypes.Created], EventTypes(world, locker));
    }

    [Fact]
    [Trait("spec", Spec + ": Identidad de la taquilla y número visible (Número no válido)")]
    public async Task An_unknown_locker_is_reported_for_every_change()
    {
        var world = new InventoryWorld();
        var missing = Guid.NewGuid();

        Assert.Equal("Lockers.NotFound", (await world.ReserveLocker.HandleAsync(new ReserveLockerRequest(missing), default)).Error!.Code);
        Assert.Equal("Lockers.NotFound", (await world.RemoveReservation.HandleAsync(new RemoveLockerReservationRequest(missing), default)).Error!.Code);
        Assert.Equal("Lockers.NotFound", (await world.RestoreService.HandleAsync(new RestoreLockerServiceRequest(missing), default)).Error!.Code);
        Assert.Equal("Lockers.NotFound", (await world.RetireLocker.HandleAsync(new RetireLockerRequest(missing), default)).Error!.Code);
        Assert.Equal("Lockers.NotFound", (await world.ChangeNumber.HandleAsync(new ChangeLockerNumberRequest(missing, 5), default)).Error!.Code);
        Assert.Equal("Lockers.NotFound",
            (await world.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(missing, OutOfServiceKind.Broken), default)).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Marcar y resolver una avería (Avería en una taquilla libre)")]
    public async Task A_free_locker_is_marked_broken_and_the_breakdown_is_resolved()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);

        var marked = await world.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(locker, OutOfServiceKind.Broken), default);
        var restored = await world.RestoreService.HandleAsync(new RestoreLockerServiceRequest(locker), default);

        Assert.False(marked.Value!.DecisionRequired);
        Assert.Equal(LockerStatus.Broken, marked.Value.Locker.Status);
        Assert.Equal(LockerStatus.Free, restored.Value!.Status);
        Assert.Equal([LockerEventTypes.Created, LockerEventTypes.OutOfService, LockerEventTypes.ServiceRestored], EventTypes(world, locker));
    }

    [Fact]
    [Trait("spec", Spec + ": Decisión obligatoria al poner fuera de servicio una taquilla ocupada (Avería sin decisión)")]
    public async Task An_occupied_locker_without_a_decision_returns_the_options_and_changes_nothing()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        world.Store.Occupancy.Occupy(locker);

        var result = await world.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(locker, OutOfServiceKind.Maintenance), default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.DecisionRequired);
        Assert.Equal(3, result.Value.DecisionsOffered.Count);
        Assert.Equal(LockerStatus.Occupied, result.Value.Locker.Status);
        Assert.Equal([LockerEventTypes.Created], EventTypes(world, locker));
    }

    [Fact]
    [Trait("spec", Spec + ": Decisión obligatoria al poner fuera de servicio una taquilla ocupada (Decisión de mantener)")]
    public async Task Keeping_the_student_marks_it_broken_and_the_event_records_the_decision()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        world.Store.Occupancy.Occupy(locker);

        var result = await world.MarkOutOfService.HandleAsync(
            new MarkLockerOutOfServiceRequest(locker, OutOfServiceKind.Broken, OutOfServiceDecision.Keep), default);

        Assert.Equal(LockerStatus.Broken, result.Value!.Locker.Status);
        Assert.True(result.Value.Locker.HasAssignment);
        Assert.Contains("Keep", world.Store.EventList.Last().AfterJson, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(OutOfServiceDecision.Reassign)]
    [InlineData(OutOfServiceDecision.Release)]
    [Trait("spec", Spec + ": Decisión obligatoria al poner fuera de servicio una taquilla ocupada (Avería sin decisión)")]
    public async Task Reassigning_or_releasing_is_not_available_until_the_assignments_exist(OutOfServiceDecision decision)
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        world.Store.Occupancy.Occupy(locker);

        var result = await world.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(locker, OutOfServiceKind.Broken, decision), default);

        Assert.Equal("Lockers.DecisionNotAvailable", result.Error!.Code);
        Assert.Equal(LockerStatus.Occupied, (await world.RowAsync(locker)).Status);
    }

    [Fact]
    [Trait("spec", Spec + ": Estado visible derivado (Estado tras resolver la avería)")]
    public async Task Resolving_the_breakdown_of_an_occupied_locker_shows_it_occupied_again()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        world.Store.Occupancy.Occupy(locker);
        await world.MarkOutOfService.HandleAsync(
            new MarkLockerOutOfServiceRequest(locker, OutOfServiceKind.Broken, OutOfServiceDecision.Keep), default);

        var restored = await world.RestoreService.HandleAsync(new RestoreLockerServiceRequest(locker), default);

        Assert.Equal(LockerStatus.Occupied, restored.Value!.Status);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambio de número y de zona (Cambio de zona)")]
    public async Task Moving_a_locker_to_another_zone_records_the_previous_and_the_new_zone()
    {
        var world = new InventoryWorld();
        var first = await world.ZoneAsync("Planta 1");
        var second = await world.ZoneAsync("Gimnàs");
        var locker = await world.LockerAsync(1, first);

        var result = await world.ChangeZone.HandleAsync(new ChangeLockerZoneRequest(locker, second), default);

        Assert.Equal("Gimnàs", result.Value!.ZoneName);
        var change = world.Store.EventList.Last();
        Assert.Equal(LockerEventTypes.ZoneChanged, change.Type);
        Assert.Contains(first.ToString(), change.BeforeJson, StringComparison.Ordinal);
        Assert.Contains(second.ToString(), change.AfterJson, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambio de número y de zona (Cambio de zona)")]
    public async Task Moving_to_an_unknown_or_deactivated_zone_is_refused()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var old = await world.ZoneAsync("Antiga");
        await world.DeactivateZone.HandleAsync(new(old), default);
        var locker = await world.LockerAsync(1, zone);

        Assert.Equal("Zones.NotFound", (await world.ChangeZone.HandleAsync(new ChangeLockerZoneRequest(locker, Guid.NewGuid()), default)).Error!.Code);
        Assert.Equal("Lockers.ZoneUnavailable", (await world.ChangeZone.HandleAsync(new ChangeLockerZoneRequest(locker, old), default)).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambio de número y de zona (Cambio de número en conflicto)")]
    public async Task Changing_the_number_to_one_in_use_is_refused_and_to_a_free_one_is_recorded()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(15, zone);
        await world.LockerAsync(20, zone);

        var conflict = await world.ChangeNumber.HandleAsync(new ChangeLockerNumberRequest(locker, 20), default);
        var free = await world.ChangeNumber.HandleAsync(new ChangeLockerNumberRequest(locker, 21), default);

        Assert.Equal("Lockers.NumberInUse", conflict.Error!.Code);
        Assert.Equal(21, free.Value!.Number);
        Assert.Equal(locker, free.Value.Id);
        Assert.Equal(LockerEventTypes.NumberChanged, world.Store.EventList.Last().Type);
    }

    [Fact]
    [Trait("spec", Spec + ": Baja definitiva (Baja correcta)")]
    public async Task Retiring_a_free_locker_records_the_event_and_runs_the_hooks_in_the_same_operation()
    {
        var world = new InventoryWorld();
        var hook = new RecordingHook(world);
        world.Hooks.Add(hook);
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);

        var result = await world.RetireLocker.HandleAsync(new RetireLockerRequest(locker), default);

        Assert.Equal(LockerStatus.Retired, result.Value!.Status);
        Assert.Equal([locker], hook.Retired);
        Assert.True(hook.SawTheEventAlreadySaved);
        Assert.Equal(LockerEventTypes.Retired, world.Store.EventList.Last().Type);
    }

    sealed class RecordingHook(InventoryWorld world) : ILockerRetiredHandler
    {
        public List<Guid> Retired { get; } = [];

        public bool SawTheEventAlreadySaved { get; private set; }

        public Task HandleAsync(Guid lockerId, DateTimeOffset retiredAtUtc, CancellationToken ct)
        {
            Retired.Add(lockerId);
            SawTheEventAlreadySaved = world.Store.EventList.Any(e => e.EntityId == lockerId && e.Type == LockerEventTypes.Retired);
            return Task.CompletedTask;
        }
    }

    sealed class FailingHook : ILockerRetiredHandler
    {
        public Task HandleAsync(Guid lockerId, DateTimeOffset retiredAtUtc, CancellationToken ct) => throw new InvalidOperationException("hook failed");
    }

    [Fact]
    [Trait("spec", Spec + ": Baja definitiva (Baja correcta)")]
    public async Task If_a_hook_fails_the_retirement_and_its_event_are_undone()
    {
        var world = new InventoryWorld();
        world.Hooks.Add(new FailingHook());
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);

        await Assert.ThrowsAsync<InvalidOperationException>(() => world.RetireLocker.HandleAsync(new RetireLockerRequest(locker), default));

        Assert.False(world.Store.LockerList.Single().IsRetired);
        Assert.Equal([LockerEventTypes.Created], EventTypes(world, locker));
    }

    [Fact]
    [Trait("spec", Spec + ": Baja definitiva (Baja con asignación)")]
    public async Task Retiring_with_an_assignment_or_a_reservation_is_refused_and_no_hook_runs()
    {
        var world = new InventoryWorld();
        var hook = new RecordingHook(world);
        world.Hooks.Add(hook);
        var zone = await world.ZoneAsync("Planta 1");
        var occupied = await world.LockerAsync(1, zone);
        var reserved = await world.LockerAsync(2, zone);
        world.Store.Occupancy.Occupy(occupied);
        await world.ReserveLocker.HandleAsync(new ReserveLockerRequest(reserved), default);

        var withAssignment = await world.RetireLocker.HandleAsync(new RetireLockerRequest(occupied), default);
        var withReservation = await world.RetireLocker.HandleAsync(new RetireLockerRequest(reserved), default);

        Assert.Equal("Lockers.HasAssignment", withAssignment.Error!.Code);
        Assert.Equal("Lockers.HasReservation", withReservation.Error!.Code);
        Assert.Empty(hook.Retired);
    }

    [Fact]
    [Trait("spec", Spec + ": Baja definitiva (Reactivar una baja)")]
    public async Task A_retired_locker_accepts_no_further_change_through_the_use_cases()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        await world.RetireLocker.HandleAsync(new RetireLockerRequest(locker), default);

        Assert.Equal("Lockers.Retired", (await world.ReserveLocker.HandleAsync(new ReserveLockerRequest(locker), default)).Error!.Code);
        Assert.Equal("Lockers.Retired", (await world.ChangeNumber.HandleAsync(new ChangeLockerNumberRequest(locker, 9), default)).Error!.Code);
        Assert.Equal("Lockers.Retired", (await world.RetireLocker.HandleAsync(new RetireLockerRequest(locker), default)).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Alta por rangos (Fallo durante la creación)")]
    public async Task If_saving_the_event_fails_the_locker_is_not_created_either()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        world.Store.FailOnNextEvent = true;

        await Assert.ThrowsAsync<IOException>(() => world.AddLocker.HandleAsync(new AddLockerRequest(1, zone), default));

        Assert.Empty(world.Store.LockerList);
        Assert.Empty(world.Store.EventList);
    }

    [Fact]
    [Trait("spec", Spec + ": Historial de eventos de la taquilla (Consulta del historial)")]
    public async Task Without_the_assignments_no_locker_is_occupied()
    {
        var occupancy = new NoOccupancy();
        var id = Guid.NewGuid();

        Assert.Empty(await occupancy.OccupiedAmongAsync([id], default));
    }
}
