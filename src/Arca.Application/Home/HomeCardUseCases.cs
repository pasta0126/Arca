// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.Domain.Home;

namespace Arca.Application.Home;

/// <summary>
/// Creates the cards every centre starts with (targetes-d-inici, Tarjetas de serie), once: the first time it runs it makes them and
/// remembers it, so one the person deleted does not come back. Restoring adds only the ones that are missing, recognised by their key.
/// </summary>
public sealed class EnsureDefaultCardsHandler(IHomeCardRepository cards, IUnitOfWork unit, IClock clock, ILocalizer localizer)
{
    /// <returns>How many cards were created: all of them the first time and none after.</returns>
    public Task<Result<int>> HandleAsync(CancellationToken ct) => unit.RunAsync(async token =>
    {
        if (await cards.GetDefaultsCreatedAtAsync(token) is not null)
        {
            return Result<int>.Success(0);
        }

        var created = await AddMissingAsync(token);
        await cards.MarkDefaultsCreatedAsync(clock.UtcNow, token);
        return created;
    }, ct);

    /// <returns>How many of the missing cards were added; the ones already there, edited or not, are not touched.</returns>
    public Task<Result<int>> RestoreAsync(CancellationToken ct) => unit.RunAsync(async token =>
    {
        var added = await AddMissingAsync(token);
        if (added.IsSuccess)
        {
            await cards.MarkDefaultsCreatedAsync(clock.UtcNow, token);
        }

        return added;
    }, ct);

    async Task<Result<int>> AddMissingAsync(CancellationToken ct)
    {
        var existing = await cards.ListAsync(ct);
        var keys = existing.Select(c => c.SeedKey).Where(k => k is not null).ToHashSet();
        var next = existing.Select(c => c.Position + 1).DefaultIfEmpty(0).Max();
        var room = HomeCard.MaximumCards - existing.Count;
        var created = 0;
        foreach (var definition in DefaultHomeCards.All.Where(d => !keys.Contains(d.SeedKey)))
        {
            if (created >= room)
            {
                return Result<int>.Failure(HomeCardErrors.TooManyCards(HomeCard.MaximumCards));
            }

            var card = HomeCard.Create(Guid.NewGuid(), localizer.Get(definition.TitleKey), definition.Target, definition.Criteria, next++, definition.SeedKey);
            if (!card.IsSuccess)
            {
                return Result<int>.Failure(card.Error!);
            }

            await cards.AddAsync(card.Value!, ct);
            created++;
        }

        return Result<int>.Success(created);
    }
}

/// <param name="Title">The title the person wrote.</param>
/// <param name="Target">The screen the card opens.</param>
/// <param name="Criteria">What to filter by, name and value.</param>
public sealed record CreateHomeCardRequest(string? Title, HomeCardTargetView Target, IReadOnlyDictionary<string, string> Criteria);

/// <summary>Makes a card at the end of the others, after checking its title and its criteria against the screen it opens, and that there is room for one more.</summary>
public sealed class CreateHomeCardHandler(IHomeCardRepository cards, IUnitOfWork unit)
{
    public Task<Result<HomeCardSaved>> HandleAsync(CreateHomeCardRequest request, CancellationToken ct) => unit.RunAsync(async token =>
    {
        var existing = await cards.ListAsync(token);
        if (existing.Count >= HomeCard.MaximumCards)
        {
            return Result<HomeCardSaved>.Failure(HomeCardErrors.TooManyCards(HomeCard.MaximumCards));
        }

        var card = HomeCard.Create(
            Guid.NewGuid(), request.Title, Enum.Parse<HomeCardTarget>(request.Target.ToString()), request.Criteria,
            existing.Select(c => c.Position + 1).DefaultIfEmpty(0).Max());
        if (!card.IsSuccess)
        {
            return Result<HomeCardSaved>.Failure(card.Error!);
        }

        await cards.AddAsync(card.Value!, token);
        return Result<HomeCardSaved>.Success(new HomeCardSaved(card.Value!.Id, card.Value.Title));
    }, ct);
}

public sealed record EditHomeCardRequest(Guid Id, string? Title, HomeCardTargetView Target, IReadOnlyDictionary<string, string> Criteria);

/// <summary>Changes the title, the screen and the criteria of a card, checked as a new one is; nothing changes if one is refused.</summary>
public sealed class EditHomeCardHandler(IHomeCardRepository cards, IUnitOfWork unit)
{
    public Task<Result<HomeCardSaved>> HandleAsync(EditHomeCardRequest request, CancellationToken ct) => unit.RunAsync(async token =>
    {
        if (await cards.GetAsync(request.Id, token) is not { } card)
        {
            return Result<HomeCardSaved>.Failure(HomeCardErrors.NotFound);
        }

        var edited = card.Edit(request.Title, Enum.Parse<HomeCardTarget>(request.Target.ToString()), request.Criteria);
        if (!edited.IsSuccess)
        {
            return Result<HomeCardSaved>.Failure(edited.Error!);
        }

        await cards.UpdateAsync(card, token);
        return Result<HomeCardSaved>.Success(new HomeCardSaved(card.Id, card.Title));
    }, ct);
}

public sealed record MoveHomeCardRequest(Guid Id, HomeCardMove Move);

/// <summary>
/// Puts a card one place earlier or later, swapping with its neighbour in one unit of work, and numbers the places again from 0 so they
/// stay dense. A card at the end it is moved towards stays where it is.
/// </summary>
public sealed class MoveHomeCardHandler(IHomeCardRepository cards, IUnitOfWork unit)
{
    /// <returns>Whether the card moved.</returns>
    public Task<Result<bool>> HandleAsync(MoveHomeCardRequest request, CancellationToken ct) => unit.RunAsync(async token =>
    {
        var all = (await cards.ListAsync(token)).ToList();
        var at = all.FindIndex(c => c.Id == request.Id);
        if (at < 0)
        {
            return Result<bool>.Failure(HomeCardErrors.NotFound);
        }

        var to = request.Move == HomeCardMove.Earlier ? at - 1 : at + 1;
        if (to < 0 || to >= all.Count)
        {
            return Result<bool>.Success(false);
        }

        (all[at], all[to]) = (all[to], all[at]);
        for (var i = 0; i < all.Count; i++)
        {
            if (all[i].Position != i)
            {
                all[i].MoveTo(i);
                await cards.UpdateAsync(all[i], token);
            }
        }

        return Result<bool>.Success(true);
    }, ct);
}

public sealed record DeleteHomeCardRequest(Guid Id);

/// <summary>Deletes a card and numbers the places of the others again. Nothing but the card is touched: no student and no locker.</summary>
public sealed class DeleteHomeCardHandler(IHomeCardRepository cards, IUnitOfWork unit)
{
    /// <returns>The title of the card that was deleted, for the notification.</returns>
    public Task<Result<string>> HandleAsync(DeleteHomeCardRequest request, CancellationToken ct) => unit.RunAsync(async token =>
    {
        var all = (await cards.ListAsync(token)).ToList();
        if (all.FirstOrDefault(c => c.Id == request.Id) is not { } card)
        {
            return Result<string>.Failure(HomeCardErrors.NotFound);
        }

        await cards.RemoveAsync(card, token);
        all.Remove(card);
        for (var i = 0; i < all.Count; i++)
        {
            if (all[i].Position != i)
            {
                all[i].MoveTo(i);
                await cards.UpdateAsync(all[i], token);
            }
        }

        return Result<string>.Success(card.Title);
    }, ct);
}
