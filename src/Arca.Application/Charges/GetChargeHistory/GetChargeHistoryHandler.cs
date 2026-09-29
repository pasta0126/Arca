// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.Json;
using Arca.Application.Localization;
using Arca.Domain.Charges;
using Arca.Domain.Common;

namespace Arca.Application.Charges.GetChargeHistory;

/// <param name="ChargeId">The charge whose history is read.</param>
public sealed record GetChargeHistoryRequest(Guid ChargeId);

/// <summary>One change of a charge, in words: what changed, from and to, and the reason when there is one.</summary>
public sealed record ChargeHistoryLine(DateTimeOffset At, string Text);

/// <summary>
/// The history of a charge (pantalles-cobraments, Historial del cargo), the most recent first: each change with its instant, the state
/// before and after, the reason and the amounts. It only reads: the events cannot be changed or removed.
/// </summary>
public sealed class GetChargeHistoryHandler(IChargeRepository charges, IChargeEventRepository events, ILocalizer localizer)
{
    public async Task<Result<IReadOnlyList<ChargeHistoryLine>>> HandleAsync(GetChargeHistoryRequest request, CancellationToken ct)
    {
        if (await charges.GetAsync(request.ChargeId, ct) is null)
        {
            return Result<IReadOnlyList<ChargeHistoryLine>>.Failure(ChargeErrors.NotFound);
        }

        IReadOnlyList<ChargeHistoryLine> lines =
        [
            .. (await events.ListAsync(request.ChargeId, ct)).OrderByDescending(e => e.OccurredAtUtc).Select(e => new ChargeHistoryLine(e.OccurredAtUtc, Compose(e))),
        ];
        return Result<IReadOnlyList<ChargeHistoryLine>>.Success(lines);
    }

    string Compose(HistoryEvent change)
    {
        var before = Parse(change.BeforeJson);
        var after = Parse(change.AfterJson);
        var text = change.Type switch
        {
            ChargeEventTypes.Created => localizer.Get("Charges.History.Created", Amount(after)),
            ChargeEventTypes.Paid => localizer.Get("Charges.History.Paid", Date(after)),
            ChargeEventTypes.AmountAdjusted => localizer.Get("Charges.History.AmountAdjusted", Amount(before), Amount(after)),
            ChargeEventTypes.Exempted or ChargeEventTypes.Waived or ChargeEventTypes.Voided or ChargeEventTypes.Reverted =>
                localizer.Get("Charges.History.StatusChanged", Status(before), Status(after)),
            _ => localizer.Get("History.Unknown", change.Type),
        };
        return change.Reason is { Length: > 0 } reason ? localizer.Get("Charges.History.WithReason", text, reason) : text;
    }

    string Status(JsonElement? root) =>
        root is { } e && e.TryGetProperty("status", out var s) && s.GetString() is { } name ? new ChargeResultTexts(localizer).StatusName(name) : string.Empty;

    string Amount(JsonElement? root) =>
        root is { } e && e.TryGetProperty("amount", out var a) ? localizer.Format(Money.FromCents((long)Math.Round(a.GetDecimal() * 100))) : string.Empty;

    string Date(JsonElement? root) =>
        root is { } e && e.TryGetProperty("paidOn", out var d) && DateOnly.TryParse(d.GetString(), out var date) ? localizer.Format(date) : string.Empty;

    static JsonElement? Parse(string? json)
    {
        if (json is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
