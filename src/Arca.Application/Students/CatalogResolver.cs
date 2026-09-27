// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Catalog;
using Arca.Domain.Catalog;
using Arca.Domain.Common;
using Arca.Domain.Enrollments;

namespace Arca.Application.Students;

internal static class CatalogResolver
{
    /// <summary>Finds or prepares the level and group that were typed. A missing level is an error; a missing group is allowed.</summary>
    public static async Task<Result<CatalogChoice>> ResolveAsync(
        ICatalogRepository catalog, string? levelName, string? groupName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(levelName))
        {
            return Result<CatalogChoice>.Failure(EnrollmentErrors.LevelRequired);
        }

        var levels = await catalog.ListLevelsAsync(ct);
        var groups = await catalog.ListGroupsAsync(ct);
        var newValues = new List<NewCatalogValue>();

        var level = Level.Find(levelName, levels);
        var levelIsNew = level is null;
        if (level is null)
        {
            var created = Level.Create(Guid.NewGuid(), levelName, levels);
            if (!created.IsSuccess)
            {
                return Result<CatalogChoice>.Failure(created.Error!);
            }

            level = created.Value!;
            newValues.Add(new NewCatalogValue(CatalogKind.Level, level.Name));
        }

        Group? group = null;
        var groupIsNew = false;
        if (!string.IsNullOrWhiteSpace(groupName))
        {
            group = Group.Find(level.Id, groupName, groups);
            if (group is null)
            {
                var created = Group.Create(Guid.NewGuid(), level, groupName, groups);
                if (!created.IsSuccess)
                {
                    return Result<CatalogChoice>.Failure(created.Error!);
                }

                group = created.Value!;
                groupIsNew = true;
                newValues.Add(new NewCatalogValue(CatalogKind.Group, group.Name, level.Name));
            }
        }

        return Result<CatalogChoice>.Success(new CatalogChoice(level, group, newValues, levelIsNew, groupIsNew));
    }

    /// <summary>Creates the values that the person confirmed.</summary>
    public static async Task CreateNewAsync(ICatalogRepository catalog, CatalogChoice choice, CancellationToken ct)
    {
        if (choice.LevelIsNew)
        {
            await catalog.AddLevelAsync(choice.Level, ct);
        }

        if (choice.GroupIsNew && choice.Group is not null)
        {
            await catalog.AddGroupAsync(choice.Group, ct);
        }
    }
}
