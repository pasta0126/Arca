// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Catalog;

namespace Arca.Application.Students;

/// <summary>
/// The level and group a person typed, resolved against the catalogue (alumnes-i-assignacions, D6): existing values are
/// reused whatever their case or accents, and the ones that do not exist yet are prepared, but not created, so the person
/// can confirm them first.
/// </summary>
/// <param name="Level">The level to use, existing or new.</param>
/// <param name="Group">The group to use, existing or new; null when no group was typed.</param>
/// <param name="NewValues">What has to be created if the person confirms.</param>
internal sealed record CatalogChoice(Level Level, Group? Group, IReadOnlyList<NewCatalogValue> NewValues, bool LevelIsNew, bool GroupIsNew);
