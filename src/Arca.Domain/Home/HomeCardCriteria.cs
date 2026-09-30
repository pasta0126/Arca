// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.Json;
using Arca.Domain.Common;

namespace Arca.Domain.Home;

/// <summary>The screen a card opens.</summary>
public enum HomeCardTarget
{
    /// <summary>The map of the lockers.</summary>
    LockerMap,

    /// <summary>The list of the lockers.</summary>
    Lockers,

    /// <summary>The list of the students.</summary>
    Students,
}

/// <summary>
/// The criteria a card can filter by (targetes-d-inici, Una tarjeta es un filtro guardado): a closed set of names with the values each one
/// accepts, the same names the screens understand when they are opened with a filter. A criterion only says what to filter by, never who:
/// it holds no name, email or identifier of a student.
/// </summary>
public static class HomeCardCriteria
{
    /// <summary>The status of a locker: Free, Occupied, Reserved, Broken or Maintenance.</summary>
    public const string Status = "Status";

    /// <summary>The zone of a locker, by its identity.</summary>
    public const string Zone = "Zone";

    /// <summary>Whether the student has a locker: with or without.</summary>
    public const string Locker = "Locker";

    /// <summary>Whether the student owes something: pending or upToDate.</summary>
    public const string Payment = "Payment";

    /// <summary>The level of the student, by its name.</summary>
    public const string Level = "Level";

    /// <summary>The group of the student, by its name.</summary>
    public const string Group = "Group";

    /// <summary>Whether the students who left are listed too: true.</summary>
    public const string IncludeRetired = "IncludeRetired";

    /// <summary>The longest name of a level or a group.</summary>
    public const int MaximumNameLength = 100;

    static readonly string[] _lockerCriteria = [Status, Zone];
    static readonly string[] _studentCriteria = [Locker, Payment, Level, Group, IncludeRetired];
    static readonly string[] _statuses = ["Free", "Occupied", "Reserved", "Broken", "Maintenance"];

    /// <summary>The criteria the screen of a target understands.</summary>
    public static IReadOnlyList<string> For(HomeCardTarget target) => target == HomeCardTarget.Students ? _studentCriteria : _lockerCriteria;

    /// <summary>
    /// Checks the criteria of a card against its target: every name must be one the screen has and every value one it understands. It
    /// answers the criteria as they will be kept, in a stable order.
    /// </summary>
    public static Result<IReadOnlyDictionary<string, string>> Check(HomeCardTarget target, IReadOnlyDictionary<string, string>? criteria)
    {
        var kept = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, value) in criteria ?? new Dictionary<string, string>())
        {
            if (!For(target).Contains(name))
            {
                return Result<IReadOnlyDictionary<string, string>>.Failure(HomeCardErrors.CriterionUnknown(name));
            }

            var clean = value?.Trim() ?? string.Empty;
            var valid = name switch
            {
                Status => _statuses.Contains(clean),
                Zone => Guid.TryParse(clean, out _),
                Locker => clean is "with" or "without",
                Payment => clean is "pending" or "upToDate",
                IncludeRetired => clean == "true",
                _ => clean.Length is > 0 and <= MaximumNameLength,
            };
            if (!valid)
            {
                return Result<IReadOnlyDictionary<string, string>>.Failure(HomeCardErrors.CriterionInvalid(name));
            }

            kept[name] = clean;
        }

        return Result<IReadOnlyDictionary<string, string>>.Success(kept);
    }

    /// <summary>The criteria as the text that is stored.</summary>
    public static string Serialize(IReadOnlyDictionary<string, string> criteria) =>
        JsonSerializer.Serialize(new SortedDictionary<string, string>(criteria.ToDictionary(c => c.Key, c => c.Value), StringComparer.Ordinal));

    /// <summary>The criteria from the text that was stored; empty when there is nothing or it cannot be read.</summary>
    public static IReadOnlyDictionary<string, string> Deserialize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(text) ?? [];
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }
}
