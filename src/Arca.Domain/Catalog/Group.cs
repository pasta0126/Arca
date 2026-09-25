// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Catalog;

/// <summary>
/// A group inside a level, such as "A" of "1r ESO" (alumnes-i-assignacions, D6). Groups belong to a level: the "A" of "2n ESO"
/// is another group. Names that differ only in case or accents are the same group of the level.
/// </summary>
public sealed class Group
{
    public Group(Guid id, Guid levelId, string name, string nameKey)
    {
        Id = id;
        LevelId = levelId;
        Name = name;
        NameKey = nameKey;
    }

    public Guid Id { get; }

    public Guid LevelId { get; }

    public string Name { get; }

    public string NameKey { get; }

    public static Result<Group> Create(Guid id, Level level, string? name, IEnumerable<Group> existing)
    {
        var clean = Level.CleanName(name);
        if (!clean.IsSuccess)
        {
            return Result<Group>.Failure(clean.Error!);
        }

        var key = TextComparer.Key(clean.Value);
        return existing.Any(g => g.LevelId == level.Id && g.NameKey == key)
            ? Result<Group>.Failure(CatalogErrors.GroupExists(clean.Value!))
            : Result<Group>.Success(new Group(id, level.Id, clean.Value!, key));
    }

    /// <summary>The group of a level that a typed name refers to, ignoring case and accents, or null if there is none.</summary>
    public static Group? Find(Guid levelId, string? name, IEnumerable<Group> groups)
    {
        var key = TextComparer.Key(name);
        return key.Length == 0 ? null : groups.FirstOrDefault(g => g.LevelId == levelId && g.NameKey == key);
    }
}
