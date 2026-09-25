// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.Common;

/// <summary>
/// One entry of an append-only history (docs/convenciones.md, section 6). It never holds translated text: the type is a
/// stable code and the text is composed when it is shown, from the key History.&lt;Type&gt; and the values.
/// </summary>
/// <param name="EntityId">The internal identity of what changed, never a visible number.</param>
/// <param name="Type">Stable code such as "Locker.NumberChanged". Each capability adds its own.</param>
/// <param name="OccurredAtUtc">The instant of the change.</param>
/// <param name="BeforeJson">The values before the change, structured; null when there was nothing before.</param>
/// <param name="AfterJson">The values after the change, structured.</param>
/// <param name="Reason">A free note that goes with the change, if any. Treated as sensitive data.</param>
public sealed record HistoryEvent(
    Guid EntityId, string Type, DateTimeOffset OccurredAtUtc, string? BeforeJson, string? AfterJson, string? Reason = null);
