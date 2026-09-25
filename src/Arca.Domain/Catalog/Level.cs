// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Catalog;

/// <summary>
/// A level of study, such as "1r ESO" (alumnes-i-assignacions, D6). There are no fixed values in the code: they come from
/// the files that are imported and from manual sign-ups. Two names that differ only in case, accents or spaces are the
/// same level.
/// </summary>
public sealed class Level
{
    public const int MaximumNameLength = 50;

    public Level(Guid id, string name, string nameKey)
    {
        Id = id;
        Name = name;
        NameKey = nameKey;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string NameKey { get; }

    public static Result<Level> Create(Guid id, string? name, IEnumerable<Level> existing)
    {
        var clean = CleanName(name);
        if (!clean.IsSuccess)
        {
            return Result<Level>.Failure(clean.Error!);
        }

        var key = TextComparer.Key(clean.Value);
        return existing.Any(l => l.NameKey == key)
            ? Result<Level>.Failure(CatalogErrors.LevelExists(clean.Value!))
            : Result<Level>.Success(new Level(id, clean.Value!, key));
    }

    /// <summary>The level that a typed name refers to, ignoring case and accents, or null if the catalogue has none.</summary>
    public static Level? Find(string? name, IEnumerable<Level> levels)
    {
        var key = TextComparer.Key(name);
        return key.Length == 0 ? null : levels.FirstOrDefault(l => l.NameKey == key);
    }

    internal static Result<string> CleanName(string? name)
    {
        var clean = name?.Trim() ?? string.Empty;
        if (clean.Length == 0)
        {
            return Result<string>.Failure(CatalogErrors.NameRequired);
        }

        return clean.Length > MaximumNameLength
            ? Result<string>.Failure(CatalogErrors.NameTooLong(MaximumNameLength))
            : Result<string>.Success(clean);
    }
}
