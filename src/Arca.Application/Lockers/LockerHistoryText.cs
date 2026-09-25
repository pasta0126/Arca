// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.Json;
using Arca.Application.Localization;
using Arca.Domain.Common;

namespace Arca.Application.Lockers;

/// <summary>
/// Composes the text of a history event when it is shown (taquilles-i-zones, D7): the key is History.&lt;type&gt; and the
/// values come from the structured before and after of the event. The text is never stored, so it follows the active
/// language and an event of a type this version does not know still shows something readable.
/// </summary>
internal sealed class LockerHistoryText(ILocalizer localizer)
{
    public string Compose(HistoryEvent change, IReadOnlyDictionary<Guid, string> zoneNames)
    {
        var before = Parse(change.BeforeJson);
        var after = Parse(change.AfterJson);
        return change.Type switch
        {
            "Locker.Created" => Get(change.Type, Number(after), Zone(after, zoneNames)),
            "Locker.NumberChanged" => Get(change.Type, Number(before), Number(after)),
            "Locker.ZoneChanged" => Get(change.Type, Zone(before, zoneNames), Zone(after, zoneNames)),
            "Locker.Reserved" => Note(after) is { } note ? Get(change.Type + "WithNote", note) : Get(change.Type),
            "Locker.ReservationRemoved" => Get(change.Type),
            "Locker.OutOfService" => Text(after, "decision") == "Keep"
                ? Get(change.Type + "Keeping", Kind(after))
                : Get(change.Type, Kind(after)),
            "Locker.ServiceTypeChanged" => Get(change.Type, Kind(before), Kind(after)),
            "Locker.ServiceRestored" => Get(change.Type, Kind(before)),
            "Locker.Retired" => Get(change.Type),
            _ => localizer.Get("History.Unknown", change.Type),
        };
    }

    /// <summary>The keys are built from the event type, so a test checks that each type has its text (a literal scan cannot).</summary>
    string Get(string type, params object[] args) => localizer.Get(string.Concat("History.", type), args);

    static JsonElement? Parse(string? json) => json is null ? null : JsonDocument.Parse(json).RootElement.Clone();

    static string Text(JsonElement? root, string name) =>
        root is { ValueKind: JsonValueKind.Object } element && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    static string Number(JsonElement? root) =>
        root is { ValueKind: JsonValueKind.Object } element && element.TryGetProperty("number", out var value) ? value.GetRawText() : "?";

    static string? Note(JsonElement? root) => Text(root, "note") is { Length: > 0 } note ? note : null;

    string Kind(JsonElement? root) => Text(root, "kind") is { Length: > 0 } kind ? localizer.Get(string.Concat("History.Kind.", kind)) : "?";

    static string Zone(JsonElement? root, IReadOnlyDictionary<Guid, string> names) =>
        Guid.TryParse(Text(root, "zoneId"), out var id) && names.TryGetValue(id, out var name) ? name : "?";
}
