// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;
using Arca.Domain.Lockers;
using Arca.Domain.Zones;
using Xunit;

namespace Arca.Domain.Tests.Lockers;

public sealed class LockerRulesTests
{
    const string Spec = "taquilles-i-zones/taquilles";

    static readonly DateTimeOffset _now = new(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);
    static readonly Zone _zone = Zone.Create(Guid.NewGuid(), "Planta 1", []).Value!;

    static Locker Make(int number = 15, Zone? zone = null) =>
        Locker.Create(Guid.NewGuid(), number, zone ?? _zone, null, [], _now).Value!.Locker;

    static Zone Inactive(string name)
    {
        var zone = Zone.Create(Guid.NewGuid(), name, []).Value!;
        zone.Deactivate(0);
        return zone;
    }

    // --- Identity and number ---

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(100000)]
    [Trait("spec", Spec + ": Identidad de la taquilla y número visible (Número no válido)")]
    public void A_number_outside_1_to_99999_is_refused(int number)
    {
        var result = Locker.Create(Guid.NewGuid(), number, _zone, null, [], _now);

        Assert.Equal("Lockers.NumberInvalid", result.Error!.Code);
        Assert.Equal([1, 99999], result.Error.Args);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("99999", 99999)]
    [InlineData(" 42 ", 42)]
    [Trait("spec", Spec + ": Identidad de la taquilla y número visible (Número no válido)")]
    public void A_valid_typed_number_is_read(string text, int expected)
    {
        Assert.Equal(expected, LockerNumber.Parse(text).Value);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("12.5")]
    [InlineData("12,5")]
    [InlineData("100000")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("99999999999999")]
    [Trait("spec", Spec + ": Identidad de la taquilla y número visible (Número no válido)")]
    public void A_typed_number_that_is_zero_negative_decimal_or_too_big_is_refused(string? text)
    {
        Assert.Equal("Lockers.NumberInvalid", LockerNumber.Parse(text).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Identidad de la taquilla y número visible (Historial ligado a la identidad)")]
    public void Changing_the_number_keeps_the_identity_and_the_event_is_tied_to_it()
    {
        var locker = Make(15);
        var id = locker.Id;

        var result = locker.ChangeNumber(16, [locker], _now);

        Assert.Equal(id, locker.Id);
        Assert.Equal(id, result.Value!.EntityId);
        Assert.Equal(LockerEventTypes.NumberChanged, result.Value.Type);
    }

    // --- Unique number among active lockers ---

    [Fact]
    [Trait("spec", Spec + ": Número único entre taquillas activas (Número duplicado entre activas)")]
    public void A_number_used_by_an_active_locker_is_refused()
    {
        var existing = Make(15);

        var result = Locker.Create(Guid.NewGuid(), 15, _zone, null, [existing], _now);

        Assert.Equal("Lockers.NumberInUse", result.Error!.Code);
        Assert.Equal(15, Assert.Single(result.Error.Args));
    }

    [Fact]
    [Trait("spec", Spec + ": Número único entre taquillas activas (Reutilizar el número de una baja)")]
    public void The_number_of_a_retired_locker_can_be_used_again_and_both_coexist()
    {
        var retired = Make(15);
        retired.Retire(hasAssignment: false, _now);

        var created = Locker.Create(Guid.NewGuid(), 15, _zone, null, [retired], _now);

        Assert.True(created.IsSuccess);
        Assert.NotEqual(retired.Id, created.Value!.Locker.Id);
    }

    [Fact]
    [Trait("spec", Spec + ": Número único entre taquillas activas (Varias bajas con el mismo número)")]
    public void Several_retired_lockers_with_the_same_number_do_not_block_a_new_one()
    {
        var first = Make(15);
        first.Retire(false, _now);
        var second = Make(15);
        second.Retire(false, _now);

        Assert.True(Locker.Create(Guid.NewGuid(), 15, _zone, null, [first, second], _now).IsSuccess);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambio de número y de zona (Cambio de número en conflicto)")]
    public void Changing_to_a_number_of_another_active_locker_is_refused_and_changes_nothing()
    {
        var locker = Make(15);
        var other = Make(20);

        var result = locker.ChangeNumber(20, [locker, other], _now);

        Assert.Equal("Lockers.NumberInUse", result.Error!.Code);
        Assert.Equal(15, locker.Number);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambio de número y de zona (Cambio de número en conflicto)")]
    public void Changing_to_a_number_of_a_retired_locker_is_accepted()
    {
        var locker = Make(15);
        var retired = Make(20);
        retired.Retire(false, _now);

        Assert.True(locker.ChangeNumber(20, [locker, retired], _now).IsSuccess);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambio de número y de zona (Cambio de número en conflicto)")]
    public void Changing_to_the_same_number_or_an_invalid_one_is_refused()
    {
        var locker = Make(15);

        Assert.Equal("Lockers.Unchanged", locker.ChangeNumber(15, [locker], _now).Error!.Code);
        Assert.Equal("Lockers.NumberInvalid", locker.ChangeNumber(0, [locker], _now).Error!.Code);
    }

    // --- Individual creation ---

    [Fact]
    [Trait("spec", Spec + ": Alta individual de una taquilla (Alta correcta)")]
    public void A_new_locker_is_free_in_its_zone_and_the_creation_event_is_returned()
    {
        var result = Locker.Create(Guid.NewGuid(), 101, _zone, "  prop de l'entrada  ", [], _now);

        var locker = result.Value!.Locker;
        Assert.Equal(LockerStatus.Free, locker.StateWith(hasAssignment: false).Status);
        Assert.Equal(_zone.Id, locker.ZoneId);
        Assert.Equal("prop de l'entrada", locker.Note);
        Assert.Equal(LockerEventTypes.Created, result.Value.Event.Type);
        Assert.Equal(locker.Id, result.Value.Event.EntityId);
        Assert.Equal(_now, result.Value.Event.OccurredAtUtc);
        Assert.Null(result.Value.Event.BeforeJson);
        Assert.Contains("\"number\":101", result.Value.Event.AfterJson, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Alta individual de una taquilla (Zona desactivada)")]
    public void A_locker_cannot_be_created_in_a_deactivated_zone()
    {
        var result = Locker.Create(Guid.NewGuid(), 101, Inactive("Antiga"), null, [], _now);

        Assert.Equal("Lockers.ZoneUnavailable", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Alta individual de una taquilla (Nota demasiado larga)")]
    public void A_note_of_more_than_500_characters_is_refused_and_500_is_accepted()
    {
        Assert.Equal("Lockers.NoteTooLong", Locker.Create(Guid.NewGuid(), 1, _zone, new string('n', 501), [], _now).Error!.Code);
        Assert.True(Locker.Create(Guid.NewGuid(), 1, _zone, new string('n', 500), [], _now).IsSuccess);
        Assert.Null(Locker.Create(Guid.NewGuid(), 1, _zone, "   ", [], _now).Value!.Locker.Note);
    }

    // --- Reservation ---

    [Fact]
    [Trait("spec", Spec + ": Reserva con nota opcional (Reservar sin nota)")]
    public void A_free_locker_can_be_reserved_without_a_note()
    {
        var locker = Make();

        var result = locker.Reserve(null, hasAssignment: false, _now);

        Assert.True(result.IsSuccess);
        Assert.Equal(LockerStatus.Reserved, locker.StateWith(false).Status);
        Assert.Null(locker.ReservationNote);
        Assert.Equal(LockerEventTypes.Reserved, result.Value!.Type);
    }

    [Fact]
    [Trait("spec", Spec + ": Reserva con nota opcional (Reservar con nota)")]
    public void A_reservation_keeps_its_note_and_makes_the_locker_not_free()
    {
        var locker = Make();

        var result = locker.Reserve("Professorat d'educació física", false, _now);

        Assert.Equal("Professorat d'educació física", locker.ReservationNote);
        Assert.Contains("Professorat", result.Value!.AfterJson, StringComparison.Ordinal);
        Assert.Equal("Lockers.NotFree", locker.Reserve(null, false, _now).Error!.Code); // not assignable, not reservable twice
    }

    [Fact]
    [Trait("spec", Spec + ": Reserva con nota opcional (Reservar una taquilla ocupada)")]
    public void An_occupied_locker_cannot_be_reserved()
    {
        var locker = Make();

        var result = locker.Reserve(null, hasAssignment: true, _now);

        Assert.Equal("Lockers.NotFree", result.Error!.Code);
        Assert.False(locker.IsReserved);
    }

    [Fact]
    [Trait("spec", Spec + ": Reserva con nota opcional (Reservar una taquilla ocupada)")]
    public void An_out_of_service_locker_cannot_be_reserved_and_a_long_note_is_refused()
    {
        var broken = Make();
        broken.MarkOutOfService(OutOfServiceKind.Broken, null, false, _now);

        Assert.Equal("Lockers.NotFree", broken.Reserve(null, false, _now).Error!.Code);
        Assert.Equal("Lockers.NoteTooLong", Make().Reserve(new string('x', 501), false, _now).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Reserva con nota opcional (Quitar la reserva)")]
    public void Removing_the_reservation_frees_the_locker_and_records_the_event()
    {
        var locker = Make();
        locker.Reserve("nota", false, _now);

        var result = locker.RemoveReservation(_now);

        Assert.Equal(LockerStatus.Free, locker.StateWith(false).Status);
        Assert.Null(locker.ReservationNote);
        Assert.Equal(LockerEventTypes.ReservationRemoved, result.Value!.Type);
        Assert.Contains("nota", result.Value.BeforeJson, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Reserva con nota opcional (Quitar la reserva)")]
    public void Removing_a_reservation_that_does_not_exist_is_refused()
    {
        Assert.Equal("Lockers.NotReserved", Make().RemoveReservation(_now).Error!.Code);
    }

    // --- Breakdown and maintenance ---

    [Fact]
    [Trait("spec", Spec + ": Marcar y resolver una avería (Avería en una taquilla libre)")]
    public void A_free_locker_can_be_marked_broken()
    {
        var locker = Make();

        var result = locker.MarkOutOfService(OutOfServiceKind.Broken, null, hasAssignment: false, _now);

        Assert.False(result.Value!.NeedsDecision);
        Assert.Equal(LockerStatus.Broken, locker.StateWith(false).Status);
        Assert.Equal(LockerEventTypes.OutOfService, result.Value.Event!.Type);
    }

    [Fact]
    [Trait("spec", Spec + ": Marcar y resolver una avería (Avería en una taquilla reservada)")]
    public void A_reserved_locker_that_breaks_keeps_its_reservation_and_gets_it_back_when_resolved()
    {
        var locker = Make();
        locker.Reserve("nota", false, _now);

        locker.MarkOutOfService(OutOfServiceKind.Broken, null, false, _now);
        Assert.Equal(LockerStatus.Broken, locker.StateWith(false).Status);
        Assert.True(locker.IsReserved);

        locker.RestoreService(_now);
        Assert.Equal(LockerStatus.Reserved, locker.StateWith(false).Status);
    }

    [Fact]
    [Trait("spec", Spec + ": Marcar y resolver una avería (Avería en una taquilla de baja)")]
    public void A_retired_locker_cannot_be_marked_broken_or_in_maintenance()
    {
        var locker = Make();
        locker.Retire(false, _now);

        Assert.Equal("Lockers.Retired", locker.MarkOutOfService(OutOfServiceKind.Broken, null, false, _now).Error!.Code);
        Assert.Equal("Lockers.Retired", locker.MarkOutOfService(OutOfServiceKind.Maintenance, null, false, _now).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Marcar y resolver una avería (Resolver la avería)")]
    public void Resolving_the_breakdown_puts_the_locker_back_in_service_and_records_which_kind_ended()
    {
        var locker = Make();
        locker.MarkOutOfService(OutOfServiceKind.Broken, null, false, _now);

        var result = locker.RestoreService(_now);

        Assert.Null(locker.OutOfService);
        Assert.Equal(LockerStatus.Free, locker.StateWith(false).Status);
        Assert.Equal(LockerEventTypes.ServiceRestored, result.Value!.Type);
        Assert.Contains("Broken", result.Value.BeforeJson, StringComparison.Ordinal);
        Assert.Equal("Lockers.NotOutOfService", locker.RestoreService(_now).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": En mantenimiento como segundo tipo de fuera de servicio (Marcar en mantenimiento)")]
    public void A_free_locker_can_be_put_in_maintenance()
    {
        var locker = Make();

        var result = locker.MarkOutOfService(OutOfServiceKind.Maintenance, null, false, _now);

        Assert.Equal(LockerStatus.Maintenance, locker.StateWith(false).Status);
        Assert.Equal(LockerEventTypes.OutOfService, result.Value!.Event!.Type);
    }

    [Fact]
    [Trait("spec", Spec + ": En mantenimiento como segundo tipo de fuera de servicio (Cambiar de tipo)")]
    public void Changing_between_broken_and_maintenance_needs_no_decision_even_if_occupied()
    {
        var locker = Make();
        locker.MarkOutOfService(OutOfServiceKind.Broken, OutOfServiceDecision.Keep, hasAssignment: true, _now);

        var result = locker.MarkOutOfService(OutOfServiceKind.Maintenance, null, hasAssignment: true, _now);

        Assert.False(result.Value!.NeedsDecision);
        Assert.Equal(LockerEventTypes.ServiceTypeChanged, result.Value.Event!.Type);
        Assert.Equal(OutOfServiceKind.Maintenance, locker.OutOfService);
        Assert.Contains("Broken", result.Value.Event.BeforeJson, StringComparison.Ordinal);
        Assert.Contains("Maintenance", result.Value.Event.AfterJson, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": En mantenimiento como segundo tipo de fuera de servicio (Mismas reglas que la avería)")]
    public void Marking_the_same_kind_again_is_refused_and_the_kinds_exclude_each_other()
    {
        var locker = Make();
        locker.MarkOutOfService(OutOfServiceKind.Maintenance, null, false, _now);

        Assert.Equal("Lockers.AlreadyOutOfService", locker.MarkOutOfService(OutOfServiceKind.Maintenance, null, false, _now).Error!.Code);
        locker.MarkOutOfService(OutOfServiceKind.Broken, null, false, _now);
        Assert.Equal(OutOfServiceKind.Broken, locker.OutOfService); // one at a time
    }

    [Fact]
    [Trait("spec", Spec + ": En mantenimiento como segundo tipo de fuera de servicio (Volver a servicio)")]
    public void Ending_the_maintenance_puts_the_locker_back_in_service()
    {
        var locker = Make();
        locker.MarkOutOfService(OutOfServiceKind.Maintenance, null, false, _now);

        Assert.True(locker.RestoreService(_now).IsSuccess);
        Assert.Equal(LockerStatus.Free, locker.StateWith(false).Status);
    }

    // --- Decision when occupied ---

    [Theory]
    [InlineData(OutOfServiceKind.Broken)]
    [InlineData(OutOfServiceKind.Maintenance)]
    [Trait("spec", Spec + ": Decisión obligatoria al poner fuera de servicio una taquilla ocupada (Avería sin decisión)")]
    public void An_occupied_locker_without_a_decision_asks_for_one_and_changes_nothing(OutOfServiceKind kind)
    {
        var locker = Make();

        var result = locker.MarkOutOfService(kind, null, hasAssignment: true, _now);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.NeedsDecision);
        Assert.Null(result.Value.Event);
        Assert.Equal(
            [OutOfServiceDecision.Reassign, OutOfServiceDecision.Keep, OutOfServiceDecision.Release], result.Value.DecisionsOffered);
        Assert.Null(locker.OutOfService);
    }

    [Fact]
    [Trait("spec", Spec + ": Decisión obligatoria al poner fuera de servicio una taquilla ocupada (Decisión de mantener)")]
    public void Keeping_the_student_puts_the_locker_out_of_service_and_records_the_decision()
    {
        var locker = Make();

        var result = locker.MarkOutOfService(OutOfServiceKind.Broken, OutOfServiceDecision.Keep, hasAssignment: true, _now);

        Assert.False(result.Value!.NeedsDecision);
        var state = locker.StateWith(hasAssignment: true);
        Assert.Equal(LockerStatus.Broken, state.Status);
        Assert.True(state.HasAssignment);
        Assert.Contains("Keep", result.Value.Event!.AfterJson, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(OutOfServiceDecision.Reassign)]
    [InlineData(OutOfServiceDecision.Release)]
    [Trait("spec", Spec + ": Decisión obligatoria al poner fuera de servicio una taquilla ocupada (Avería sin decisión)")]
    public void Reassigning_or_releasing_is_not_available_yet_and_changes_nothing(OutOfServiceDecision decision)
    {
        var locker = Make();

        var result = locker.MarkOutOfService(OutOfServiceKind.Broken, decision, hasAssignment: true, _now);

        Assert.Equal("Lockers.DecisionNotAvailable", result.Error!.Code);
        Assert.Equal(decision.ToString(), Assert.Single(result.Error.Args));
        Assert.Null(locker.OutOfService);
    }

    // --- Zone change ---

    [Fact]
    [Trait("spec", Spec + ": Cambio de número y de zona (Cambio de zona)")]
    public void Moving_to_another_active_zone_records_the_previous_and_the_new_zone()
    {
        var locker = Make();
        var other = Zone.Create(Guid.NewGuid(), "Gimnàs", [_zone]).Value!;

        var result = locker.ChangeZone(other, _now);

        Assert.Equal(other.Id, locker.ZoneId);
        Assert.Contains(_zone.Id.ToString(), result.Value!.BeforeJson, StringComparison.Ordinal);
        Assert.Contains(other.Id.ToString(), result.Value.AfterJson, StringComparison.Ordinal);
        Assert.Equal(LockerEventTypes.ZoneChanged, result.Value.Type);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambio de número y de zona (Cambio de zona)")]
    public void Moving_to_a_deactivated_zone_or_to_the_same_zone_is_refused()
    {
        var locker = Make();

        Assert.Equal("Lockers.ZoneUnavailable", locker.ChangeZone(Inactive("Antiga"), _now).Error!.Code);
        Assert.Equal("Lockers.Unchanged", locker.ChangeZone(_zone, _now).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambio de número y de zona (Cambio sobre una taquilla de baja)")]
    public void A_retired_locker_accepts_no_change_of_number_or_zone()
    {
        var locker = Make();
        locker.Retire(false, _now);
        var other = Zone.Create(Guid.NewGuid(), "Gimnàs", [_zone]).Value!;

        Assert.Equal("Lockers.Retired", locker.ChangeNumber(99, [locker], _now).Error!.Code);
        Assert.Equal("Lockers.Retired", locker.ChangeZone(other, _now).Error!.Code);
        Assert.Equal("Lockers.Retired", locker.Reserve(null, false, _now).Error!.Code);
        Assert.Equal("Lockers.Retired", locker.RemoveReservation(_now).Error!.Code);
        Assert.Equal("Lockers.Retired", locker.RestoreService(_now).Error!.Code);
        Assert.Equal("Lockers.Retired", locker.Retire(false, _now).Error!.Code);
    }

    // --- Retirement ---

    [Fact]
    [Trait("spec", Spec + ": Baja definitiva (Baja correcta)")]
    public void A_free_locker_can_be_retired_and_is_no_longer_active()
    {
        var locker = Make();

        var result = locker.Retire(hasAssignment: false, _now);

        Assert.True(locker.IsRetired);
        Assert.Equal(_now, locker.RetiredAtUtc);
        Assert.Equal(LockerStatus.Retired, locker.StateWith(false).Status);
        Assert.Equal(LockerEventTypes.Retired, result.Value!.Type);
    }

    [Fact]
    [Trait("spec", Spec + ": Baja definitiva (Baja con asignación)")]
    public void A_locker_with_an_assignment_cannot_be_retired()
    {
        var locker = Make();

        Assert.Equal("Lockers.HasAssignment", locker.Retire(hasAssignment: true, _now).Error!.Code);
        Assert.False(locker.IsRetired);
    }

    [Fact]
    [Trait("spec", Spec + ": Baja definitiva (Baja con reserva)")]
    public void A_reserved_locker_cannot_be_retired_until_the_reservation_is_removed()
    {
        var locker = Make();
        locker.Reserve(null, false, _now);

        Assert.Equal("Lockers.HasReservation", locker.Retire(false, _now).Error!.Code);
        locker.RemoveReservation(_now);
        Assert.True(locker.Retire(false, _now).IsSuccess);
    }

    [Theory]
    [InlineData(OutOfServiceKind.Broken)]
    [InlineData(OutOfServiceKind.Maintenance)]
    [Trait("spec", Spec + ": Baja definitiva (Baja de una taquilla averiada)")]
    public void A_broken_or_maintenance_locker_without_assignment_or_reservation_can_be_retired(OutOfServiceKind kind)
    {
        var locker = Make();
        locker.MarkOutOfService(kind, null, false, _now);

        Assert.True(locker.Retire(false, _now).IsSuccess);
    }

    [Fact]
    [Trait("spec", Spec + ": Baja definitiva (Reactivar una baja)")]
    public void There_is_no_operation_to_reactivate_a_retired_locker()
    {
        var operations = typeof(Locker).GetMethods().Select(m => m.Name);

        Assert.DoesNotContain(operations, name => name.Contains("Reactivate", StringComparison.Ordinal) || name.Contains("Unretire", StringComparison.Ordinal));
        Assert.False(typeof(Locker).GetProperty(nameof(Locker.RetiredAtUtc))!.SetMethod is { IsPublic: true }); // only Retire sets it
    }

    // --- History ---

    [Fact]
    [Trait("spec", Spec + ": Historial de eventos de la taquilla (Historial inmutable)")]
    public void A_history_event_has_no_operation_to_be_edited_and_holds_no_translated_text()
    {
        var properties = typeof(HistoryEvent).GetProperties();

        Assert.All(properties.Where(p => p.SetMethod is not null), p => Assert.True(p.SetMethod!.ReturnParameter.GetRequiredCustomModifiers().Length > 0, p.Name + " must be init-only"));
        Assert.All(LockerEventTypes.All, type => Assert.Matches("^Locker\\.[A-Z][A-Za-z]+$", type));
        Assert.Equal(LockerEventTypes.All.Count, LockerEventTypes.All.Distinct().Count());
    }
}
