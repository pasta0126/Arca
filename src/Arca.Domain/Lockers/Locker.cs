// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.Encodings.Web;
using System.Text.Json;
using Arca.Domain.Common;
using Arca.Domain.Zones;

namespace Arca.Domain.Lockers;

/// <summary>
/// A locker (taquilles-i-zones). It stores facts, not a status: number, zone, note, whether it is out of service and how,
/// its reservation and when it was retired. Whether a student holds it comes from the assignments and is given to each
/// rule. Every change returns the history event it originates, so it is saved in the same operation; the rules return a
/// failure instead of throwing, and a retired locker accepts no change.
/// </summary>
public sealed class Locker
{
    public const int MaximumNoteLength = 500;

    static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // keep accents readable in the stored history
    };

    /// <summary>Rebuilds a stored locker. Used by persistence, which has already validated it.</summary>
    public Locker(
        Guid id, int number, Guid zoneId, string? note, OutOfServiceKind? outOfService, bool isReserved, string? reservationNote,
        DateTimeOffset? retiredAtUtc, Guid? reservedForStudentId = null)
    {
        Id = id;
        Number = number;
        ZoneId = zoneId;
        Note = note;
        OutOfService = outOfService;
        IsReserved = isReserved;
        ReservationNote = reservationNote;
        RetiredAtUtc = retiredAtUtc;
        ReservedForStudentId = reservedForStudentId;
    }

    /// <summary>The internal identity. It never changes; history and everything tied to the locker follow it, not the number.</summary>
    public Guid Id { get; }

    public int Number { get; private set; }

    public Guid ZoneId { get; private set; }

    public string? Note { get; private set; }

    public OutOfServiceKind? OutOfService { get; private set; }

    public bool IsReserved { get; private set; }

    public string? ReservationNote { get; private set; }

    /// <summary>The student the reservation is for, or null for a reservation with no student (alumnes-i-assignacions).</summary>
    public Guid? ReservedForStudentId { get; private set; }

    public DateTimeOffset? RetiredAtUtc { get; private set; }

    public bool IsRetired => RetiredAtUtc is not null;

    /// <summary>The status and what the locker keeps underneath, given whether a student holds it.</summary>
    public LockerState StateWith(bool hasAssignment) =>
        LockerStatusCalculator.Calculate(new LockerFacts(IsRetired, OutOfService, hasAssignment, IsReserved));

    /// <summary>Creates a free locker in an active zone. Its number must not belong to another locker that is not retired.</summary>
    public static Result<LockerCreated> Create(
        Guid id, int number, Zone zone, string? note, IEnumerable<Locker> existing, DateTimeOffset now)
    {
        var valid = LockerNumber.Validate(number);
        if (!valid.IsSuccess)
        {
            return Result<LockerCreated>.Failure(valid.Error!);
        }

        var cleanNote = CleanNote(note);
        if (!cleanNote.IsSuccess)
        {
            return Result<LockerCreated>.Failure(cleanNote.Error!);
        }

        if (!zone.IsActive)
        {
            return Result<LockerCreated>.Failure(LockerErrors.ZoneUnavailable);
        }

        if (NumberTaken(number, existing, except: null))
        {
            return Result<LockerCreated>.Failure(LockerErrors.NumberInUse(number));
        }

        var locker = new Locker(id, number, zone.Id, cleanNote.Value, null, false, null, null);
        var created = new HistoryEvent(
            id, LockerEventTypes.Created, now, null, Json(new { number, zoneId = zone.Id, note = cleanNote.Value }));
        return Result<LockerCreated>.Success(new LockerCreated(locker, created));
    }

    /// <summary>Changes the number, which must not belong to another locker that is not retired.</summary>
    public Result<HistoryEvent> ChangeNumber(int number, IEnumerable<Locker> all, DateTimeOffset now)
    {
        var refused = RefuseIfRetired();
        if (refused is not null)
        {
            return Result<HistoryEvent>.Failure(refused);
        }

        var valid = LockerNumber.Validate(number);
        if (!valid.IsSuccess)
        {
            return Result<HistoryEvent>.Failure(valid.Error!);
        }

        if (number == Number)
        {
            return Result<HistoryEvent>.Failure(LockerErrors.Unchanged);
        }

        if (NumberTaken(number, all, except: this))
        {
            return Result<HistoryEvent>.Failure(LockerErrors.NumberInUse(number));
        }

        var before = Number;
        Number = number;
        return Done(LockerEventTypes.NumberChanged, now, new { number = before }, new { number });
    }

    /// <summary>Moves the locker to another zone, which must be active.</summary>
    public Result<HistoryEvent> ChangeZone(Zone zone, DateTimeOffset now)
    {
        var refused = RefuseIfRetired();
        if (refused is not null)
        {
            return Result<HistoryEvent>.Failure(refused);
        }

        if (!zone.IsActive)
        {
            return Result<HistoryEvent>.Failure(LockerErrors.ZoneUnavailable);
        }

        if (zone.Id == ZoneId)
        {
            return Result<HistoryEvent>.Failure(LockerErrors.Unchanged);
        }

        var before = ZoneId;
        ZoneId = zone.Id;
        return Done(LockerEventTypes.ZoneChanged, now, new { zoneId = before }, new { zoneId = zone.Id });
    }

    /// <summary>Reserves a free locker, with an optional note. The reservation belongs to no student.</summary>
    public Result<HistoryEvent> Reserve(string? note, bool hasAssignment, DateTimeOffset now)
    {
        var refused = RefuseIfRetired();
        if (refused is not null)
        {
            return Result<HistoryEvent>.Failure(refused);
        }

        if (StateWith(hasAssignment).Status != LockerStatus.Free)
        {
            return Result<HistoryEvent>.Failure(LockerErrors.NotFree);
        }

        var cleanNote = CleanNote(note);
        if (!cleanNote.IsSuccess)
        {
            return Result<HistoryEvent>.Failure(cleanNote.Error!);
        }

        IsReserved = true;
        ReservationNote = cleanNote.Value;
        return Done(LockerEventTypes.Reserved, now, null, new { note = cleanNote.Value });
    }

    /// <summary>
    /// Reserves a free locker for a given student, with an optional note. It becomes an assignment when it is formalised, and the
    /// reservation is then consumed. Whether the student already has a locker or another reservation is checked by the use case.
    /// </summary>
    public Result<HistoryEvent> ReserveForStudent(Guid studentId, string? note, bool hasAssignment, DateTimeOffset now)
    {
        var reserved = Reserve(note, hasAssignment, now);
        if (!reserved.IsSuccess)
        {
            return reserved;
        }

        ReservedForStudentId = studentId;
        return Done(LockerEventTypes.Reserved, now, null, new { note = ReservationNote, studentId });
    }

    /// <summary>The reservation is turned into an assignment for the student it was for.</summary>
    public Result<HistoryEvent> ConsumeReservation(DateTimeOffset now)
    {
        if (!IsReserved)
        {
            return Result<HistoryEvent>.Failure(LockerErrors.NotReserved);
        }

        var before = new { note = ReservationNote, studentId = ReservedForStudentId };
        (IsReserved, ReservationNote, ReservedForStudentId) = (false, null, null);
        return Done(LockerEventTypes.ReservationConsumed, now, before, null);
    }

    public Result<HistoryEvent> RemoveReservation(DateTimeOffset now)
    {
        var refused = RefuseIfRetired();
        if (refused is not null)
        {
            return Result<HistoryEvent>.Failure(refused);
        }

        if (!IsReserved)
        {
            return Result<HistoryEvent>.Failure(LockerErrors.NotReserved);
        }

        var before = new { note = ReservationNote, studentId = ReservedForStudentId };
        (IsReserved, ReservationNote, ReservedForStudentId) = (false, null, null);
        return Done(LockerEventTypes.ReservationRemoved, now, before, null);
    }

    /// <summary>
    /// Puts the locker out of service as broken or in maintenance (taquilles-i-zones, D3). If a student holds it, a
    /// decision is needed first and nothing changes without one. Only keeping the student is available for now.
    /// Changing from one kind to the other needs no decision. A reservation is kept.
    /// </summary>
    public Result<OutOfServiceOutcome> MarkOutOfService(
        OutOfServiceKind kind, OutOfServiceDecision? decision, bool hasAssignment, DateTimeOffset now)
    {
        var refused = RefuseIfRetired();
        if (refused is not null)
        {
            return Result<OutOfServiceOutcome>.Failure(refused);
        }

        if (OutOfService == kind)
        {
            return Result<OutOfServiceOutcome>.Failure(LockerErrors.AlreadyOutOfService);
        }

        if (OutOfService is { } current)
        {
            OutOfService = kind;
            return Result<OutOfServiceOutcome>.Success(OutOfServiceOutcome.Done(
                Event(LockerEventTypes.ServiceTypeChanged, now, new { kind = current.ToString() }, new { kind = kind.ToString() })));
        }

        if (hasAssignment)
        {
            if (decision is null)
            {
                return Result<OutOfServiceOutcome>.Success(OutOfServiceOutcome.DecisionRequired());
            }

            if (decision != OutOfServiceDecision.Keep)
            {
                return Result<OutOfServiceOutcome>.Failure(LockerErrors.DecisionNotAvailable(decision.Value));
            }

        }

        OutOfService = kind;
        return Result<OutOfServiceOutcome>.Success(OutOfServiceOutcome.Done(Event(
            LockerEventTypes.OutOfService, now, null,
            new { kind = kind.ToString(), decision = hasAssignment ? decision.ToString() : null })));
    }

    /// <summary>Ends the breakdown or the maintenance. The status is recomputed from the facts, nothing else is done.</summary>
    public Result<HistoryEvent> RestoreService(DateTimeOffset now)
    {
        var refused = RefuseIfRetired();
        if (refused is not null)
        {
            return Result<HistoryEvent>.Failure(refused);
        }

        if (OutOfService is not { } kind)
        {
            return Result<HistoryEvent>.Failure(LockerErrors.NotOutOfService);
        }

        OutOfService = null;
        return Done(LockerEventTypes.ServiceRestored, now, new { kind = kind.ToString() }, null);
    }

    /// <summary>Retires the locker for good. It cannot have an assignment or a reservation, and there is no way back.</summary>
    public Result<HistoryEvent> Retire(bool hasAssignment, DateTimeOffset now)
    {
        var refused = RefuseIfRetired();
        if (refused is not null)
        {
            return Result<HistoryEvent>.Failure(refused);
        }

        if (hasAssignment)
        {
            return Result<HistoryEvent>.Failure(LockerErrors.HasAssignment);
        }

        if (IsReserved)
        {
            return Result<HistoryEvent>.Failure(LockerErrors.HasReservation);
        }

        RetiredAtUtc = now;
        return Done(LockerEventTypes.Retired, now, null, new { retiredAtUtc = now });
    }

    static bool NumberTaken(int number, IEnumerable<Locker> lockers, Locker? except) =>
        lockers.Any(l => !l.IsRetired && l.Number == number && l.Id != except?.Id);

    static Result<string?> CleanNote(string? note)
    {
        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        return trimmed?.Length > MaximumNoteLength
            ? Result<string?>.Failure(LockerErrors.NoteTooLong(MaximumNoteLength))
            : Result<string?>.Success(trimmed);
    }

    Error? RefuseIfRetired() => IsRetired ? LockerErrors.Retired : null;

    Result<HistoryEvent> Done(string type, DateTimeOffset now, object? before, object? after) =>
        Result<HistoryEvent>.Success(Event(type, now, before, after));

    HistoryEvent Event(string type, DateTimeOffset now, object? before, object? after) =>
        new(Id, type, now, before is null ? null : Json(before), after is null ? null : Json(after));

    static string Json(object value) => JsonSerializer.Serialize(value, _json);
}
