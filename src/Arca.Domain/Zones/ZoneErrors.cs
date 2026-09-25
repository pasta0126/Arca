// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Zones;

/// <summary>Error codes of the zones. Resource keys: Zones.Error.&lt;Name&gt;.</summary>
public static class ZoneErrors
{
    /// <summary>The name is empty or only spaces.</summary>
    public static readonly Error NameRequired = new("Zones.NameRequired");

    /// <summary>The name is longer than allowed. Args: {0} maximum length.</summary>
    public static Error NameTooLong(int maximum) => new("Zones.NameTooLong", Args: [maximum]);

    /// <summary>Another zone, active or not, has the same name ignoring case and accents. Args: {0} the name.</summary>
    public static Error NameDuplicate(string name) => new("Zones.NameDuplicate", Args: [name]);

    /// <summary>The zone still has lockers that are not retired. Args: {0} how many.</summary>
    public static Error HasActiveLockers(int count) => new("Zones.HasActiveLockers", Args: [count]);

    /// <summary>The zone has or had lockers, so it cannot be deleted, only deactivated.</summary>
    public static readonly Error HasHistory = new("Zones.HasHistory");
}
