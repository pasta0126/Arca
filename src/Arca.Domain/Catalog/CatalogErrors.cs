// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Catalog;

/// <summary>Error codes of the catalogue of levels and groups. Resource keys: Catalog.Error.&lt;Name&gt;.</summary>
public static class CatalogErrors
{
    public static readonly Error NameRequired = new("Catalog.NameRequired");

    /// <summary>Args: {0} maximum length.</summary>
    public static Error NameTooLong(int maximum) => new("Catalog.NameTooLong", Args: [maximum]);

    /// <summary>A level with the same name, ignoring case and accents, already exists. Args: {0} the name.</summary>
    public static Error LevelExists(string name) => new("Catalog.LevelExists", Args: [name]);

    /// <summary>The level already has a group with that name. Args: {0} the name.</summary>
    public static Error GroupExists(string name) => new("Catalog.GroupExists", Args: [name]);
}
