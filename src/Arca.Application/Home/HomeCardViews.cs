// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Home;

/// <summary>The screen a card opens, as the interface sees it: the map of the lockers, the list of the lockers or the list of the students.</summary>
public enum HomeCardTargetView
{
    LockerMap,
    Lockers,
    Students,
}

/// <summary>What a card is showing: its count, no count (there is no active year for students), or a filter that no longer exists.</summary>
public enum HomeCardState
{
    /// <summary>The filter is fine and the count is there.</summary>
    WithCount,

    /// <summary>There is nothing to count yet: the students of a centre without an active year.</summary>
    NoCount,

    /// <summary>A criterion of the filter (a zone, a level or a group) no longer exists; the count leaves it out.</summary>
    Obsolete,
}

/// <summary>A card of the start screen as it is drawn (targetes-d-inici): title, screen, the criteria that still hold, how many elements it counts and what is wrong with it, if anything.</summary>
/// <param name="Criteria">The criteria that still exist: the ones a screen opened by this card is given.</param>
/// <param name="Count">How many elements meet the filter, or null when there is nothing to count; always a count, never an amount.</param>
/// <param name="Ignored">The names of the criteria that no longer exist and were left out.</param>
/// <param name="IsFirst">Whether it is the first card, so it cannot move earlier.</param>
/// <param name="IsLast">Whether it is the last card, so it cannot move later.</param>
public sealed record HomeCardView(
    Guid Id, string Title, HomeCardTargetView Target, IReadOnlyDictionary<string, string> Criteria, int? Count, HomeCardState State,
    IReadOnlyList<string> Ignored, int Position, bool IsFirst, bool IsLast, string? SeedKey);

/// <summary>The cards of the start screen in their order, with the active year they are counted for.</summary>
public sealed record HomeCardsView(string? ActiveYearName, IReadOnlyList<HomeCardView> Cards);

/// <summary>A card that was saved, for the notification that says so.</summary>
public sealed record HomeCardSaved(Guid Id, string Title);

/// <summary>A card as it opens its screen: the criteria that still exist and the ones that were left out, to say so.</summary>
public sealed record ResolvedHomeCard(Guid Id, string Title, HomeCardTargetView Target, IReadOnlyDictionary<string, string> Criteria, IReadOnlyList<string> Ignored);

/// <summary>A zone the form of a card can filter by.</summary>
public sealed record CardZoneOption(Guid Id, string Name);

/// <summary>What the form of a card offers to filter by: the zones in use, and the levels and groups of the catalogue.</summary>
public sealed record CardOptions(IReadOnlyList<CardZoneOption> Zones, IReadOnlyList<string> Levels, IReadOnlyList<string> Groups);

/// <summary>Which way a card moves.</summary>
public enum HomeCardMove
{
    Earlier,
    Later,
}
