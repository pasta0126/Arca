// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.Json;
using Arca.Domain.Common;

namespace Arca.Domain.ConceptAmounts;

/// <summary>
/// The amount of a concept for a school year (pagaments, D5): fixed for every student, and independent of the amounts of
/// other years. A charge keeps the amount it was created with, so changing this one never touches a charge already
/// generated. Every change returns the history event it originates.
/// </summary>
public sealed class ConceptAmount
{
    public const decimal MaximumAmount = 9999.99m;

    static readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Rebuilds a stored amount. Used by persistence, which has already validated it.</summary>
    public ConceptAmount(Guid id, Guid yearId, ChargeConcept concept, Money amount)
    {
        Id = id;
        YearId = yearId;
        Concept = concept;
        Amount = amount;
    }

    public Guid Id { get; }

    public Guid YearId { get; }

    public ChargeConcept Concept { get; }

    public Money Amount { get; private set; }

    /// <summary>Defines the amount of a concept for a year. Greater than zero, exact cents, up to the maximum.</summary>
    public static Result<ConceptAmountCreated> Create(Guid id, Guid yearId, ChargeConcept concept, decimal amount, DateTimeOffset now)
    {
        var validated = ValidateAmount(amount);
        if (!validated.IsSuccess)
        {
            return Result<ConceptAmountCreated>.Failure(validated.Error!);
        }

        var entity = new ConceptAmount(id, yearId, concept, validated.Value);
        var created = new HistoryEvent(id, ConceptAmountEventTypes.Created, now, null, Json(validated.Value));
        return Result<ConceptAmountCreated>.Success(new ConceptAmountCreated(entity, created));
    }

    /// <summary>Changes the amount. The caller decides whether it is worth calling: an identical amount is not rejected here.</summary>
    public Result<HistoryEvent> ChangeAmount(decimal amount, DateTimeOffset now)
    {
        var validated = ValidateAmount(amount);
        if (!validated.IsSuccess)
        {
            return Result<HistoryEvent>.Failure(validated.Error!);
        }

        var before = Amount;
        Amount = validated.Value;
        return Result<HistoryEvent>.Success(new HistoryEvent(Id, ConceptAmountEventTypes.Changed, now, Json(before), Json(Amount)));
    }

    static Result<Money> ValidateAmount(decimal amount) =>
        Money.TryFromDecimal(amount, out var money) && money.Amount > 0 && money.Amount <= MaximumAmount
            ? Result<Money>.Success(money)
            : Result<Money>.Failure(ConceptAmountErrors.AmountInvalid(MaximumAmount));

    static string Json(Money amount) => JsonSerializer.Serialize(new { amount = amount.Amount }, _json);
}
