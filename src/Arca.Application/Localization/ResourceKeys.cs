// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Localization;

/// <summary>
/// Derives resource keys from stable codes, with no lookup table (docs/convenciones.md, section 3):
/// error code Capacity.Name gives key Capacity.Error.Name, and a warning gives Capacity.Warning.Name.
/// </summary>
public static class ResourceKeys
{
    public static string For(Error error) => Derive(error.Code, "Error");

    public static string For(Notice notice) => Derive(notice.Code, "Warning");

    /// <summary>The capability is the first segment of a key and names its resource file.</summary>
    public static string CapabilityOf(string key)
    {
        var dot = key.IndexOf('.', StringComparison.Ordinal);
        return dot < 0 ? key : key[..dot];
    }

    static string Derive(string code, string kind)
    {
        var dot = code.IndexOf('.', StringComparison.Ordinal);
        return dot <= 0 || dot == code.Length - 1
            ? code // malformed codes are caught by the key-coverage test; at runtime the code itself is shown
            : $"{code[..dot]}.{kind}.{code[(dot + 1)..]}";
    }
}
