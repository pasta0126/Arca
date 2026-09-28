// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Actions;

/// <summary>Whether an action makes sense now, and if not, why, so it can be shown disabled with an explanation.</summary>
public readonly record struct Availability(bool IsAvailable, string? Reason = null)
{
    public static Availability Available { get; } = new(true);

    /// <summary>Not available now. The reason is what the person reads in the tooltip of the disabled action.</summary>
    public static Availability Unavailable(string? reason = null) => new(false, reason);
}
