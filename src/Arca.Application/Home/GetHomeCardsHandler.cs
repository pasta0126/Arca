// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Catalog;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.SchoolYears;
using Arca.Application.Students.ListStudentRows;
using Arca.Application.Zones;
using Arca.Domain.Common;
using Arca.Domain.Home;

namespace Arca.Application.Home;

/// <summary>
/// Which criteria of a card still exist (targetes-d-inici, Tarjetas con filtros obsoletos): a zone that was deleted, or deactivated with no
/// lockers left, and a level or group that is no longer in the catalogue, are left out and named. Nothing is changed or deleted.
/// </summary>
static class HomeCardResolver
{
    public static (IReadOnlyDictionary<string, string> Valid, IReadOnlyList<string> Ignored) Resolve(
        IReadOnlyDictionary<string, string> criteria, IReadOnlySet<Guid> usableZones, IReadOnlySet<string> levels, IReadOnlySet<string> groups)
    {
        var valid = new Dictionary<string, string>();
        var ignored = new List<string>();
        foreach (var (name, value) in criteria)
        {
            var exists = name switch
            {
                HomeCardCriteria.Zone => Guid.TryParse(value, out var zone) && usableZones.Contains(zone),
                HomeCardCriteria.Level => levels.Contains(value),
                HomeCardCriteria.Group => groups.Contains(value),
                _ => true,
            };
            if (exists)
            {
                valid[name] = value;
            }
            else
            {
                ignored.Add(name);
            }
        }

        return (valid, ignored);
    }
}

/// <summary>
/// Reads the cards of the start screen with how many elements each counts (targetes-d-inici, Recuento en vivo de la tarjeta). It reads the
/// lockers once and the students once, however many cards there are, and counts every card over those rows with the same rule the screens
/// filter with, so what a card says and what its screen shows are the same. Only counts: never an amount.
/// </summary>
public sealed class GetHomeCardsHandler(
    IHomeCardRepository cards, IZoneRepository zones, ICatalogRepository catalog, IAcademicYearRepository years,
    Func<CancellationToken, Task<Result<LockerRowsListing>>> lockerRows, Func<CancellationToken, Task<Result<StudentRowsListing>>> studentRows)
{
    public async Task<Result<HomeCardsView>> HandleAsync(CancellationToken ct)
    {
        var all = await cards.ListAsync(ct);
        var year = await years.GetActiveAsync(ct);

        IReadOnlyList<LockerListRow> lockers = [];
        var usableZones = new HashSet<Guid>();
        if (all.Any(c => c.Target != HomeCardTarget.Students))
        {
            var listing = await lockerRows(ct);
            if (!listing.IsSuccess)
            {
                return Result<HomeCardsView>.Failure(listing.Error!);
            }

            lockers = listing.Value!.Rows;
            var withLockers = lockers.Where(l => l.Status != Arca.Application.Search.LockerStatusView.Retired).Select(l => l.ZoneId).ToHashSet();
            usableZones = [.. (await zones.ListAsync(ct)).Where(z => z.IsActive || withLockers.Contains(z.Id)).Select(z => z.Id)];
        }

        IReadOnlyList<StudentListRow>? students = null;
        var levels = new HashSet<string>();
        var groups = new HashSet<string>();
        if (all.Any(c => c.Target == HomeCardTarget.Students))
        {
            var listing = await studentRows(ct);
            if (listing.IsSuccess)
            {
                students = listing.Value!.Rows;
            }
            else if (listing.Error!.Code != "SchoolYears.NoActiveYear")
            {
                return Result<HomeCardsView>.Failure(listing.Error);
            }

            levels = [.. (await catalog.ListLevelsAsync(ct)).Select(l => l.Name)];
            groups = [.. (await catalog.ListGroupsAsync(ct)).Select(g => g.Name)];
        }

        var views = all.Select((card, index) =>
        {
            var (valid, ignored) = HomeCardResolver.Resolve(card.Criteria, usableZones, levels, groups);
            int? count = card.Target == HomeCardTarget.Students
                ? students?.Count(StudentCardFilter.From(valid).Matches)
                : lockers.Count(LockerCardFilter.From(valid).Matches);
            var state = ignored.Count > 0 ? HomeCardState.Obsolete : count is null ? HomeCardState.NoCount : HomeCardState.WithCount;
            return new HomeCardView(
                card.Id, card.Title, Enum.Parse<HomeCardTargetView>(card.Target.ToString()), valid, count, state, ignored, card.Position,
                index == 0, index == all.Count - 1, card.SeedKey);
        }).ToList();
        return Result<HomeCardsView>.Success(new HomeCardsView(year?.Name, views));
    }
}

/// <summary>
/// Opens a card: the criteria that still exist and the ones left out, so the screen is opened without the obsolete ones and says which were
/// ignored (targetes-d-inici, Abrir una tarjeta obsoleta).
/// </summary>
public sealed class ResolveHomeCardHandler(IHomeCardRepository cards, IZoneRepository zones, ICatalogRepository catalog, Func<CancellationToken, Task<Result<LockerRowsListing>>> lockerRows)
{
    public async Task<Result<ResolvedHomeCard>> HandleAsync(Guid cardId, CancellationToken ct)
    {
        if (await cards.GetAsync(cardId, ct) is not { } card)
        {
            return Result<ResolvedHomeCard>.Failure(HomeCardErrors.NotFound);
        }

        var usableZones = new HashSet<Guid>();
        if (card.Criteria.ContainsKey(HomeCardCriteria.Zone))
        {
            var listing = await lockerRows(ct);
            if (!listing.IsSuccess)
            {
                return Result<ResolvedHomeCard>.Failure(listing.Error!);
            }

            var withLockers = listing.Value!.Rows.Where(l => l.Status != Arca.Application.Search.LockerStatusView.Retired).Select(l => l.ZoneId).ToHashSet();
            usableZones = [.. (await zones.ListAsync(ct)).Where(z => z.IsActive || withLockers.Contains(z.Id)).Select(z => z.Id)];
        }

        var levels = card.Target == HomeCardTarget.Students ? (await catalog.ListLevelsAsync(ct)).Select(l => l.Name).ToHashSet() : [];
        var groups = card.Target == HomeCardTarget.Students ? (await catalog.ListGroupsAsync(ct)).Select(g => g.Name).ToHashSet() : [];
        var (valid, ignored) = HomeCardResolver.Resolve(card.Criteria, usableZones, levels, groups);
        return Result<ResolvedHomeCard>.Success(new ResolvedHomeCard(card.Id, card.Title, Enum.Parse<HomeCardTargetView>(card.Target.ToString()), valid, ignored));
    }
}

/// <summary>What the form of a card offers: the zones in use and the levels and groups of the catalogue, from the same sources as everything else.</summary>
public sealed class GetCardOptionsHandler(IZoneRepository zones, ICatalogRepository catalog)
{
    public async Task<Result<CardOptions>> HandleAsync(CancellationToken ct) => Result<CardOptions>.Success(new CardOptions(
        [.. (await zones.ListAsync(ct)).Where(z => z.IsActive).OrderBy(z => z.Name, TextComparer.Comparer).Select(z => new CardZoneOption(z.Id, z.Name))],
        [.. (await catalog.ListLevelsAsync(ct)).Select(l => l.Name).OrderBy(n => n, TextComparer.Comparer)],
        [.. (await catalog.ListGroupsAsync(ct)).Select(g => g.Name).OrderBy(n => n, TextComparer.Comparer)]));
}
