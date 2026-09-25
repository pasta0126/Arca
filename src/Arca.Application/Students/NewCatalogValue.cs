// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Students;

/// <summary>Which kind of catalogue value is new.</summary>
public enum CatalogKind
{
    Level,
    Group,
}

/// <summary>A level or group typed by the person that the catalogue does not have yet, to be confirmed before it is created.</summary>
/// <param name="Kind">Level or group.</param>
/// <param name="Name">The name as typed.</param>
/// <param name="LevelName">For a group, the name of its level.</param>
public sealed record NewCatalogValue(CatalogKind Kind, string Name, string? LevelName = null);
