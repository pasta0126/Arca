// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Catalog.ListCatalog;

/// <summary>The levels and groups that exist, by name as people read them: what the filters and the student form offer.</summary>
public sealed record CatalogListing(IReadOnlyList<string> Levels, IReadOnlyList<string> Groups);

/// <summary>The catalogue of levels and groups (pantalles-de-domini, D5 and D6), read only. New values are created by writing them in a student form.</summary>
public sealed class ListCatalogHandler(ICatalogRepository catalog)
{
    public async Task<Result<CatalogListing>> HandleAsync(CancellationToken ct) =>
        Result<CatalogListing>.Success(new CatalogListing(
            [.. (await catalog.ListLevelsAsync(ct)).Select(l => l.Name).Order(TextComparer.Comparer)],
            [.. (await catalog.ListGroupsAsync(ct)).Select(g => g.Name).Order(TextComparer.Comparer)]));
}
