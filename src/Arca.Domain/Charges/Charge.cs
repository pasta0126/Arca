// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.Encodings.Web;
using System.Text.Json;
using Arca.Domain.Common;
using Arca.Domain.ConceptAmounts;

namespace Arca.Domain.Charges;

/// <summary>
/// A charge of a student for a concept, generated in a school year (pagaments, D1): its amount is fixed when it is
/// created and never follows a later change to the year's amounts. It is pending, paid, exempt, condoned or voided;
/// only pending counts as debt. Every change returns the history event it originates, and business rules return a
/// failure instead of throwing.
/// </summary>
public sealed class Charge
{
    public const int MaximumReasonLength = 500;

    static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Rebuilds a stored charge. Used by persistence, which has already validated it.</summary>
    public Charge(Guid id, Guid studentId, ChargeConcept concept, Guid yearId, Money amount, ChargeStatus status, DateOnly? paidOn, string? reason)
    {
        Id = id;
        StudentId = studentId;
        Concept = concept;
        YearId = yearId;
        Amount = amount;
        Status = status;
        PaidOn = paidOn;
        Reason = reason;
    }

    public Guid Id { get; }

    public Guid StudentId { get; }

    public ChargeConcept Concept { get; }

    /// <summary>The year the charge was generated in.</summary>
    public Guid YearId { get; }

    /// <summary>Fixed when the charge is created; a later change to the year's amounts never touches it.</summary>
    public Money Amount { get; private set; }

    public ChargeStatus Status { get; private set; }

    /// <summary>When it was paid, set only while <see cref="Status"/> is <see cref="ChargeStatus.Paid"/>.</summary>
    public DateOnly? PaidOn { get; private set; }

    /// <summary>The reason of the current status, set only while it is exempt, waived or voided.</summary>
    public string? Reason { get; private set; }

    /// <summary>Only a pending charge counts as debt.</summary>
    public bool CountsAsDebt => Status == ChargeStatus.Pending;

    /// <summary>Generates a pending charge with the amount fixed at this instant.</summary>
    public static ChargeCreated Create(Guid id, Guid studentId, ChargeConcept concept, Guid yearId, Money amount, DateTimeOffset now)
    {
        var charge = new Charge(id, studentId, concept, yearId, amount, ChargeStatus.Pending, null, null);
        var created = new HistoryEvent(id, ChargeEventTypes.Created, now, null, Json(new { status = ChargeStatus.Pending.ToString(), amount = amount.Amount }));
        return new ChargeCreated(charge, created);
    }

    /// <summary>Marks a pending charge as paid. The date defaults to today and cannot be in the future.</summary>
    public Result<HistoryEvent> MarkPaid(DateOnly? paidOn, DateOnly today, DateTimeOffset now)
    {
        var refused = RefuseUnlessPending();
        if (refused is not null)
        {
            return Result<HistoryEvent>.Failure(refused);
        }

        var date = paidOn ?? today;
        if (date > today)
        {
            return Result<HistoryEvent>.Failure(ChargeErrors.DateInvalid);
        }

        var before = StatusSnapshot();
        (Status, PaidOn, Reason) = (ChargeStatus.Paid, date, null);
        return Result<HistoryEvent>.Success(new HistoryEvent(Id, ChargeEventTypes.Paid, now, before, Json(new { status = Status.ToString(), paidOn = date })));
    }

    /// <summary>Marks a pending charge as exempt, with a reason. It stops counting as debt.</summary>
    public Result<HistoryEvent> MarkExempt(string? reason, DateTimeOffset now) => Transition(ChargeStatus.Exempt, reason, ChargeEventTypes.Exempted, now);

    /// <summary>Waives a pending charge, with a reason. It stops counting as debt.</summary>
    public Result<HistoryEvent> Waive(string? reason, DateTimeOffset now) => Transition(ChargeStatus.Waived, reason, ChargeEventTypes.Waived, now);

    /// <summary>Reverts a paid, exempt or waived charge back to pending, with a reason. A voided charge cannot be reverted.</summary>
    public Result<HistoryEvent> Revert(string? reason, DateTimeOffset now)
    {
        if (Status is ChargeStatus.Pending or ChargeStatus.Voided)
        {
            return Result<HistoryEvent>.Failure(ChargeErrors.InvalidStatus);
        }

        var clean = CleanReason(reason);
        if (!clean.IsSuccess)
        {
            return Result<HistoryEvent>.Failure(clean.Error!);
        }

        var before = StatusSnapshot();
        (Status, PaidOn, Reason) = (ChargeStatus.Pending, null, null);
        return Result<HistoryEvent>.Success(new HistoryEvent(Id, ChargeEventTypes.Reverted, now, before, null, clean.Value));
    }

    /// <summary>
    /// Voids a pending charge, with a reason. It becomes final: it no longer counts as debt and cannot be reverted. A
    /// paid, exempt or waived charge has to be reverted to pending first; an already voided one cannot be voided again.
    /// </summary>
    public Result<HistoryEvent> Void(string? reason, DateTimeOffset now)
    {
        if (Status == ChargeStatus.Voided)
        {
            return Result<HistoryEvent>.Failure(ChargeErrors.InvalidStatus);
        }

        if (Status != ChargeStatus.Pending)
        {
            return Result<HistoryEvent>.Failure(ChargeErrors.MustRevertFirst);
        }

        var clean = CleanReason(reason);
        if (!clean.IsSuccess)
        {
            return Result<HistoryEvent>.Failure(clean.Error!);
        }

        var before = StatusSnapshot();
        (Status, Reason) = (ChargeStatus.Voided, clean.Value);
        return Result<HistoryEvent>.Success(new HistoryEvent(Id, ChargeEventTypes.Voided, now, before, Json(new { status = Status.ToString() }), clean.Value));
    }

    /// <summary>Changes the amount of a pending charge, with a reason and within the same limits as the year's amounts.</summary>
    public Result<HistoryEvent> AdjustAmount(decimal amount, string? reason, DateTimeOffset now)
    {
        var refused = RefuseUnlessPending();
        if (refused is not null)
        {
            return Result<HistoryEvent>.Failure(refused);
        }

        var clean = CleanReason(reason);
        if (!clean.IsSuccess)
        {
            return Result<HistoryEvent>.Failure(clean.Error!);
        }

        if (!Money.TryFromDecimalInRange(amount, ConceptAmount.MaximumAmount, out var money))
        {
            return Result<HistoryEvent>.Failure(ChargeErrors.AmountInvalid(ConceptAmount.MaximumAmount));
        }

        var before = Json(new { amount = Amount.Amount });
        Amount = money;
        return Result<HistoryEvent>.Success(new HistoryEvent(Id, ChargeEventTypes.AmountAdjusted, now, before, Json(new { amount = Amount.Amount }), clean.Value));
    }

    Error? RefuseUnlessPending() => Status == ChargeStatus.Pending ? null : ChargeErrors.InvalidStatus;

    string StatusSnapshot() => Json(new { status = Status.ToString(), paidOn = PaidOn, reason = Reason });

    Result<HistoryEvent> Transition(ChargeStatus target, string? reason, string eventType, DateTimeOffset now)
    {
        var refused = RefuseUnlessPending();
        if (refused is not null)
        {
            return Result<HistoryEvent>.Failure(refused);
        }

        var clean = CleanReason(reason);
        if (!clean.IsSuccess)
        {
            return Result<HistoryEvent>.Failure(clean.Error!);
        }

        var before = StatusSnapshot();
        (Status, PaidOn, Reason) = (target, null, clean.Value);
        return Result<HistoryEvent>.Success(new HistoryEvent(Id, eventType, now, before, Json(new { status = target.ToString(), reason = clean.Value }), clean.Value));
    }

    static Result<string> CleanReason(string? reason)
    {
        var clean = reason?.Trim() ?? string.Empty;
        if (clean.Length == 0)
        {
            return Result<string>.Failure(ChargeErrors.ReasonRequired);
        }

        return clean.Length > MaximumReasonLength
            ? Result<string>.Failure(ChargeErrors.ReasonTooLong(MaximumReasonLength))
            : Result<string>.Success(clean);
    }

    static string Json(object value) => JsonSerializer.Serialize(value, _json);
}
