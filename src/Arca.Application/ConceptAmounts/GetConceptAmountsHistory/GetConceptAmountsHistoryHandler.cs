// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.Json;
using Arca.Application.Localization;
using Arca.Application.SchoolYears;
using Arca.Domain.Common;
using Arca.Domain.ConceptAmounts;
using Arca.Domain.SchoolYears;

namespace Arca.Application.ConceptAmounts.GetConceptAmountsHistory;

/// <param name="YearId">The year whose changes of amount are read.</param>
public sealed record GetConceptAmountsHistoryRequest(Guid YearId);

/// <summary>One change of an amount, in words: which concept, and the value it had and the one it has.</summary>
public sealed record AmountHistoryLine(DateTimeOffset At, string Text);

/// <summary>
/// The history of the amounts of a year (pantalles-de-domini, Historial de importes): every definition and change of each of its
/// three amounts, the most recent first. It only reads: the events cannot be changed or removed.
/// </summary>
public sealed class GetConceptAmountsHistoryHandler(
    IAcademicYearRepository years, IConceptAmountRepository amounts, IConceptAmountEventRepository events, ILocalizer localizer)
{
    public async Task<Result<IReadOnlyList<AmountHistoryLine>>> HandleAsync(GetConceptAmountsHistoryRequest request, CancellationToken ct)
    {
        if (await years.GetAsync(request.YearId, ct) is null)
        {
            return Result<IReadOnlyList<AmountHistoryLine>>.Failure(SchoolYearErrors.NotFound);
        }

        var lines = new List<AmountHistoryLine>();
        foreach (var amount in await amounts.ListByYearAsync(request.YearId, ct))
        {
            var concept = ConceptNames.Of(localizer, amount.Concept.ToString());
            foreach (var change in await events.ListAsync(amount.Id, ct))
            {
                lines.Add(new AmountHistoryLine(change.OccurredAtUtc, change.Type == ConceptAmountEventTypes.Created
                    ? localizer.Get("ConceptAmounts.History.Created", concept, Show(change.AfterJson))
                    : localizer.Get("ConceptAmounts.History.Changed", concept, Show(change.BeforeJson), Show(change.AfterJson))));
            }
        }

        return Result<IReadOnlyList<AmountHistoryLine>>.Success([.. lines.OrderByDescending(l => l.At)]);
    }

    string Show(string? json)
    {
        if (json is null)
        {
            return string.Empty;
        }

        using var document = JsonDocument.Parse(json);
        return localizer.Format(Money.FromCents((long)Math.Round(document.RootElement.GetProperty("amount").GetDecimal() * 100)));
    }
}
