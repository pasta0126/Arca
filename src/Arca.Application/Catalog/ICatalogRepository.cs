// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Catalog;

namespace Arca.Application.Catalog;

/// <summary>Where the catalogue of levels and groups is kept. It only grows: values are created, never deleted.</summary>
public interface ICatalogRepository
{
    Task<IReadOnlyList<Level>> ListLevelsAsync(CancellationToken ct);

    Task<IReadOnlyList<Group>> ListGroupsAsync(CancellationToken ct);

    Task AddLevelAsync(Level level, CancellationToken ct);

    Task AddGroupAsync(Group group, CancellationToken ct);
}
