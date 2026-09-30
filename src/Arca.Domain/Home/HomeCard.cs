// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Home;

/// <summary>
/// A card of the start screen (targetes-d-inici, Una tarjeta es un filtro guardado): a title, the screen it opens, the criteria of the
/// filter it opens it with and its place among the others. It holds what to filter by and nothing about any student.
/// </summary>
public sealed class HomeCard
{
    /// <summary>The longest title, in characters.</summary>
    public const int MaximumTitleLength = 60;

    /// <summary>The most cards a centre can have.</summary>
    public const int MaximumCards = 24;

    string _criteriaText;

    public HomeCard(Guid id, string title, HomeCardTarget target, string criteriaText, int position, string? seedKey)
    {
        Id = id;
        Title = title;
        Target = target;
        _criteriaText = criteriaText;
        Position = position;
        SeedKey = seedKey;
    }

    public Guid Id { get; }

    public string Title { get; private set; }

    public HomeCardTarget Target { get; private set; }

    /// <summary>The criteria as the text that is stored. <see cref="Criteria"/> reads them.</summary>
    public string CriteriaText => _criteriaText;

    /// <summary>The place among the cards, from 0; the cards are shown by it.</summary>
    public int Position { get; private set; }

    /// <summary>A stable name when this is one of the cards every centre starts with, so restoring them never duplicates one; otherwise null.</summary>
    public string? SeedKey { get; }

    /// <summary>What the card filters by, name and value.</summary>
    public IReadOnlyDictionary<string, string> Criteria => HomeCardCriteria.Deserialize(_criteriaText);

    /// <summary>Checks a title: from 1 to 60 characters once trimmed.</summary>
    public static Result<string> CheckTitle(string? title)
    {
        var clean = title?.Trim() ?? string.Empty;
        return clean.Length == 0 ? Result<string>.Failure(HomeCardErrors.TitleRequired)
            : clean.Length > MaximumTitleLength ? Result<string>.Failure(HomeCardErrors.TitleTooLong(MaximumTitleLength))
            : Result<string>.Success(clean);
    }

    /// <summary>Makes a card, after checking its title and its criteria against the screen it opens.</summary>
    public static Result<HomeCard> Create(
        Guid id, string? title, HomeCardTarget target, IReadOnlyDictionary<string, string>? criteria, int position, string? seedKey = null)
    {
        var checkedTitle = CheckTitle(title);
        if (!checkedTitle.IsSuccess)
        {
            return Result<HomeCard>.Failure(checkedTitle.Error!);
        }

        var checkedCriteria = HomeCardCriteria.Check(target, criteria);
        return checkedCriteria.IsSuccess
            ? Result<HomeCard>.Success(new HomeCard(id, checkedTitle.Value!, target, HomeCardCriteria.Serialize(checkedCriteria.Value!), position, seedKey))
            : Result<HomeCard>.Failure(checkedCriteria.Error!);
    }

    /// <summary>Changes the title, the screen and the criteria, checking them as a new card is checked. Nothing changes if one is refused.</summary>
    public Result<bool> Edit(string? title, HomeCardTarget target, IReadOnlyDictionary<string, string>? criteria)
    {
        var checkedTitle = CheckTitle(title);
        if (!checkedTitle.IsSuccess)
        {
            return Result<bool>.Failure(checkedTitle.Error!);
        }

        var checkedCriteria = HomeCardCriteria.Check(target, criteria);
        if (!checkedCriteria.IsSuccess)
        {
            return Result<bool>.Failure(checkedCriteria.Error!);
        }

        (Title, Target, _criteriaText) = (checkedTitle.Value!, target, HomeCardCriteria.Serialize(checkedCriteria.Value!));
        return Result<bool>.Success(true);
    }

    /// <summary>Puts the card in another place among the others.</summary>
    public void MoveTo(int position) => Position = position;
}
