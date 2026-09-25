// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Zones;

/// <summary>
/// The name of a zone once it is valid (taquilles-i-zones): trimmed, at most 60 characters, and with the normalised key
/// that decides whether two names are the same.
/// </summary>
public sealed record ZoneName
{
    public const int MaximumLength = 60;

    ZoneName(string value, string key)
    {
        Value = value;
        Key = key;
    }

    /// <summary>The name as it is shown, without leading or trailing spaces.</summary>
    public string Value { get; }

    /// <summary>The name without case, accents or repeated spaces, unique among zones.</summary>
    public string Key { get; }

    public static Result<ZoneName> Parse(string? typed)
    {
        var value = typed?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            return Result<ZoneName>.Failure(ZoneErrors.NameRequired);
        }

        return value.Length > MaximumLength
            ? Result<ZoneName>.Failure(ZoneErrors.NameTooLong(MaximumLength))
            : Result<ZoneName>.Success(new ZoneName(value, TextComparer.Key(value)));
    }
}
