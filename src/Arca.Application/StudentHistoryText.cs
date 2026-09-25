// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.Json;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.Domain.Students;

namespace Arca.Application.Students;

/// <summary>
/// Composes the text of a student's history event when it is shown (alumnes-i-assignacions, D1), from the key
/// History.&lt;type&gt; and the structured values of the event. The text is never stored, so it follows the active language.
/// The history is opened from the record of the student, where the email may be shown.
/// </summary>
internal sealed class StudentHistoryText(
    ILocalizer localizer, IReadOnlyDictionary<Guid, string> levels, IReadOnlyDictionary<Guid, string> groups, IReadOnlyDictionary<Guid, string> years)
{
    public string Compose(HistoryEvent change)
    {
        var before = Parse(change.BeforeJson);
        var after = Parse(change.AfterJson);
        return change.Type switch
        {
            StudentEventTypes.Created => Get(change.Type, Text(after, "firstName"), Text(after, "lastName")),
            StudentEventTypes.DataChanged => Get(change.Type, Changes(before, after)),
            StudentEventTypes.Enrolled => Text(after, "groupId") is { Length: > 0 }
                ? Get(change.Type + "WithGroup", Name(years, after, "yearId"), Name(levels, after, "levelId"), Name(groups, after, "groupId"))
                : Get(change.Type, Name(years, after, "yearId"), Name(levels, after, "levelId")),
            StudentEventTypes.EnrollmentChanged => Get(change.Type, Placement(before), Placement(after)),
            StudentEventTypes.Retired => Get(change.Type, Text(after, "reason")),
            StudentEventTypes.Reactivated => Get(change.Type),
            _ => localizer.Get("History.Unknown", change.Type),
        };
    }

    string Get(string type, params object[] args) => localizer.Get(string.Concat("History.", type), args);

    string Changes(JsonElement? before, JsonElement? after)
    {
        var parts = new List<string>();
        foreach (var field in new[] { "firstName", "lastName", "email" })
        {
            if (Text(after, field).Length > 0)
            {
                parts.Add(localizer.Get(
                    "History.Student.Change", localizer.Get(string.Concat("History.Field.", field)), Text(before, field), Text(after, field)));
            }
        }

        return string.Join("; ", parts);
    }

    string Placement(JsonElement? root) => Text(root, "groupId") is { Length: > 0 }
        ? localizer.Get("History.Student.PlacementWithGroup", Name(levels, root, "levelId"), Name(groups, root, "groupId"))
        : localizer.Get("History.Student.Placement", Name(levels, root, "levelId"));

    static JsonElement? Parse(string? json) => json is null ? null : JsonDocument.Parse(json).RootElement.Clone();

    static string Text(JsonElement? root, string name) =>
        root is { ValueKind: JsonValueKind.Object } element && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    static string Name(IReadOnlyDictionary<Guid, string> names, JsonElement? root, string field) =>
        Guid.TryParse(Text(root, field), out var id) && names.TryGetValue(id, out var name) ? name : "?";
}
